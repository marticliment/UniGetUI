using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public ThemeListener ThemeListener;

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
            ThemeListener = new ThemeListener();
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
            string output = (await process.StandardOutput.ReadLineAsync()).Trim();
            await process.WaitForExitAsync();
            if(process.ExitCode != 0 || output == "")
                return Path.Join(Environment.GetLogicalDrives()[0], "ThisExe\\WasNotFound\\InPath", command);
            else
                return output;
        }

        public string FormatAsName(string name)
        {
            name = name.Replace("-", " ").Replace(".", " ").Replace("(", " ").Replace(")", " ").Replace("/", " ").Replace("\\", " ").Replace(":", " ").Replace(";", " ").Replace(",", " ").Replace("'", " ").Replace("_", " ").Replace("?", " ").Replace("!", " ").Replace("=", " ").Replace("+", " ").Replace("*", " ").Replace("&", " ").Replace("^", " ").Replace("%", " ").Replace("$", " ").Replace("#", " ").Replace("@", " ");
            string newName = "";
            for(int i = 0; i<name.Length; i++)
            {
                if (i == 0 || name[i - 1] == ' ')
                    newName += name[i].ToString().ToUpper();
                else 
                    newName += name[i];
            }
            return name;
        }
    }

}
