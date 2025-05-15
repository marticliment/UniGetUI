using System.Diagnostics;
using Microsoft.UI;
using UniGetUI.Core.Data;
using UniGetUI.Core.Language;
using UniGetUI.Core.Tools;

namespace UniGetUI;

public static class CrashHandler
{
    private const uint MB_ICONSTOP = 0x00000010;

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    private static bool _reportMissingFiles()
    {
        try
        {
            var errorMessage = "UniGetUI has detected that some required files are missing."
                             + "\n\nThis might be caused by an incomplete installation or corrupted files. Please reinstall UniGetUI."
                             + "\n\nRun UniGetUI with the parameter '--no-corrupt-dialog' to get more details about the crash.";

            var title = "UniGetUI - Missing Files";

            MessageBox(IntPtr.Zero,  errorMessage, title, MB_ICONSTOP);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void ReportFatalException(Exception e)
    {
        Debugger.Break();


        if (!Environment.GetCommandLineArgs().Contains(CLIHandler.NO_CORRUPT_DIALOG))
        {
            Exception? fileEx = e;
            while (fileEx is not null)
            {
                if (fileEx.ToString().Contains("Could not load file or assembly"))
                {
                    if (_reportMissingFiles())
                    {
                        Environment.Exit(1);
                    }
                }
                fileEx = fileEx.InnerException;
            }
        }

        string LangName = "Unknown";
        try
        {
            LangName = CoreTools.GetCurrentLocale();
        }
        catch
        {
            // ignored
        }

        string Error_String = $$"""
            Environment details:
                    Windows version: {{Environment.OSVersion.VersionString}}
                    Language: {{LangName}}
                    APP Version: {{CoreData.VersionName}}
                    APP Build number: {{CoreData.BuildNumber}}
                    Executable: {{Environment.ProcessPath}}
                    Command-line arguments: {{Environment.CommandLine}}

            Exception details:
                Crash HResult: 0x{{(uint)e.HResult:X}} ({{(uint)e.HResult}}, {{e.HResult}})
                Crash Message: {{e.Message}}

                Crash Trace:
                    {{e.StackTrace?.Replace("\n", "\n        ")}}
            """;

        try
        {
            int i = 0;
            while (e.InnerException is not null)
            {
                i++;
                e = e.InnerException;
                Error_String += "\n\n\n\n" + $$"""
                    ———————————————————————————————————————————————————————————
                    Inner exception details (depth level: {{i}})
                        Crash HResult: 0x{{(uint)e.HResult:X}} ({{(uint)e.HResult}}, {{e.HResult}})
                        Crash Message: {{e.Message}}

                        Crash Traceback:
                            {{e.StackTrace?.Replace("\n", "\n        ")}}
                    """;
            }

            if (i == 0)
            {
                Error_String += $"\n\n\nNo inner exceptions found";
            }
        } catch
        {
            // ignore
        }

        Console.WriteLine(Error_String);

        string ErrorUrl = $"https://www.marticliment.com/error-report/" +
              $"?appName=UniGetUI" +
              $"&appVersion={Uri.EscapeDataString(CoreData.VersionName)}" +
              $"&buildNumber={Uri.EscapeDataString(CoreData.BuildNumber.ToString())}" +
              $"&errorBody={Uri.EscapeDataString(Error_String)}";
        Console.WriteLine(ErrorUrl);

        using Process p = new();
        p.StartInfo.FileName = ErrorUrl;
        p.StartInfo.CreateNoWindow = true;
        p.StartInfo.UseShellExecute = true;
        p.Start();
        Environment.Exit(1);
    }
}
