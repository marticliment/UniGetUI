using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Xaml;
using ModernWindow.Core.Data;
using ModernWindow.Interface;
using ModernWindow.PackageEngine.Classes;
using ModernWindow.PackageEngine.Managers;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using YamlDotNet.Serialization;

namespace ModernWindow
{
    public partial class MainApp : Application
    {
        public Scoop Scoop;
        public Winget Winget;
        public Chocolatey Choco;
        public Pip Pip;
        public Npm Npm;
        public Dotnet Dotnet;
        public PowerShell Powershell;

        public List<PackageManager> PackageManagerList = new();

        public Interface.SettingsInterface settings;
        public MainWindow mainWindow;


        public MainApp()
        {
            try
            {
                InitializeComponent();

                // Register custom error handler
                UnhandledException += (sender, e) =>
                {
                    AppTools.Log("Unhandled Exception raised: " + e.Message);
                    AppTools.Log("Stack Trace: \n" + e.Exception.StackTrace);
                    AppTools.ReportFatalException(e.Exception);
                };

                // Set WebView user data folder to temp folder.
                // The default C:\Program Files\WingetUI\WebView2.UserData is read-only for non-admin processes
                if (!Directory.Exists(Path.Join(Path.GetTempPath(), "WingetUI", "WebView")))
                    Directory.CreateDirectory(Path.Join(Path.GetTempPath(), "WingetUI", "WebView"));
                Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", Path.Join(Path.GetTempPath(), "WingetUI", "WebView"));

                // Create the main window, but do not show it yet
                mainWindow = new MainWindow();
                mainWindow.BlockLoading = true;
                mainWindow.Closed += (sender, args) => { DisposeAndQuit(0); };

                // Reroute close event for the mainWindow
                IntPtr hWnd = mainWindow.GetWindowHandle();
                Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
                Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                if (appWindow != null)
                    appWindow.Closing += mainWindow.HandleClosingEvent;

                // Clear notification history
                try 
                { 
                    ToastNotificationManagerCompat.History.Clear();
                }
                catch (Exception ex)
                {
                    AppTools.Log(ex);
                }
                
                // Register notification activation event
                try
                { 
                    ToastNotificationManagerCompat.OnActivated += toastArgs =>
                    {
                        ToastArguments args = ToastArguments.Parse(toastArgs.Argument);
                        ValueSet userInput = toastArgs.UserInput;
                        mainWindow.DispatcherQueue.TryEnqueue(() =>
                        {
                            mainWindow.HandleNotificationActivation(args, userInput);
                        });
                    };
                }
                catch (Exception ex)
                {
                    AppTools.Log(ex);
                }

                // Start async loading of components
                LoadComponents();
            }
            catch (Exception e)
            {
                AppTools.ReportFatalException(e);
            }
        }

        public async void LoadComponents()
        {
            try
            {
                // Start WingetUI AutoUpdater
                AppTools.Instance.UpdateWingetUIIfPossible();

                // Download and load icon and screenshot database
                _ = CoreData.LoadIconAndScreenshotsDatabase();

                // Do WingetUI entry text animation
                await mainWindow.DoEntryTextAnimation();

                var apirunner = new BackgroundApiRunner();
                _ = apirunner.Start();

                // Load Package Managers
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

                // Start initializing package managers
                foreach (PackageManager manager in PackageManagerList)
                    _ = manager.Initialize();

                // Timeout for Package Managers initialization, to prevent infinite loading.
                // Current timeout (in millisecs) is 10 seconds
                const int MANAGER_LOAD_TIMEOUT = 10000;
                int StartTime = Environment.TickCount;
                foreach (PackageManager manager in PackageManagerList)
                    while (!manager.ManagerReady && Environment.TickCount - StartTime < MANAGER_LOAD_TIMEOUT)
                        await Task.Delay(100);

                // Hide the loading page and show the main interface
                Debug.WriteLine("LoadComponents finished executing, all managers loaded. Proceeding to interface.");
                await mainWindow.SwitchToInterface();
            }
            catch (Exception e)
            {
                AppTools.ReportFatalException(e);
            }
        }

        public async Task ShowMainWindow_FromRedirect()
        {
            // Thread-safe method to show main window.
            // if mainWindow == null, wait until it is defined.
            while (mainWindow == null)
                await Task.Delay(100);
            mainWindow.DispatcherQueue.TryEnqueue(() => { mainWindow.Activate(); });
        }


        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // If the app has not been called with the --daemon argument, show the main window.
            
            // TODO: Deeply test this method
            if (!CoreData.IsDaemon)
            {
                await ShowMainWindow_FromRedirect();
                CoreData.IsDaemon = false;
            }
        }

        public async void DisposeAndQuit(int outputCode = 0)
        {
            AppTools.Log("Quitting...");
            mainWindow.Close();
            Environment.Exit(outputCode);
        }
    }
}
