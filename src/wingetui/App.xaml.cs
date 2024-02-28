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
        public PowerShell PowerShell;

        public readonly List<PackageManager> PackageManagerList = new();

        public Interface.SettingsInterface settings;
        public MainWindow MainWindow;

        private const string WebViewFolder = "WingetUI\\WebView";
        private readonly string _webViewPath = Path.Join(Path.GetTempPath(), WebViewFolder);

        private const int ManagerLoadTimeout = 10000; // 10 seconds timeout for Package Managers initialization

        public MainApp()
        {
            InitializeComponent();
            RegisterErrorHandling();
            SetUpWebViewUserDataFolder();
            InitializeMainWindow();
            ClearNotificationHistory_Safe();
            RegisterNotificationActivationEvent_Safe();

            LoadComponentsAsync().ConfigureAwait(false);
        }

        private void RegisterErrorHandling()
        {
            UnhandledException += (sender, e) =>
            {
                var message = $"Unhandled Exception raised: {e.Message}";
                var stackTrace = $"Stack Trace: \n{e.Exception.StackTrace}";
                AppTools.Log(message);
                AppTools.Log(stackTrace);
                AppTools.ReportFatalException(e.Exception);
            };
        }

        private void SetUpWebViewUserDataFolder()
        {
            if (!Directory.Exists(_webViewPath))
                Directory.CreateDirectory(_webViewPath);
            Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", _webViewPath);
        }

        /// <summary>
        /// Initialize the main window
        /// </summary>
        private void InitializeMainWindow()
        {
            MainWindow = new MainWindow
            {
                BlockLoading = true
            };
            MainWindow.Closed += (sender, args) => DisposeAndQuit(0);
            
            var hWnd = MainWindow.GetWindowHandle();
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if(appWindow != null)
                appWindow.Closing += MainWindow.HandleClosingEvent;
        }


        /// <summary>
        /// Clear the notification history, if possible
        /// </summary>
        private void ClearNotificationHistory_Safe()
        {
            try
            {
                ToastNotificationManagerCompat.History.Clear();
            }
            catch (Exception ex)
            {
                AppTools.Log(ex);
            }
        }

        /// <summary>
        /// Register the notification activation event
        /// </summary>
        private void RegisterNotificationActivationEvent_Safe()
        {
            try
            {
                ToastNotificationManagerCompat.OnActivated += toastArgs =>
                {
                    var args = ToastArguments.Parse(toastArgs.Argument);
                    var userInput = toastArgs.UserInput;

                    MainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        MainWindow.HandleNotificationActivation(args, userInput);
                    });
                };
            }
            catch (Exception ex)
            {
                AppTools.Log(ex);
            }
        }

        /// <summary>
        /// Background component loader 
        /// </summary>
        /// <returns></returns>
        private async Task LoadComponentsAsync()
        {
            try
            {
                InitializePackageManagers();

                // Run other initializations asynchronously
                var updateWingetUITask = Task.Run(() => { AppTools.Instance.UpdateWingetUIIfPossible(); });
                var loadIconAndScreenshotsTask = Task.Run(() => { _ = CoreData.LoadIconAndScreenshotsDatabase(); });

                await Task.WhenAll(updateWingetUITask, loadIconAndScreenshotsTask, InitializeAllManagersAsync());

                AppTools.Log("LoadComponentsAsync finished executing. All managers loaded. Proceeding to interface.");
                MainWindow.SwitchToInterface();
            }
            catch (Exception e)
            {
                AppTools.ReportFatalException(e);
            }
        }

        private void InitializePackageManagers()
        {
            Winget = new Winget();
            Scoop = new Scoop();
            Choco = new Chocolatey();
            Pip = new Pip();
            Npm = new Npm();
            Dotnet = new Dotnet();
            PowerShell = new PowerShell();

            PackageManagerList.AddRange(new PackageManager[]
            {
                Winget,
                Scoop,
                Choco,
                Pip,
                Npm,
                Dotnet,
                PowerShell
            });
        }

        private async Task InitializeAllManagersAsync()
        {
            var initializeTasks = new List<Task>();

            foreach (var manager in PackageManagerList)
            {
                initializeTasks.Add(manager.InitializeAsync());
            }


            var ManagersMetaTask = Task.WhenAll(initializeTasks);
            await ManagersMetaTask.WaitAsync(TimeSpan.FromMilliseconds(ManagerLoadTimeout));
            if (ManagersMetaTask.IsCompletedSuccessfully == false)
            {
                AppTools.Log("Timeout: Not all package managers have finished initializing.");
            }
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (!CoreData.IsDaemon)
            {
                await ShowMainWindowFromRedirectAsync();
            }

            CoreData.IsDaemon = false;
        }

        public async Task ShowMainWindowFromRedirectAsync()
        {
            while (MainWindow == null)
            {
                await Task.Delay(100);
            }

            MainWindow.DispatcherQueue.TryEnqueue(MainWindow.Activate);
        }

        public void DisposeAndQuit(int outputCode = 0)
        {
            AppTools.Log("Quitting...");
            MainWindow?.Close();
            Environment.Exit(outputCode);
        }
    }
}
