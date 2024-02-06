using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using ModernWindow.Data;
using ModernWindow.PackageEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Audio;
using Windows.Storage.Streams;

namespace ModernWindow.Structures
{
    public class AppTools
    {

        public class __tooltip_options
        {
            private int _errors_occurred = 0;
            public int ErrorsOccurred { get { return _errors_occurred; } set { _errors_occurred = value;AppTools.Instance.App.mainWindow.UpdateSystemTrayStatus() ;  } }
            private bool _restart_required = false;
            public bool RestartRequired { get { return _restart_required; } set { _restart_required = value;AppTools.Instance.App.mainWindow.UpdateSystemTrayStatus();  } }
            private int _operations_in_progress = 0;
            public int OperationsInProgress { get { return _operations_in_progress; } set { _operations_in_progress = value;AppTools.Instance.App.mainWindow.UpdateSystemTrayStatus();  } }
            private int _available_updates = 0;
            public int AvailableUpdates { get { return _available_updates; } set { _available_updates = value; AppTools.Instance.App.mainWindow.UpdateSystemTrayStatus(); } }
        }


        public MainApp App;

        public ThemeListener ThemeListener;
        public List<AbstractOperation> OperationQueue = new();

        public __tooltip_options TooltipStatus = new __tooltip_options();

        private LanguageEngine LanguageEngine = new LanguageEngine();

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

        public bool GetSettings(string setting, bool invert = false)
        { return AppTools.GetSettings_Static(setting, invert); }

        public static bool GetSettings_Static(string setting, bool invert = false)
        {
            return File.Exists(Path.Join(CoreData.WingetUIDataDirectory, setting)) ^ invert;
        }

        public void SetSettings(string setting, bool value)
        { AppTools.SetSettings_Static(setting, value); }

        public static void SetSettings_Static(string setting, bool value)
        {
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
            if (!File.Exists(Path.Join(CoreData.WingetUIDataDirectory, setting)))
                return "";
            return File.ReadAllText(Path.Join(CoreData.WingetUIDataDirectory, setting));
        }
        public void SetSettingsValue(string setting, string value)
        { AppTools.SetSettingsValue_Static(setting, value); }

        public static void SetSettingsValue_Static(string setting, string value)
        {
            File.WriteAllText(Path.Join(CoreData.WingetUIDataDirectory, setting), value);
        }

        public string Translate(string text)
        {
            return LanguageEngine.Translate(text);
        }

        public void RestartApp()
        {
            Console.WriteLine(Environment.GetCommandLineArgs()[0].Replace(".dll", ".exe"));
            System.Diagnostics.Process.Start(Environment.GetCommandLineArgs()[0].Replace(".dll", ".exe"));
            App.DisposeAndQuit();
        }

        public async Task<string> Which(string command)
        {
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = "/C where " + command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var line = await process.StandardOutput.ReadLineAsync();
            string output;
            if (line == null)
                output = "";
            else
                output = line.Trim();
            await process.WaitForExitAsync();
            if(process.ExitCode != 0 || output == "")
                return Path.Join(Environment.GetLogicalDrives()[0], "ThisExe\\WasNotFound\\InPath", command);
            else
                return output;
        }

        public string FormatAsName(string name)
        {
            name = name.Replace(".install", "").Replace(".portable", "").Replace("-", " ").Replace("_", " ").Split("/")[^1];
            string newName = "";
            for(int i = 0; i<name.Length; i++)
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
        
    }

}
