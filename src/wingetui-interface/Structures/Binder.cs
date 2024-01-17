using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using Python.Runtime;
using System;
using System.Collections.Generic;
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
        public dynamic PackageClasses;
        public dynamic PackageTools;

        public MainAppBindings()
        {
            App = (MainApp)Application.Current;
            Globals = App.Globals;
            Tools = App.Tools;
            Core = App.Core;
            PackageClasses = App.PackageClasses;
            PackageTools = App.PackageTools;
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
    }

}
