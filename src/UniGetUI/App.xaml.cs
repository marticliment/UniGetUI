using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UniGetUI.Core;
using UniGetUI.Core.Data;
using UniGetUI.Core.IconEngine;
using UniGetUI.Interface;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.PackageEngine.Managers;
using Windows.Foundation.Collections;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;

namespace UniGetUI
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

        public bool RaiseExceptionAsFatal = true;

        public readonly List<PackageManager> PackageManagerList = new();

        public Interface.SettingsInterface settings;
        public MainWindow MainWindow;

        private BackgroundApiRunner BackgroundApi = new();

        private const int ManagerLoadTimeout = 10000; // 10 seconds timeout for Package Manager initialization

        public MainApp()
        {
            try
            {
                InitializeComponent();

                string preferredTheme = AppTools.GetSettingsValue_Static("PreferredTheme");
                if (preferredTheme == "dark")
                {
                    RequestedTheme = ApplicationTheme.Dark;
                }
                else if (preferredTheme == "light")
                {
                    RequestedTheme = ApplicationTheme.Light;
                }


                RegisterErrorHandling();
                SetUpWebViewUserDataFolder();
                InitializeMainWindow();
                ClearNotificationHistory_Safe();
                RegisterNotificationActivationEvent_Safe();

                LoadComponentsAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                CoreTools.ReportFatalException(e);
            }
        }

        private void RegisterErrorHandling()
        {
            UnhandledException += (sender, e) =>
            {
                string message = $"Unhandled Exception raised: {e.Message}";
                string stackTrace = $"Stack Trace: \n{e.Exception.StackTrace}";
                Logger.Log(" -");
                Logger.Log(" -");
                Logger.Log("  ⚠️⚠️⚠️ START OF UNHANDLED ERROR TRACE ⚠️⚠️⚠️");
                Logger.Log(message);
                Logger.Log(stackTrace);
                Logger.Log("  ⚠️⚠️⚠️  END OF UNHANDLED ERROR TRACE  ⚠️⚠️⚠️");
                Logger.Log(" -");
                Logger.Log(" -");
                if (Environment.GetCommandLineArgs().Contains("--report-all-errors") || RaiseExceptionAsFatal || MainWindow == null)
                    CoreTools.ReportFatalException(e.Exception);
                else
                {
                    MainWindow.ErrorBanner.Title = AppTools.Instance.Translate("Something went wrong");
                    MainWindow.ErrorBanner.Message = AppTools.Instance.Translate("An interal error occurred. Please view the Logger.Log for further details.");
                    MainWindow.ErrorBanner.IsOpen = true;
                    Button button = new()
                    {
                        Content = AppTools.Instance.Translate("WingetUI Logger.Log"),
                    };
                    button.Click += (sender, args) =>
                    {
                        MainWindow.NavigationPage.UniGetUILogs_Click(sender, args);
                    };
                    MainWindow.ErrorBanner.ActionButton = button;
                    e.Handled = true;
                }
            };
        }

        private void SetUpWebViewUserDataFolder()
        {
            try
            {
                string WebViewPath = Path.Join(Path.GetTempPath(), "UniGetUI", "WebView");
                if (!Directory.Exists(WebViewPath))
                    Directory.CreateDirectory(WebViewPath);
                Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", WebViewPath);
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }
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

            nint hWnd = MainWindow.GetWindowHandle();
            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
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
                Logger.Log(ex);
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
                    ToastArguments args = ToastArguments.Parse(toastArgs.Argument);
                    ValueSet userInput = toastArgs.UserInput;

                    MainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        MainWindow.HandleNotificationActivation(args, userInput);
                    });
                };
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
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
                AppTools.Instance.UpdateUniGetUIIfPossible();
                _ = AppTools.IconDatabase.LoadIconAndScreenshotsDatabase();
                if (!AppTools.Instance.GetSettings("DisableApi"))
                    _ = BackgroundApi.Start();

                _ = MainWindow.DoEntryTextAnimationAsync();

                await InitializeAllManagersAsync();

                Logger.Log("LoadComponentsAsync finished executing. All managers loaded. Proceeding to interface.");
                MainWindow.SwitchToInterface();
                RaiseExceptionAsFatal = false;

                if (Environment.GetCommandLineArgs().Contains("--load-and-quit"))
                    DisposeAndQuit(0);
            }
            catch (Exception e)
            {
                CoreTools.ReportFatalException(e);
            }
        }

        /// <summary>
        /// Constructs Package Manager objects
        /// </summary>
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

        /// <summary>
        /// Initializes Package Manager objects (asynchronously)
        /// </summary>
        /// <returns></returns>
        private async Task InitializeAllManagersAsync()
        {
            List<Task> initializeTasks = new();

            foreach (PackageManager manager in PackageManagerList)
            {
                initializeTasks.Add(manager.InitializeAsync());
            }


            Task ManagersMetaTask = Task.WhenAll(initializeTasks);
            try
            {
                await ManagersMetaTask.WaitAsync(TimeSpan.FromMilliseconds(ManagerLoadTimeout));
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }
            if (ManagersMetaTask.IsCompletedSuccessfully == false)
                Logger.Log("Timeout: Not all package managers have finished initializing.");
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
            Logger.Log("Quitting...");
            MainWindow?.Close();
            BackgroundApi?.Stop();
            Environment.Exit(outputCode);
        }
    }
}
