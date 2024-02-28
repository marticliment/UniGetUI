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

        private readonly List<PackageManager> _packageManagerList = new();

        public Interface.SettingsInterface settings;
        public MainWindow mainWindow;

        private const string WebViewFolder = "WingetUI\\WebView";
        private readonly string _webViewPath = Path.Join(Path.GetTempPath(), WebViewFolder);

        private const int ManagerLoadTimeout = 10000; // 10 seconds timeout for Package Managers initialization

        public MainApp()
        {
            InitializeComponent();
            RegisterErrorHandling();
            SetUpWebViewUserDataFolder();
            InitializeMainWindow();
            ClearNotificationHistory();
            RegisterNotificationActivationEvent();

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
            {
                Directory.CreateDirectory(_webViewPath);
            }
            Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", _webViewPath);
        }

        private void InitializeMainWindow()
        {
            mainWindow = new MainWindow
            {
                BlockLoading = true
            };
            mainWindow.Closed += (sender, args) => DisposeAndQuit(0);

            RedirectMainWindowCloseEvent();
        }

        private void RedirectMainWindowCloseEvent()
        {
            var hWnd = mainWindow.GetWindowHandle();
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            appWindow?.Closing += mainWindow.HandleClosingEvent;
        }

        private void ClearNotificationHistory()
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

        private void RegisterNotificationActivationEvent()
        {
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                var args = ToastArguments.Parse(toastArgs.Argument);
                var userInput = toastArgs.UserInput;

                mainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    mainWindow.HandleNotificationActivation(args, userInput);
                });
            };
        }

        private async Task LoadComponentsAsync()
        {
            try
            {
                InitializePackageManagers();

                // Run other initializations asynchronously
                var updateWingetUITask = Task.Run(AppTools.Instance.UpdateWingetUIIfPossible);
                var loadIconAndScreenshotsTask = Task.Run(CoreData.LoadIconAndScreenshotsDatabase);

                await Task.WhenAll(updateWingetUITask, loadIconAndScreenshotsTask, InitializeAllManagersAsync());

                Debug.WriteLine("LoadComponentsAsync finished executing. All managers loaded. Proceeding to interface.");
                await mainWindow.SwitchToInterfaceAsync();
            }
            catch (Exception e)
            {
                AppTools.ReportFatalException(e);
            }
        }

        private void InitializePackageManagers()
        {
            _packageManagerList.AddRange(new PackageManager[]
            {
                new Winget(),
                new Scoop(),
                new Chocolatey(),
                new Pip(),
                new Npm(),
                new Dotnet(),
                new PowerShell()
            });
        }

        private async Task InitializeAllManagersAsync()
        {
            var initializeTasks = new List<Task>();

            foreach (var manager in _packageManagerList)
            {
                initializeTasks.Add(manager.InitializeAsync());
            }

            if (await Task.WhenAll(initializeTasks).WaitAsync(TimeSpan.FromMilliseconds(ManagerLoadTimeout)) == false)
            {
                Debug.WriteLine("Timeout: Not all package managers have finished initializing.");
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

        private async Task ShowMainWindowFromRedirectAsync()
        {
            while (mainWindow == null)
            {
                await Task.Delay(100);
            }

            mainWindow.DispatcherQueue.TryEnqueue(mainWindow.Activate);
        }

        public void DisposeAndQuit(int outputCode = 0)
        {
            AppTools.Log("Quitting...");
            mainWindow?.Close();
            Environment.Exit(outputCode);
        }
    }
}
