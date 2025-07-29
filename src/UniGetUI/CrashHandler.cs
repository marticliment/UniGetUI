using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using Microsoft.UI.Xaml.Markup;
using UniGetUI.Core.Data;
using UniGetUI.Core.Tools;

namespace UniGetUI;

public static class CrashHandler
{
    private const uint MB_ICONSTOP = 0x00000010;
    private const uint MB_OKCANCEL = 0x00000001;
    private const int IDCANCEL = 2;

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    private static bool _reportMissingFiles()
    {
        try
        {
            var errorMessage = "UniGetUI has detected that some required files are missing."
                             + "\n\nThis might be caused by an incomplete installation or corrupted files. Please reinstall UniGetUI."
                             + "\n\nPress CANCEL to get more details about the crash.";

            var title = "UniGetUI - Missing Files";

            return MessageBox(IntPtr.Zero,  errorMessage, title, MB_ICONSTOP | MB_OKCANCEL) is not IDCANCEL;
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
                if ((uint)fileEx.HResult is 0x80070002 or 0x8007007E)
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

        string GetExceptionData(Exception e)
        {
            try
            {
                StringBuilder b = new();
                foreach (var key in e.Data.Keys)
                {
                    b.AppendLine($"{key}: {e.Data[key]}");
                }

                string r = b.ToString();
                return r.Any()? r: "No extra data was provided";
            }
            catch (Exception ex)
            {
                return $"Failed to get exception Data with exception {ex.Message}";
            }
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

                Crash Data:
                    {{GetExceptionData(e).Replace("\n", "\n        ")}}

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

                        Crash Data:
                            {{GetExceptionData(e).Replace("\n", "\n        ")}}

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

        Error_String += "\n\n -------------------------------------- \n\n";

        try
        {
            var integrityReport = IntegrityTester.CheckIntegrityAsync().GetAwaiter().GetResult();
            if (integrityReport.Passed)
            {
                Error_String += "No corrupted files were found";
            }
            else
            {
                if (integrityReport.MissingFiles.Any())
                {
                    Error_String += "Missing files: \n - " + string.Join("\n - ", integrityReport.MissingFiles) + "\n\n";
                }
                if (integrityReport.CorruptedFiles.Any())
                {
                    var list = integrityReport.CorruptedFiles.Select((k) =>
                        $" - {k.Key}: (found {k.Value.Got} instead of {k.Value.Expected})");

                    Error_String += "Corrupted files: \n - " + string.Join("\n", list) + "\n\n";
                }
            }
        }
        catch (Exception ex)
        {
            Error_String += "Failed to compute integrity report: ";
            Error_String +=  ex.GetType() + ": " + ex.Message;
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
