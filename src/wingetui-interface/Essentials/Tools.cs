using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using ModernWindow.Data;
using ModernWindow.PackageEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace ModernWindow.Structures
{
    public class AppTools
    {

        public class __tooltip_options
        {
            private int _errors_occurred = 0;
            public int ErrorsOccurred { get { return _errors_occurred; } set { _errors_occurred = value; AppTools.Instance.App.mainWindow.UpdateSystemTrayStatus(); } }
            private bool _restart_required = false;
            public bool RestartRequired { get { return _restart_required; } set { _restart_required = value; AppTools.Instance.App.mainWindow.UpdateSystemTrayStatus(); } }
            private int _operations_in_progress = 0;
            public int OperationsInProgress { get { return _operations_in_progress; } set { _operations_in_progress = value; AppTools.Instance.App.mainWindow.UpdateSystemTrayStatus(); } }
            private int _available_updates = 0;
            public int AvailableUpdates { get { return _available_updates; } set { _available_updates = value; AppTools.Instance.App.mainWindow.UpdateSystemTrayStatus(); } }
        }


        public MainApp App;

        public ThemeListener ThemeListener;
        public List<AbstractOperation> OperationQueue = new();

        public __tooltip_options TooltipStatus = new();

        private LanguageEngine LanguageEngine = new();

        private static AppTools instance;

        public static AppTools Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AppTools();
                }
                return instance;
            }
        }

        private AppTools()
        {
            App = (MainApp)Application.Current;
            ThemeListener = new ThemeListener();
        }

        public static void EnsureTempDir()
        {
            if (!Directory.Exists(CoreData.WingetUIDataDirectory))
                Directory.CreateDirectory(CoreData.WingetUIDataDirectory);
        }

        public bool GetSettings(string setting, bool invert = false)
        { return AppTools.GetSettings_Static(setting, invert); }

        public static bool GetSettings_Static(string setting, bool invert = false)
        {
            EnsureTempDir();
            return File.Exists(Path.Join(CoreData.WingetUIDataDirectory, setting)) ^ invert;
        }

        public void SetSettings(string setting, bool value)
        { AppTools.SetSettings_Static(setting, value); }

        public static void SetSettings_Static(string setting, bool value)
        {
            EnsureTempDir();
            if (value)
            {
                if (!File.Exists(Path.Join(CoreData.WingetUIDataDirectory, setting)))
                    File.WriteAllText(Path.Join(CoreData.WingetUIDataDirectory, setting), "");
            }
            else
            {
                if (File.Exists(Path.Join(CoreData.WingetUIDataDirectory, setting)))
                    File.Delete(Path.Join(CoreData.WingetUIDataDirectory, setting));
            }
        }
        public string GetSettingsValue(string setting)
        { return AppTools.GetSettingsValue_Static(setting); }

        public static string GetSettingsValue_Static(string setting)
        {
            EnsureTempDir();
            if (!File.Exists(Path.Join(CoreData.WingetUIDataDirectory, setting)))
                return "";
            return File.ReadAllText(Path.Join(CoreData.WingetUIDataDirectory, setting));
        }
        public void SetSettingsValue(string setting, string value)
        { AppTools.SetSettingsValue_Static(setting, value); }

        public static void SetSettingsValue_Static(string setting, string value)
        {
            EnsureTempDir();
            File.WriteAllText(Path.Join(CoreData.WingetUIDataDirectory, setting), value);
        }

        public string Translate(string text)
        {
            return LanguageEngine.Translate(text);
        }

        public void RestartApp()
        {
            AppTools.Log(Environment.GetCommandLineArgs()[0].Replace(".dll", ".exe"));
            System.Diagnostics.Process.Start(Environment.GetCommandLineArgs()[0].Replace(".dll", ".exe"));
            App.DisposeAndQuit();
        }

        public async Task<string> Which(string command)
        {
            Process process = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = "/C where " + command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string line = await process.StandardOutput.ReadLineAsync();
            string output;
            if (line == null)
                output = "";
            else
                output = line.Trim();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0 || output == "")
                return Path.Join(Environment.GetLogicalDrives()[0], "ThisExe\\WasNotFound\\InPath", command);
            else
                return output;
        }

        public string FormatAsName(string name)
        {
            name = name.Replace(".install", "").Replace(".portable", "").Replace("-", " ").Replace("_", " ").Split("/")[^1];
            string newName = "";
            for (int i = 0; i < name.Length; i++)
            {
                if (i == 0 || name[i - 1] == ' ')
                    newName += name[i].ToString().ToUpper();
                else
                    newName += name[i];
            }
            return newName;
        }

        public void AddOperationToList(AbstractOperation operation)
        {
            App.mainWindow.NavigationPage.OperationStackPanel.Children.Add(operation);
        }

        public static void Log(string s)
        {
            CoreData.WingetUILog += s + "\n";
            Debug.WriteLine(s);
        }

        public static void Log(Exception e)
        { Log(e.ToString()); }

        public static void Log(object o)
        { Log(o.ToString()); }


        public static void LogManagerOperation(PackageManager manager, Process process, string output)
        {
            output = Regex.Replace(output, "\n.{0,6}\n", "\n");
            CoreData.ManagerLogs += $"=========================================\n";
            CoreData.ManagerLogs += $"[{DateTime.Now}] {manager.Name} - Arguments: {process.StartInfo.Arguments}\n";
            CoreData.ManagerLogs += $"       Executable: {process.StartInfo.FileName}\n";
            CoreData.ManagerLogs += "\n";
            CoreData.ManagerLogs += output;
            CoreData.ManagerLogs += "\n";
            CoreData.ManagerLogs += $"[{DateTime.Now}] Exit Code: {process.ExitCode}";
            CoreData.ManagerLogs += "\n";
            CoreData.ManagerLogs += "\n";
            CoreData.ManagerLogs += $"=========================================\n";
        }

        public static void ReportFatalException(Exception e)
        {
            string LangName = "Unknown";
            try
            {
                LangName = LanguageEngine.MainLangDict["langName"];
            }
            catch { }

            string Error_String = $@"
                        OS: {Environment.OSVersion.Platform}
                   Version: {Environment.OSVersion.VersionString}
           OS Architecture: {Environment.Is64BitOperatingSystem}
          APP Architecture: {Environment.Is64BitProcess}
                  Language: {LangName}
               APP Version: {CoreData.VersionName}
                Executable: {Environment.ProcessPath}

Crash Message: {e.Message}

Crash Traceback: 
{e.StackTrace}";

            Console.WriteLine(Error_String);


            string ErrorBody = "https://www.marticliment.com/error-report/?appName=WingetUI^&errorBody=" + Uri.EscapeDataString(Error_String.Replace("\n", "{l}"));

            Console.WriteLine(ErrorBody);

            using System.Diagnostics.Process cmd = new();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = false;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            cmd.StandardInput.WriteLine("start " + ErrorBody);
            cmd.StandardInput.WriteLine("exit");
            cmd.WaitForExit();
            Environment.Exit(1);

        }

        public static async void LaunchBatchFile(string path, string WindowTitle = "", bool RunAsAdmin = false)
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = "/C start \"" + WindowTitle + "\" \"" + path + "\"";
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.Verb = RunAsAdmin? "runas": "";
            p.Start();
            await p.WaitForExitAsync();
        }
    }

}
