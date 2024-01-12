using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public string Translate(string text)
        {
            return app.Translate(text);
        }
    }

}
