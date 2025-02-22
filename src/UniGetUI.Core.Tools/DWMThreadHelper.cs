using System.Diagnostics;
using System.Runtime.InteropServices;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;

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

    private static int? DWMThreadId;
    private static IntPtr? DWMThreadAdress;
    private static int? XAMLThreadId;
    private static IntPtr? XAMLThreadAdress;

    public static void ChangeState_DWM(bool suspend)
    {
        if (Settings.Get("DisableDMWThreadOptimizations")) return;

        if (DWM_IsSuspended && suspend)
        {
            Logger.Debug("DWM Thread was already suspended"); return;
        }
        else if (!DWM_IsSuspended && !suspend)
        {
            Logger.Debug("DWM Thread was already running"); return;
        }

        DWMThreadAdress ??= GetTargetFunctionAddress("dwmcorei.dll", 0x54F70);
        if (DWMThreadAdress is null)
        {
            Logger.Error("Failed to resolve thread start adress."); return;
        }

        ChangeState(suspend, (IntPtr)DWMThreadAdress, ref DWM_IsSuspended, ref DWMThreadId, "DWM");
    }

    public static void ChangeState_XAML(bool suspend)
    {
        if (Settings.Get("DisableDMWThreadOptimizations")) return;

        if (XAML_IsSuspended && suspend)
        {
            Logger.Debug("XAML Thread was already suspended"); return;
        }
        else if (!XAML_IsSuspended && !suspend)
        {
            Logger.Debug("XAML Thread was already running"); return;
        }

        // The reported offset on ProcessExplorer seems to be missing 0x6280 somehow
        // 0x54F70 + 0x6280 = 0x5B1F0
        XAMLThreadAdress ??= GetTargetFunctionAddress("Microsoft.UI.Xaml.dll", 0x5B1F0);
        if (XAMLThreadAdress is null)
        {
            Logger.Error("Failed to resolve thread start adress."); return;
        }

        ChangeState(suspend, (IntPtr)XAMLThreadAdress, ref XAML_IsSuspended, ref XAMLThreadId, "XAML");
    }

    private static IntPtr GetThreadStartAdress(int threadId)
    {
        IntPtr hThread = OpenThread(ThreadAccess.QUERY_INFORMATION | ThreadAccess.SUSPEND_RESUME, false, (uint)threadId);
        if (hThread == IntPtr.Zero) return IntPtr.Zero;

        IntPtr adress = 0x00;
        int status = NtQueryInformationThread(hThread, ThreadQuerySetWin32StartAddress, ref adress, Marshal.SizeOf(typeof(IntPtr)), out _);
        if(status != 0) Logger.Warn($"NtQueryInformationThread returned non-zero status code 0x{(uint)status:X}");
        CloseHandle(hThread);
        return adress;
    }

    private static void ChangeState(bool suspend, IntPtr threadAdress, ref bool IsSuspended, ref int? threadId,
        string loggerName, bool canRetry = true)
    {
        if (threadId is null)
        {
            foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
            {
                if (GetThreadStartAdress(thread.Id) == threadAdress)
                {
                    threadId = thread.Id;
                    Logger.Info($"Thread with Id {threadId} was assigned as {loggerName} thread");
                }
            }
        }

        if (threadId is null)
        {
            Logger.Error($"No thread matching {loggerName} with start adress {threadAdress:X} was found");
            return;
        }

        IntPtr hThread = OpenThread(ThreadAccess.QUERY_INFORMATION | ThreadAccess.SUSPEND_RESUME, false, (uint)threadId);
        if (hThread == IntPtr.Zero)
        {   // When the thread cannot be opened
            if (canRetry)
            {
                threadId = null; // On first try, reset argument threadId so it does get loaded again.
                ChangeState(suspend, threadAdress, ref IsSuspended, ref threadId, loggerName, false);
                return;
            }
            // The threadId was already reloaded
            Logger.Warn($"Thread with id={threadId} and assigned as {loggerName} exists but could not be opened!");
            return;
        }


        if (suspend)
        {
            uint res = SuspendThread(hThread);
            if (res == 0)
            {
                IsSuspended = true;
                Logger.Info($"{loggerName} Thread was suspended successfully");
                CloseHandle(hThread);
                return;
            }
            else
            {
                Logger.Warn($"Could not suspend {loggerName} Thread with NTSTATUS = 0x{res:X}");
            }
        }
        else
        {
            int res = (int)ResumeThread(hThread);
            if (res >= 0)
            {
                IsSuspended = false;
                Logger.Info($"{loggerName} Thread was resumed successfully");
                CloseHandle(hThread);
                return;
            }
            else
            {
                Logger.Error($"Could not resume {loggerName} Thread with NTSTATUS = 0x{res:X}");
            }
        }

        CloseHandle(hThread);
    }

    private static IntPtr? GetTargetFunctionAddress(string moduleName, int offset)
    {
        foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
        {
            if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
            {
                return module.BaseAddress + offset;
            }
        }
        return null;
    }
}
