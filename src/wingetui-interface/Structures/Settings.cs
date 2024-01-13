using Microsoft.UI.Xaml;
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

        private MainApp app;
        public MainAppBindings()
        {
            app = (MainApp)Application.Current;
        }

        public bool GetSettings(string setting)
        {
            return app.GetSettings(setting);
        }

        public void SetSettings(string setting, bool value)
        {
            app.SetSettings(setting, value);
        }

        public string GetSettingsValue(string setting)
        {
            return app.GetSettingsValue(setting);
        }

        public void SetSettingsValue(string setting, string value)
        {
            app.SetSettingsValue(setting, value);
        }

        public string Translate(string text)
        {
            return app.Translate(text);
        }

        public void RestartApp()
        {
            Console.WriteLine(Environment.GetCommandLineArgs()[0].Replace(".dll", ".exe"));
            System.Diagnostics.Process.Start(Environment.GetCommandLineArgs()[0].Replace(".dll", ".exe"));
            app.DisposeAndQuit();
    }
    }

}
