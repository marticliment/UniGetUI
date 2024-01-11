using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static System.Formats.Asn1.AsnWriter;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            settings = new SettingsInterface();
            Console.WriteLine("Settings Window Handle: " + settings.GetHwnd());

            Thread python = new Thread(LoadPython);
            python.SetApartmentState(ApartmentState.STA);
            python.Start();

        }

        [STAThread]
        void LoadPython()
        {
            Runtime.PythonDLL = @"C:\Users\marti\AppData\Local\Programs\Python\Python311\Python311.dll";
            PythonEngine.Initialize();
            PythonEngine.BeginAllowThreads();
            using (Py.GIL())
            {
                using (PyModule scope = Py.CreateScope())
                {
                    scope.Set("CSharpApp", this.ToPython());
                    
                    string json_options = @"{""settings_window_handle"": " + settings.GetHwnd() + "}";
                    using var locals = new PyDict();
                    locals["CSharpApp"] = this.ToPython();

                    PythonEngine.Exec("import os\n"
                        + "os.environ['WINGETUI_OPTIONS'] = '" + json_options + "'\n"
                        + "import wingetui.Core.Globals\n"
                        + "wingetui.Core.Globals.CSharpApp = CSharpApp\n"
                        + "import wingetui.__main__\n", null, locals);


                }
            }
        }

    public SettingsInterface settings;
    }
}
