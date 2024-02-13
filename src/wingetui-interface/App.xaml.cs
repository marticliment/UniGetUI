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

                UnhandledException += (sender, e) =>
                {
                    AppTools.Log("Unhandled Exception raised: " + e.Message);
                    AppTools.Log("Stack Trace: \n" + e.Exception.StackTrace);
                    AppTools.ReportFatalException(e.Exception);
                };

                if (!Directory.Exists(System.IO.Path.Join(Path.GetTempPath(), "WingetUI", "WebView")))
                    Directory.CreateDirectory(Path.Join(Path.GetTempPath(), "WingetUI", "WebView"));
                Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", Path.Join(Path.GetTempPath(), "WingetUI", "WebView"));

                mainWindow = new MainWindow();
                mainWindow.BlockLoading = true;
                mainWindow.Closed += (sender, args) => { DisposeAndQuit(0); };

                IntPtr hWnd = mainWindow.GetWindowHandle();

                Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
                Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

                if (appWindow != null)
                    appWindow.Closing += mainWindow.HandleClosingEvent;

                ToastNotificationManagerCompat.OnActivated += toastArgs =>
                {
                    ToastArguments args = ToastArguments.Parse(toastArgs.Argument);
                    ValueSet userInput = toastArgs.UserInput;
                    mainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        mainWindow.HandleNotificationActivation(args, userInput);
                    });
                };

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
                _ = CoreData.LoadIconAndScreenshotsDatabase();

                await mainWindow.DoEntryTextAnimation();

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

                foreach (PackageManager manager in PackageManagerList)
                    _ = manager.Initialize();

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
            ToastNotificationManagerCompat.Uninstall();
            Environment.Exit(outputCode);
        }

        private void __quit_app()
        {
            Exit();
        }

    }
}
