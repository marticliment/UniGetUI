using System.Diagnostics;
using System.Runtime.InteropServices;
using UniGetUI.Core.Logging;

namespace UniGetUI;

public class DWMThreadHelper
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

    [Flags]
    private enum ThreadAccess : uint
    {
        QUERY_INFORMATION = 0x0040,
        SUSPEND_RESUME = 0x0002
    }

    private const int ThreadQuerySetWin32StartAddress = 9;
    private static bool DWM_IsSuspended;
    private static bool XAML_IsSuspended;

    public static void ChangeState_DWM(bool suspend)
    {
        if (DWM_IsSuspended && suspend)
        {
            Logger.Warn("DWM Thread was already suspended"); return;
        }
        else if (!DWM_IsSuspended && !suspend)
        {
            Logger.Warn("DWM Thread was already running"); return;
        }

        IntPtr adress = GetTargetFunctionAddress("dwmcorei.dll", 0x54F70);
        if (adress == IntPtr.Zero)
        {
            Logger.Error("Failed to resolve thread start adress."); return;
        }

        ChangeState(suspend, adress, ref DWM_IsSuspended, "DWM");
    }

    public static void ChangeState_XAML(bool suspend)
    {
        if (XAML_IsSuspended && suspend)
        {
            Logger.Warn("XAML Thread was already suspended"); return;
        }
        else if (!XAML_IsSuspended && !suspend)
        {
            Logger.Warn("XAML Thread was already running"); return;
        }

        // The reported offset on ProcessExplorer seems to be missing 0x6280 somehow
        // 0x54F70 + 0x6280 = 0x5B1F0
        IntPtr adress = GetTargetFunctionAddress("Microsoft.UI.Xaml.dll", 0x5B1F0);
        if (adress == IntPtr.Zero)
        {
            Logger.Error("Failed to resolve thread start adress."); return;
        }

        ChangeState(suspend, adress, ref XAML_IsSuspended, "XAML");
    }

    private static void ChangeState(bool suspend, IntPtr threadAdress, ref bool IsSuspended, string loggerName)
    {
        foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
        {
            IntPtr hThread = OpenThread(ThreadAccess.QUERY_INFORMATION | ThreadAccess.SUSPEND_RESUME, false,
                (uint)thread.Id);
            if (hThread == IntPtr.Zero)
                continue;

            IntPtr adress = 0x00;
            int status = NtQueryInformationThread(hThread, ThreadQuerySetWin32StartAddress, ref adress, Marshal.SizeOf(typeof(IntPtr)), out _);

            if (status == 0 && adress == threadAdress)
            {
                if (suspend)
                {
                    uint res = SuspendThread(hThread);
                    if (res == 0)
                    {
                        IsSuspended = true;
                        Logger.Warn($"{loggerName} Thread was suspended successfully");
                        CloseHandle(hThread);
                        return;
                    }
                    else
                    {
                        Logger.Error($"Could not suspend {loggerName} Thread with NTSTATUS = 0x{res:X}");
                    }
                }
                else
                {
                    int res = (int)ResumeThread(hThread);
                    if (res >= 0)
                    {
                        IsSuspended = false;
                        Logger.Warn($"{loggerName} Thread was resumed successfully");
                        CloseHandle(hThread);
                        return;
                    }
                    else
                    {
                        Logger.Error($"Could not resume {loggerName} Thread with NTSTATUS = 0x{res:X}");
                    }
                }
            }

            CloseHandle(hThread);
        }
        Logger.Error($"No thread matching {loggerName} was found");
    }

    private static IntPtr GetTargetFunctionAddress(string moduleName, int offset)
    {
        foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
        {
            if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
            {
                return module.BaseAddress + offset;
            }
        }
        return IntPtr.Zero;
    }
}
