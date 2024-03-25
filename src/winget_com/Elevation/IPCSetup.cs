// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Contracts;
using DevHome.SetupFlow.Common.Helpers;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using WinRT;

namespace DevHome.SetupFlow.Common.Elevation;

/// <summary>
/// Helper class for establishing a background process to offload work to,
/// and communicate with it. This is currently used only when we need
/// some tasks to be executed with admin permissions; in that case we
/// create a background process with the required permissions and then
/// hand off all the work to it.
/// </summary>
/// <remarks>
/// For this setup we need two things, first is having inter-process
/// communication between the processes and the second is having the
/// background process run elevated. There are multiple ways to get
/// this to work (e.g. using COM and creating objects with the COM
/// Elevation Moniker).
///
/// The solution we use here is to start the elevated process, and then
/// use a shared block of memory to pass the marshalling info for a
/// WinRT object that will then be used to create all the required objects
/// for communication.
/// </remarks>
//// Implementation details:
////
//// * The background process executable has an app manifest that requires
////   it to always run elevated.
////
//// * We use a MemoryMappedFile to share a block of memory between the
////   app process and the background process we start. On this block we
////   write, in order: an HResult to report failures, the size of the
////   marshal information for the remote object, and finally the
////   marshaler object itself.
////
//// * To have the main app process wait for the initialization done in the
////   background process to finish, it creates a global EventWaitHandle
////   that is signaled by the background process.
////
//// * To prevent the background process from terminating right after the
////   setup while we still have objects hosted on it, the main app
////   process creates and acquires a global mutex and only releases when
////   the remote object is not needed anymore. The background process
////   waits to acquire the mutex before exiting, ensuring that it only
////   terminates when it is no longer needed.
////
////   Conceptually, we use a mutex, but in reality we use a semaphore.
////   Mutexes are owned by threads, so it's hard to keep one in the main
////   process due to async calls.
////
//// * The methods that set up the remote object are generic due to some
////   behaviors of CsWinRT that need to be worked around. The ElevatedServer
////   process needs the actual types from the ElevatedComponent to
////   create the new object, so it has a project reference to it.
////   Everywhere else, what we need is the projection types to be able
////   to create the proxy objects; use a reference to the
////   ElevatedComponent.Projection that uses the WinMD to generate the
////   projections. The code here is called from both sides, so it needs
////   to work for different versions of the same type
public static class IPCSetup
{
    /// <summary>
    /// Object that is written at the beginning of the shared memory block.
    /// The marshalled remote object is written immediately after this.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct MappedMemoryValue
    {
        /// <summary>
        /// Result of the setup operation.
        /// </summary>
        public int HResult;

        /// <summary>
        /// Size of the marshaled remote object.
        /// </summary>
        public long MarshaledObjectSize;
    }

    /// <summary>
    /// Maximum capacity of the shared memory block. Must be at least big
    /// enough to hold a <see cref="MappedMemoryValue"/> and the marshalled
    /// remote object. Default to 4kb.
    /// </summary>
    private const long MappedMemoryCapacityInBytes = 4 << 10;

    /// <summary>
    /// The size of the <see cref="MappedMemoryValue"/> that is written
    /// at the beginning of the shared memory.
    /// </summary>
    private static readonly long MappedMemoryValueSizeInBytes = Marshal.SizeOf<MappedMemoryValue>();

    /// <summary>
    /// The maximum size that the marshalled remote object can have.
    /// If this is not big enough, we should increase the maximum capacity.
    /// </summary>
    private static readonly long MaxRemoteObjectSizeInBytes = MappedMemoryCapacityInBytes - MappedMemoryValueSizeInBytes;

    /// <summary>
    /// Gets the Interface ID for a type; used for the initial interface being marshalled between the processes.
    /// </summary>
    private static Guid GetMarshalInterfaceGUID<T>()
    {
        return GuidGenerator.CreateIID(typeof(T));
    }

    /// <summary>
    /// Creates a remote object for the operations that need to execute in the elevated
    /// background process. This is to be called from the (unelevated) main
    /// app process.
    /// </summary>
    /// <param name="tasksArguments">Tasks arguments</param>
    /// <returns>A proxy object that executes operations in the background process.</returns>
    public static async Task<RemoteObject<T>> CreateOutOfProcessObjectAsync<T>(TasksArguments tasksArguments)
    {
        // Run this in the background since it may take a while
        (var remoteObject, _) = await Task.Run(() => CreateOutOfProcessObjectAndGetProcess<T>(tasksArguments));
        return remoteObject;
    }

    /// <summary>
    /// Creates a remote object for the operations that need to execute in the elevated
    /// background process. This is to be called from the main app process.
    /// </summary>
    /// <param name="tasksArguments">Tasks arguments</param>
    /// <remarks>
    /// This is intended to be used for tests. For anything else we
    /// should use <see cref="IPCSetup.CreateOutOfProcessObjectAsync{T}"/>
    /// </remarks>
    /// <returns>A proxy object that execute operations in the background process.</returns>
    public static (RemoteObject<T>, Process) CreateOutOfProcessObjectAndGetProcess<T>(TasksArguments tasksArguments, bool isForTesting = false)
    {
        // The shared memory block, initialization event and completion semaphore all need a name
        // that will be used by the child process to find them. We use new random GUIDs for them.
        // For the memory block, we also set the handle inheritability so that only descendant
        // process can access it.
        var mappedFileName = Guid.NewGuid().ToString();
        var initEventName = Guid.NewGuid().ToString();
        var completionSemaphoreName = Guid.NewGuid().ToString();
        var tasksArgumentList = tasksArguments.ToArgumentList();

        // Create shared memory block.
        Debug.WriteLine("Creating shared memory block");
        using var mappedFile = MemoryMappedFile.CreateNew(
            mappedFileName,
            MappedMemoryCapacityInBytes,
            MemoryMappedFileAccess.ReadWrite,
            MemoryMappedFileOptions.None,
            HandleInheritability.Inheritable);

        // Write a failure result to the shared memory in case the background process
        // fails without writing anything.
        MappedMemoryValue mappedMemoryValue = default;
        using (var mappedFileAccessor = mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Write))
        {
            mappedMemoryValue.HResult = unchecked((int)0x80000008); // E_FAIL
            Debug.WriteLine($"Writing initial value in memory with HResult={mappedMemoryValue.HResult:x}");
            mappedFileAccessor.Write(0, ref mappedMemoryValue);
        }

        // Create an event that the background process will signal to indicate it has completed
        // creating the object and writing it to the shared block.
        using var initEvent = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, initEventName);

        // Create a semaphore that the background process will wait on to keep it alive.
        var completionSemaphore = new Semaphore(initialCount: 0, maximumCount: 1, completionSemaphoreName);
        try
        {
            // Start the elevated process.
            // Command is: <server>.exe <mapped memory name> <event name> <semaphore name>
            var serverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DevHome.SetupFlow.ElevatedServer.exe");

            // We need to start the process with ShellExecute to run elevated
            var processStartInfo = new ProcessStartInfo
            {
                FileName = serverPath,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true,
                Verb = "runas",
                ArgumentList =
                {
                    mappedFileName,
                    initEventName,
                    completionSemaphoreName,
                },
            };

            // Append tasks arguments
            tasksArgumentList.ForEach(arg => processStartInfo.ArgumentList.Add(arg));

            if (isForTesting)
            {
                // For testing we run without ShellExecute so the process output can be inspected.
                // This has the side effect of not running elevated.
                processStartInfo.UseShellExecute = false;
                processStartInfo.Verb = string.Empty;
                processStartInfo.RedirectStandardOutput = true;
            }

            Debug.WriteLine("Starting server process");
            var process = Process.Start(processStartInfo);
            if (process is null)
            {
                Debug.WriteLine("ERROR: " + "Failed to start background process");
                throw new InvalidOperationException("Failed to start background process");
            }

            // Wait for the background process to finish initializing the object and writing
            // it to the shared memory. The timeout is arbitrary and can be changed.
            // We also stop waiting if the process exits or has already exited.
            process.Exited += (_, _) =>
            {
                Debug.WriteLine("Background process exited");
                initEvent.Set();
            };

            if (process.HasExited || !initEvent.WaitOne(60 * 1000))
            {
                Debug.WriteLine("ERROR: " + "Background process failed to initialized in the allowed time");
                throw new TimeoutException("Background process failed to initialized in the allowed time");
            }

            if (process.HasExited)
            {
                Debug.WriteLine("ERROR: " + $"Background process terminated with error code {process.ExitCode}");
                throw new InvalidOperationException("Background process terminated");
            }

            // Read the initialization result and the remote object size
            using (var mappedFileAccessor = mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read))
            {
                mappedFileAccessor.Read(0, out mappedMemoryValue);
                Debug.WriteLine($"Read mapped memory value. HResult: {mappedMemoryValue.HResult:x}");
                Marshal.ThrowExceptionForHR(mappedMemoryValue.HResult);
            }

            // Read the marshalling object
            Marshal.ThrowExceptionForHR(PInvoke.CreateStreamOnHGlobal((HGLOBAL)(nint)0, fDeleteOnRelease: true, out var stream));

            using (var mappedFileAccessor = mappedFile.CreateViewAccessor())
            {
                unsafe
                {
                    // Copy the object into an IStream to use with CoUnmarshalInterface
                    byte* rawPointer = null;
                    uint bytesWritten;
                    try
                    {
                        Debug.WriteLine("Read mapped memory object into stream");
                        mappedFileAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref rawPointer);
                        Marshal.ThrowExceptionForHR(stream.Write(rawPointer + MappedMemoryValueSizeInBytes, (uint)mappedMemoryValue.MarshaledObjectSize, &bytesWritten));
                    }
                    finally
                    {
                        if (rawPointer != null)
                        {
                            mappedFileAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
                        }
                    }

                    if (bytesWritten != mappedMemoryValue.MarshaledObjectSize)
                    {
                        Debug.WriteLine("ERROR: " + "Shared memory stream has unexpected data");
                        throw new InvalidDataException("Shared memory stream has unexpected data");
                    }

                    // Reset the stream to the beginning before reading the object
                    stream.Seek(0, SeekOrigin.Begin);
                }
            }

            Debug.WriteLine("Unmarshaling object from stream data");
            Marshal.ThrowExceptionForHR(PInvoke.CoUnmarshalInterface(stream, GetMarshalInterfaceGUID<T>(), out var obj));
            var value = MarshalInterface<T>.FromAbi(Marshal.GetIUnknownForObject(obj));

            Debug.WriteLine("Returning remote object");
            return (new RemoteObject<T>(value, completionSemaphore), process);
        }
        catch (Exception e)
        {
            Debug.WriteLine("ERROR: " + $"Error occurring while setting up elevated process:", e);

            // Release the "mutex" if there is any error.
            // On success, the mutex will be released after work is done.
            completionSemaphore.Release();
            throw;
        }
    }

    /// <summary>
    /// Completes the remote object initialization from the background process.
    /// This means writing the result to the shared memory block, signaling
    /// to the caller that the initialization is complete, and waiting for
    /// the completion semaphore to be released to signal that we can return.
    /// This is to be called from the elevated background process.
    /// </summary>
    /// <param name="defaultHResult">HResult to write to the shared memory.</param>
    /// <param name="value">
    /// The object to write on the shared memory, can be null when writing a failure.
    /// </param>
#nullable enable
    public static void CompleteRemoteObjectInitialization<T>(
        int defaultHResult,
        T? value,
        string mappedFileName,
        string initEventName,
        string completionSemaphoreName)
    {
        // Open the shared resources
        Debug.WriteLine("INFO:  " + "Opening shared resources");
        var mappedFile = MemoryMappedFile.OpenExisting(mappedFileName, MemoryMappedFileRights.Write);
        var initEvent = EventWaitHandle.OpenExisting(initEventName);
        var completionSemaphore = Semaphore.OpenExisting(completionSemaphoreName);

        MappedMemoryValue mappedMemory = default;

        try
        {
            // Only read the object for non-error cases
            Marshal.ThrowExceptionForHR(defaultHResult);
            if (value is not null)
            {
                unsafe
                {
                    // Write the object into a stream from which will be copied to the shared memory
                    Marshal.ThrowExceptionForHR(PInvoke.CreateStreamOnHGlobal((HGLOBAL)(nint)0, fDeleteOnRelease: true, out var stream));

                    var marshaler = MarshalInterface<T>.CreateMarshaler(value);
                    var marshalerAbi = MarshalInterface<T>.GetAbi(marshaler);

                    Marshal.ThrowExceptionForHR(PInvoke.CoMarshalInterface(stream, GetMarshalInterfaceGUID<T>(), Marshal.GetObjectForIUnknown(marshalerAbi), (uint)MSHCTX.MSHCTX_LOCAL, null, (uint)MSHLFLAGS.MSHLFLAGS_NORMAL));

                    // Store the object size
                    ulong streamSize;
                    stream.Seek(0, SeekOrigin.Current, &streamSize);
                    mappedMemory.MarshaledObjectSize = (long)streamSize;

                    if (mappedMemory.MarshaledObjectSize > MaxRemoteObjectSizeInBytes)
                    {
                        throw new InvalidDataException("Marshaled object is too large for shared memory block");
                    }

                    // Reset the stream to the beginning before reading it to the shared memory.
                    stream.Seek(0, SeekOrigin.Begin);

                    using var mappedFileAccessor = mappedFile.CreateViewAccessor();
                    byte* rawPointer = null;
                    uint bytesRead;
                    try
                    {
                        mappedFileAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref rawPointer);
                        Marshal.ThrowExceptionForHR(stream.Read(rawPointer + MappedMemoryValueSizeInBytes, (uint)streamSize, &bytesRead));
                    }
                    finally
                    {
                        if (rawPointer != null)
                        {
                            mappedFileAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
                        }
                    }

                    if (bytesRead != streamSize)
                    {
                        throw new InvalidDataException("Failed to write marshal object to shared memory");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine("ERROR:  " + $"Error occurred during setup.", e);
            mappedMemory.HResult = e.HResult;
        }

        // Write the init result and if needed the remote object size.
        using (var accessor = mappedFile.CreateViewAccessor())
        {
            Debug.WriteLine("INFO:  " + $"Writing value into shared memory block");
            accessor.Write(0, ref mappedMemory);
        }

        // Signal to the caller that we finished initialization.
        Debug.WriteLine("INFO:  " + "Signaling initialization finished");
        initEvent.Set();

        // Wait until the caller releases the object
        Debug.WriteLine("INFO:  " + "Waiting to receive signal to exit");
        completionSemaphore.WaitOne();

        Debug.WriteLine("INFO:  " + "Exiting");
    }
#nullable disable
}
