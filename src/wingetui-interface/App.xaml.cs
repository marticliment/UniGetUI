using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Xaml;
using ModernWindow.Data;
using ModernWindow.PackageEngine;
using ModernWindow.PackageEngine.Managers;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation.Collections;

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

                // Initialize error handler
                UnhandledException += (sender, e) =>
                {
                    AppTools.Log("Unhandled Exception raised: " + e.Message);
                    AppTools.Log("Stack Trace: \n" + e.Exception.StackTrace);
                    AppTools.ReportFatalException(e.Exception);
                };

                // Set WebView user data folder to temp folder, since C:\Program Files\WingetUI is read-only for non-admin processes
                if (!Directory.Exists(System.IO.Path.Join(Path.GetTempPath(), "WingetUI", "WebView")))
                    Directory.CreateDirectory(Path.Join(Path.GetTempPath(), "WingetUI", "WebView"));
                
                Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", Path.Join(Path.GetTempPath(), "WingetUI", "WebView"));

                // Reroute close event for the mainWindow
                mainWindow = new MainWindow();
                mainWindow.BlockLoading = true;
                mainWindow.Closed += (sender, args) => { DisposeAndQuit(0); };

                IntPtr hWnd = mainWindow.GetWindowHandle();

                Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
                Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

                if (appWindow != null)
                    appWindow.Closing += mainWindow.HandleClosingEvent;
                
                // Clear old notifications and register the activation event
                ToastNotificationManagerCompat.History.Clear();
                ToastNotificationManagerCompat.OnActivated += toastArgs =>
                {
                    ToastArguments args = ToastArguments.Parse(toastArgs.Argument);
                    ValueSet userInput = toastArgs.UserInput;
                    mainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        mainWindow.HandleNotificationActivation(args, userInput);
                    });
                };

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
                // Download and load icon and screenshot database
                _ = CoreData.LoadIconAndScreenshotsDatabase();

                // Do WingetUI entry text animation
                await mainWindow.DoEntryTextAnimation();

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

                // Timeout for Package Managers initialization
                int StartTime = Environment.TickCount;
                foreach (PackageManager manager in PackageManagerList)
                {
                    while (!manager.ManagerReady && Environment.TickCount - StartTime < 10000)
                    {
                        await Task.Delay(100);
                        AppTools.Log("Waiting for manager " + manager.Name);
                    }
                    AppTools.Log(manager.Name + " ready");
                }

                Debug.WriteLine("All managers loaded");

                // Hide the loading page and show the main interface
                await mainWindow.SwitchToInterface();
            }
            catch (Exception e)
            {
                AppTools.ReportFatalException(e);
            }
        }

        public async Task ShowMainWindow_FromRedirect()
        {
            while (mainWindow == null)
                await Task.Delay(100);
            mainWindow.DispatcherQueue.TryEnqueue(() => { mainWindow.Activate(); });
        }


        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            if (!CoreData.IsDaemon)
            {
                await ShowMainWindow_FromRedirect();
                CoreData.IsDaemon = false;
            }
        }

        public void DisposeAndQuit(int outputCode = 0)
        {
            AppTools.Log("Quitting...");
            mainWindow.Close();
            Environment.Exit(outputCode);
        }
    }
}
