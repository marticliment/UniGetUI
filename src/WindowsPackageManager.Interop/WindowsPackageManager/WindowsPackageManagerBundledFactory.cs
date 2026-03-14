using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using WinRT;

namespace WindowsPackageManager.Interop;

[SupportedOSPlatform("windows5.0")]
public sealed class WindowsPackageManagerBundledFactory : WindowsPackageManagerFactory
{
    private static readonly Lazy<NativeExports> Native = new(LoadNativeExports);

    public string LibraryPath => Native.Value.LibraryPath;

    public WindowsPackageManagerBundledFactory(ClsidContext clsidContext = ClsidContext.Prod)
        : base(clsidContext) { }

    protected override T CreateInstance<T>(Guid clsid, Guid iid)
    {
        IntPtr instancePointer;

        try
        {
            int errorCode = Native.Value.CreateInstance(clsid, iid, out instancePointer);
            if (errorCode < 0)
            {
                throw new WinGetComActivationException(clsid, iid, errorCode, false);
            }

            return MarshalGeneric<T>.FromAbi(instancePointer);
        }
        catch (COMException ex) when (ex.HResult < 0)
        {
            throw new WinGetComActivationException(clsid, iid, ex.HResult, false);
        }
    }

    private static NativeExports LoadNativeExports()
    {
        string libraryPath = ResolveBundledLibraryPath();
        IntPtr moduleHandle = LoadLibraryEx(libraryPath, IntPtr.Zero, LoadWithAlteredSearchPath);
        if (moduleHandle == IntPtr.Zero)
        {
            int errorCode = Marshal.GetLastPInvokeError();
            throw new InvalidOperationException(
                $"Failed to load bundled WindowsPackageManager.dll from '{libraryPath}' (Win32 error {errorCode})."
            );
        }

        var initialize = GetExport<InitializeServerDelegate>(
            moduleHandle,
            nameof(WindowsPackageManagerServerInitialize)
        );
        var createInstance = GetExport<CreateInstanceDelegate>(
            moduleHandle,
            nameof(WindowsPackageManagerServerCreateInstance)
        );

        int errorCodeFromInitialize = initialize();
        if (errorCodeFromInitialize < 0)
        {
            throw new InvalidOperationException(
                $"Failed to initialize bundled WindowsPackageManager in-proc module (HRESULT 0x{errorCodeFromInitialize:X8})."
            );
        }

        return new NativeExports(moduleHandle, libraryPath, createInstance);
    }

    private static string ResolveBundledLibraryPath()
    {
        string baseDirectory = AppContext.BaseDirectory;
        string architectureFolder = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "winget-cli_arm64",
            Architecture.X64 => "winget-cli_x64",
            Architecture.X86 => "winget-cli_x86",
            _ => "winget-cli_x64",
        };

        string[] candidates =
        [
            Path.Combine(baseDirectory, architectureFolder, "WindowsPackageManager.dll"),
            Path.Combine(baseDirectory, "winget-cli_x64", "WindowsPackageManager.dll"),
            Path.Combine(baseDirectory, "WindowsPackageManager.dll"),
        ];

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("Bundled WindowsPackageManager.dll was not found.");
    }

    private static TDelegate GetExport<TDelegate>(IntPtr moduleHandle, string exportName)
        where TDelegate : Delegate
    {
        IntPtr exportPointer = GetProcAddress(moduleHandle, exportName);
        if (exportPointer == IntPtr.Zero)
        {
            throw new EntryPointNotFoundException(
                $"Export '{exportName}' was not found in bundled WindowsPackageManager.dll."
            );
        }

        return Marshal.GetDelegateForFunctionPointer<TDelegate>(exportPointer);
    }

    private sealed record NativeExports(
        IntPtr ModuleHandle,
        string LibraryPath,
        CreateInstanceDelegate CreateInstance
    );

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int InitializeServerDelegate();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CreateInstanceDelegate(in Guid clsid, in Guid iid, out IntPtr instance);

    private const uint LoadWithAlteredSearchPath = 0x00000008;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibraryEx(
        string lpFileName,
        IntPtr hFile,
        uint dwFlags
    );

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    private static string WindowsPackageManagerServerInitialize =>
        "WindowsPackageManagerServerInitialize";

    private static string WindowsPackageManagerServerCreateInstance =>
        "WindowsPackageManagerServerCreateInstance";
}
