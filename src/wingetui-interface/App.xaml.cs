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
using System.Diagnostics;
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

namespace ModernWindow
{
    public partial class MainApp : Application
    {
        
        // Python modules to be imported
        private dynamic Tools;
        private dynamic Data;
        private Py.GILState GIL;

        // Windows (MUST BE PUBLIS FOR PYTHON TO ACCESS)
        public SettingsTab.MainInterface settings;


        public MainApp()
        {
            this.InitializeComponent();

            // Load Python runtime
            Runtime.PythonDLL = @"C:\Users\marti\AppData\Local\Programs\Python\Python311\Python311.dll";
            PythonEngine.Initialize();
            PythonEngine.BeginAllowThreads();

            Debug.WriteLine("Python Runtime Loaded");

            // Import Python modules
            GIL = Py.GIL();
            Tools = (PyModule)Py.Import("wingetui.Core.Tools");
            Data = (PyModule)Py.Import("wingetui.Core.Data");

            Debug.WriteLine("Python modules imported");

            // Initialize Windows
            settings = new SettingsTab.MainInterface(this);
            settings.Activate();

            Debug.WriteLine("All windows loaded");

            Thread python = new Thread(LoadPython);
            python.SetApartmentState(ApartmentState.STA);
            python.Start();
        }

        // setSettings binding
        public void SetSettings(string setting, bool value)
        {
            this.Tools.setSettings(setting, value);
        }

        // getSettings binding
        public bool GetSettings(string setting)
        {
            return (bool)this.Tools.getSettings(setting);
        }


        // Python code loader - Use STA. Otherwise, Qt will crash.
        [STAThread]
        void LoadPython()
        {
            PythonEngine.Initialize();
            PythonEngine.BeginAllowThreads();
            using (Py.GIL())
            {
                using (PyModule scope = Py.CreateScope())
                {
                    scope.Set("CSharpApp", this.ToPython());
                    
                    using var locals = new PyDict();
                    locals["CSharpApp"] = this.ToPython();

                    PythonEngine.Exec("\n"
                        + "import wingetui.Core.Globals\n"
                        + "wingetui.Core.Globals.CSharpApp = CSharpApp\n"
                        + "import wingetui.__main__\n", null, locals);


                }
            }
        }


        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
        }

    }
}
