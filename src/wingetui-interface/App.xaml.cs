using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using ModernWindow.PackageEngine;
using ModernWindow.PackageEngine.Managers;
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
using Windows.System;

namespace ModernWindow
{
    public partial class MainApp : Application
    {
        
        // Python modules to be imported
        public dynamic Tools;
        public dynamic Core;
        public dynamic Globals;

        public Scoop Scoop;
        public Winget Winget;
        public Chocolatey Choco;
        public Pip Pip;
        public Npm Npm;
        public Dotnet Dotnet;
        public PowerShell Powershell;

        public List<PackageManager> PackageManagerList = new List<PackageManager>();

        private Py.GILState GIL;

        public Interface.SettingsInterface settings;
        public MainWindow mainWindow;


        public MainApp()
        {
            this.InitializeComponent();
            
            Runtime.PythonDLL = @"C:\Users\marti\AppData\Local\Programs\Python\Python311\Python311.dll";
            PythonEngine.Initialize();
            PythonEngine.BeginAllowThreads();

            Debug.WriteLine("Python Runtime Loaded");
            Debug.WriteLine("Current Path: " + Environment.CurrentDirectory);

            // Import Python modules
            GIL = Py.GIL();

            dynamic os = Py.Import("os");
            dynamic sys = Py.Import("sys");
            sys.path.append(os.getcwd());

            Globals = (PyModule)Py.Import("wingetui.Core.Globals");
            Tools = (PyModule)Py.Import("wingetui.Core.Tools");
            Core = (PyModule)Py.Import("wingetui.Core");

            Debug.WriteLine("Python modules imported");

            mainWindow = new MainWindow();
            mainWindow.Activate();

            var hWnd = mainWindow.GetWindowHandle();

            Microsoft.UI.WindowId windowId =
                Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

            // Lastly, retrieve the AppWindow for the current (XAML) WinUI 3 window.
            Microsoft.UI.Windowing.AppWindow appWindow =
                Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                appWindow.Closing += mainWindow.HandleClosingEvent;
            }


            LoadComponents();
        }

        public async void LoadComponents()
        {             
            mainWindow.BlockLoading = true;
            mainWindow.Closed += (sender, args) => { DisposeAndQuit(0); };

            await Task.Delay(100);

            // Load managers
            
            Winget = new Winget();
            PackageManagerList.Add(Winget);
            Scoop = new Scoop();
            PackageManagerList.Add(Scoop);
            Choco = new Chocolatey();
            PackageManagerList.Add(Choco);
            Pip = new Pip();
            PackageManagerList.Add(Pip);
            Npm = new Npm();
            PackageManagerList.Add(Npm);
            Dotnet = new Dotnet();
            PackageManagerList.Add(Dotnet);
            Powershell = new PowerShell();
            PackageManagerList.Add(Powershell);

            foreach(PackageManager manager in PackageManagerList)
                _ = manager.Initialize();

            foreach(PackageManager manager in PackageManagerList)
            {
                while(!manager.ManagerReady){
                    await Task.Delay(100);
                    Console.WriteLine("Waiting for manager " + manager.Name);
                }
                Console.WriteLine(manager.Name + " ready");
            }


            Debug.WriteLine("All managers loaded");

            mainWindow.SwitchToInterface();
            // settings = mainWindow.SettingsTab;


            Thread python = new Thread(LoadPython);
            python.SetApartmentState(ApartmentState.STA);
            bool run_python = false;
            if (run_python)
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

        // setSettingsValue binding
        public void SetSettingsValue(string setting, string value)
        {
            this.Tools.setSettingsValue(setting, value);
        }

        // getSettingsValue binding
        public string GetSettingsValue(string setting)
        {
            return (string)this.Tools.getSettingsValue(setting);
        }

        public string Translate(string text)
        {
            return (string)this.Tools._(text);
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

        public void DisposeAndQuit(int outputCode)
        {
            Console.WriteLine("Quitting...");
            try { PythonEngine.Shutdown(); } catch { Debug.WriteLine("Cannot shutdown Python Runtime"); }
            try { GIL.Dispose(); } catch { Debug.WriteLine("Cannot dispose GIL"); }
            mainWindow.Close();
            Environment.Exit(outputCode);
        }

        private void __quit_app()
        {
            this.Exit();
        }

        public void DisposeAndQuit() { DisposeAndQuit(0); }

    }
}
