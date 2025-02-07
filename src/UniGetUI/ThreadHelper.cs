using System.Diagnostics;
using System.Runtime.InteropServices;
using UniGetUI.Core.Logging;

namespace UniGetUI;

class ThreadHelper
{
    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationThread(IntPtr ThreadHandle, int ThreadInformationClass,
        ref IntPtr ThreadInformation, int ThreadInformationLength, out int ReturnLength);

    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

    [DllImport("kernel32.dll")]
    private static extern uint SuspendThread(IntPtr hThread);

    [DllImport("kernel32.dll")]
    private static extern uint ResumeThread(IntPtr hThread);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct THREAD_BASIC_INFORMATION
    {
        public IntPtr ExitStatus;
        public IntPtr TebBaseAddress;
        public uint ProcessId;   // Change to uint
        public uint ThreadId;    // Change to uint
        public IntPtr AffinityMask;
        public int Priority;     // Change to int
        public int BasePriority; // Change to int
        public IntPtr StartAddress;
    }


    [Flags]
    private enum ThreadAccess : uint
    {
        QUERY_INFORMATION = 0x0040,
        SUSPEND_RESUME = 0x0002
    }

    public static void HandleDWMThread(bool enable)
    {
        IntPtr DWMtargetStartAddress = GetTargetFunctionAddress("dwmcorei.dll", 0x54F70);
        IntPtr UGUItargetStartAddress = GetTargetFunctionAddress("UniGetUI.exe", 0x11240);

        if (DWMtargetStartAddress == IntPtr.Zero)
        {
            Logger.Error("Failed to resolve target function address.");
            return;
        }
        if (UGUItargetStartAddress == IntPtr.Zero)
        {
            Logger.Error("Failed to resolve target function address.");
            return;
        }

        foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
        {
            IntPtr hThread = OpenThread(ThreadAccess.QUERY_INFORMATION | ThreadAccess.SUSPEND_RESUME, false,
                (uint)thread.Id);
            if (hThread == IntPtr.Zero)
                continue;

            IntPtr adress = 0x00;
            int status = NtQueryInformationThread(hThread, 9, ref adress, Marshal.SizeOf(typeof(IntPtr)), out _);
            if (status == 0)
            {
                if (adress == DWMtargetStartAddress) {
                    if (enable) {
                        Logger.Warn("Resuming DWM thread!");
                        ResumeThread(hThread);
                    } else {
                        Logger.Warn("Suspending DWM thread!");
                        SuspendThread(hThread);
                    }
                } /*else if (adress == UGUItargetStartAddress) {
                    if (enable) {
                        Logger.Warn("Resuming UniGetUI thread!");
                        ResumeThread(hThread);
                    } else {
                        Logger.Warn("Suspending UniGetUI thread!");
                        SuspendThread(hThread);
                    }
                }*/
            }

        CloseHandle(hThread);
        }
    }

    static IntPtr GetTargetFunctionAddress(string moduleName, int offset)
    {
        foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
        {
            if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
            {
                return module.BaseAddress + offset;
            }
        }
        return IntPtr.Zero; // Module not found
    }
}
