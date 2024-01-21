using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModernWindow.Structures
{
    public class MainAppBindings
    {

        public MainApp App;

        public dynamic Globals;
        public dynamic Tools;
        public dynamic Core;

        private static MainAppBindings instance;

        public static MainAppBindings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MainAppBindings();
                }
                return instance;
            }
        }

        private MainAppBindings()
        {
            App = (MainApp)Application.Current;
            Globals = App.Globals;
            Tools = App.Tools;
            Core = App.Core;
        }

        public bool GetSettings(string setting)
        {
            return App.GetSettings(setting);
        }

        public void SetSettings(string setting, bool value)
        {
            App.SetSettings(setting, value);
        }

        public string GetSettingsValue(string setting)
        {
            return App.GetSettingsValue(setting);
        }

        public void SetSettingsValue(string setting, string value)
        {
            App.SetSettingsValue(setting, value);
        }

        public string Translate(string text)
        {
            return App.Translate(text);
        }

        public void RestartApp()
        {
            Console.WriteLine(Environment.GetCommandLineArgs()[0].Replace(".dll", ".exe"));
            System.Diagnostics.Process.Start(Environment.GetCommandLineArgs()[0].Replace(".dll", ".exe"));
            App.DisposeAndQuit();
        }

        public string Which_MachinePath(string command)
        {
            var paths = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine).Split(Path.PathSeparator);
            return paths.FirstOrDefault(x => command.Equals(Path.GetFileName(x), StringComparison.OrdinalIgnoreCase), Path.Join(Environment.GetLogicalDrives()[0], "ThisExe\\WasNotFound\\InPath", command));
        }

        public string Which(string command)
        {
            var paths = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User).Split(Path.PathSeparator);
            return paths.FirstOrDefault(x => command.Equals(Path.GetFileName(x), StringComparison.OrdinalIgnoreCase), Which_MachinePath(command));
        }
    }

}
