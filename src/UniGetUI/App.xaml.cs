using System.Diagnostics;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Activation;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Data;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Classes.Manager.Classes;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using UniGetUI.Core.SettingsEngine.SecureSettings;
using UniGetUI.Interface.Telemetry;
using UniGetUI.PackageEngine.Interfaces;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
using UniGetUI.Pages.DialogPages;

namespace UniGetUI
{
    public partial class MainApp
    {
        public static DispatcherQueue Dispatcher = null!;

        public static class Tooltip
        {
            private static int _errors_occurred;
            public static int ErrorsOccurred
            {
                get => _errors_occurred;
                set { _errors_occurred = value; Instance?.MainWindow?.UpdateSystemTrayStatus(); }
            }

            private static bool _restart_required;
            public static bool RestartRequired
            {
                get => _restart_required;
                set { _restart_required = value; Instance?.MainWindow?.UpdateSystemTrayStatus(); }
            }

            private static int _available_updates;
            public static int AvailableUpdates
            {
                get => _available_updates;
                set { _available_updates = value; Instance?.MainWindow?.UpdateSystemTrayStatus(); }
            }
        }

        public bool RaiseExceptionAsFatal = true;

        public MainWindow MainWindow = null!;
        public ThemeListener ThemeListener = null!;

        private readonly BackgroundApiRunner BackgroundApi = new();
        public static MainApp Instance = null!;

        public MainApp()
        {
            try
            {
                Instance = this;
                Dispatcher = DispatcherQueue.GetForCurrentThread();

                InitializeComponent();

                string preferredTheme = Settings.GetValue(Settings.K.PreferredTheme);
                if (preferredTheme == "dark")
                {
                    RequestedTheme = ApplicationTheme.Dark;
                }
                else if (preferredTheme == "light")
                {
                    RequestedTheme = ApplicationTheme.Light;
                }
                ThemeListener = new ThemeListener();

                _ = LoadGSudo();
                RegisterErrorHandling();
                SetUpWebViewUserDataFolder();
                InitializeMainWindow();
                RegisterNotificationService();

                LoadComponentsAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                CrashHandler.ReportFatalException(e);
            }
        }

        private static async Task LoadGSudo()
        {
            try
            {
                if (SecureSettings.Get(SecureSettings.K.ForceUserGSudo))
                {
                    var res = await CoreTools.WhichAsync("gsudo.exe");
                    if (res.Item1)
                    {
                        CoreData.ElevatorPath = res.Item2;
                        Logger.Warn($"Using user GSudo (forced by user) at {CoreData.ElevatorPath}");
                        return;
                    }
                }

#if DEBUG
                Logger.Warn($"Using bundled GSudo at {CoreData.ElevatorPath} since UniGetUI Elevator is not available!");
                CoreData.ElevatorPath = (await CoreTools.WhichAsync("gsudo.exe")).Item2;
#else
                CoreData.ElevatorPath = Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities",
                    "UniGetUI Elevator.exe");
                Logger.Debug($"Using built-in UniGetUI Elevator at {CoreData.ElevatorPath}");
#endif
            }
            catch (Exception ex)
            {
                Logger.Error("Elevator/GSudo failed to be loaded!");
                Logger.Error(ex);
            }
        }

        private void RegisterErrorHandling()
        {
            UnhandledException += (_, e) =>
            {
                string message = $"Unhandled Exception raised: {e.Message}";
                string stackTrace = $"Stack Trace: \n{e.Exception.StackTrace}";
                Logger.Error(" -");
                Logger.Error(" -");
                Logger.Error("  ⚠️⚠️⚠️ START OF UNHANDLED ERROR TRACE ⚠️⚠️⚠️");
                Logger.Error(message);
                Logger.Error(stackTrace);
                Logger.Error("  ⚠️⚠️⚠️  END OF UNHANDLED ERROR TRACE  ⚠️⚠️⚠️");
                Logger.Error(" -");
                Logger.Error(" -");
                if (Environment.GetCommandLineArgs().Contains("--report-all-errors") || RaiseExceptionAsFatal || MainWindow is null)
                {
                    CrashHandler.ReportFatalException(e.Exception);
                }
                else
                {
                    MainWindow.ErrorBanner.Title = CoreTools.Translate("Something went wrong");
                    MainWindow.ErrorBanner.Message = CoreTools.Translate("An interal error occurred. Please view the log for further details.");
                    MainWindow.ErrorBanner.IsOpen = true;
                    Button button = new()
                    {
                        Content = CoreTools.Translate("WingetUI Log"),
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

        private static void SetUpWebViewUserDataFolder()
        {
            try
            {
                string WebViewPath = Path.Join(Path.GetTempPath(), "UniGetUI", "WebView");
                if (!Directory.Exists(WebViewPath))
                {
                    Directory.CreateDirectory(WebViewPath);
                }

                Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", WebViewPath);
            }
            catch (Exception e)
            {
                Logger.Warn("Could not set up data folder for WebView2");
                Logger.Warn(e);
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
            MainWindow.Closed += (_, _) => DisposeAndQuit(0);

            nint hWnd = MainWindow.GetWindowHandle();
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow is not null)
            {
                appWindow.Closing += MainWindow.HandleClosingEvent;
            }
        }

        /// <summary>
        /// Register the notification activation event
        /// </summary>
        private void RegisterNotificationService()
        {
            try
            {
                AppNotificationManager.Default.NotificationInvoked += (_, args) =>
                {
                    MainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        MainWindow.HandleNotificationActivation(args);
                    });
                };
                AppNotificationManager.Default.Register();
            }
            catch (Exception ex)
            {
                Logger.Error("Could not register notification event");
                Logger.Error(ex);
            }
        }

        /// <summary>
        /// Background component loader
        /// </summary>
        private async Task LoadComponentsAsync()
        {
            try
            {
                IconDatabase.InitializeInstance();
                IconDatabase.Instance.LoadIconAndScreenshotsDatabase();

                await InitializeBackgroundAPI();

                _ = MainWindow.DoEntryTextAnimationAsync();

                // Load package managers
                await Task.Run(PEInterface.Initialize);
                TelemetryHandler.Initialize();

                Logger.Info("LoadComponentsAsync finished executing. All managers loaded. Proceeding to interface.");
                MainWindow.SwitchToInterface();
                RaiseExceptionAsFatal = false;

                MainWindow.ProcessCommandLineParameters();
                MainWindow.ParametersToProcess.ItemEnqueued += (_, _) =>
                {
                    MainWindow.DispatcherQueue.TryEnqueue(MainWindow.ProcessCommandLineParameters);
                };

                await CheckForMissingDependencies();
            }
            catch (Exception e)
            {
                CrashHandler.ReportFatalException(e);
            }
        }

        private async Task InitializeBackgroundAPI()
        {
            // Bind the background api to the main interface
            if (!Settings.Get(Settings.K.DisableApi))
            {
                try
                {
                    BackgroundApi.OnOpenWindow += (_, _) =>
                        MainWindow.DispatcherQueue.TryEnqueue(() => MainWindow.Activate());

                    BackgroundApi.OnOpenUpdatesPage += (_, _) => MainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        MainWindow?.NavigationPage?.NavigateTo(PageType.Updates);
                        MainWindow?.Activate();
                    });

                    BackgroundApi.OnShowSharedPackage += (_, package) => MainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        DialogHelper.ShowSharedPackage_ThreadSafe(package.Key, package.Value);
                    });

                    BackgroundApi.OnUpgradeAll += (_, _) => MainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        Operations.UpdateAll();
                    });

                    BackgroundApi.OnUpgradeAllForManager += (_, managerName) =>
                        MainWindow.DispatcherQueue.TryEnqueue(async () =>
                        {
                            Operations.UpdateAllForManager(managerName);
                        });

                    BackgroundApi.OnUpgradePackage += (_, packageId) => MainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        Operations.UpdateForId(packageId);
                    });

                    await BackgroundApi.Start();
                }
                catch (Exception ex)
                {
                    Logger.Error("Could not initialize Background API:");
                    Logger.Error(ex);
                }
            }
        }

        private async Task CheckForMissingDependencies()
        {
            // Check for missing dependencies on package managers
            List<ManagerDependency> missing_deps = [];
            foreach (IPackageManager manager in PEInterface.Managers)
            {
                if (!manager.IsReady())
                {
                    continue;
                }

                foreach (ManagerDependency dependency in manager.Dependencies)
                {
                    bool isInstalled = true;
                    try
                    {
                        isInstalled = await dependency.IsInstalled();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(
                            $"An error occurred while checking if dependency {dependency.Name} was installed:");
                        Logger.Error(ex);
                    }

                    if (!isInstalled)
                    {
                        if (Settings.GetDictionaryItem<string, string>(Settings.K.DependencyManagement, dependency.Name) == "skipped")
                        {
                            Logger.Error($"Dependency {dependency.Name} was not found, and the user set it to not be reminded of the missing dependency");
                        }
                        else
                        {
                            Logger.Warn($"Dependency {dependency.Name} was not found for manager {manager.Name}, marking to prompt...");
                            missing_deps.Add(dependency);
                        }
                    }
                    else
                    {
                        Logger.Info($"Dependency {dependency.Name} for manager {manager.Name} is present");
                    }
                }
            }
            await MainWindow.HandleMissingDependencies(missing_deps);
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            MainWindow?.Activate();
        }

        public async Task ShowMainWindowFromRedirectAsync(AppActivationArguments rawArgs)
        {
            while (MainWindow is null)
                await Task.Delay(100);

            ExtendedActivationKind kind = rawArgs.Kind;
            if (kind is ExtendedActivationKind.Launch)
            {
                if (rawArgs.Data is ILaunchActivatedEventArgs launchArguments)
                {
                    // If the app redirection event comes from a launch, extract
                    // the CLI arguments and redirect them to the ParameterProcessor
                    foreach (Match argument in Regex.Matches(launchArguments.Arguments,
                                 "([^ \"']+|\"[^\"]+\"|'[^']+')"))
                    {
                        MainWindow.ParametersToProcess.Enqueue(argument.Value);
                    }
                }
                else
                {
                    Logger.Error("REDIRECTOR ACTIVATOR: args.Data was null when casted to ILaunchActivatedEventArgs");
                }
            }
            else
            {
                Logger.Warn("REDIRECTOR ACTIVATOR: args.Kind is not Launch but rather " + kind);
            }

            MainWindow.DispatcherQueue.TryEnqueue(MainWindow.Activate);
        }

        public async void DisposeAndQuit(int outputCode = 0)
        {
            Logger.Warn("Quitting UniGetUI");
            DWMThreadHelper.ChangeState_DWM(false);
            DWMThreadHelper.ChangeState_XAML(false);
            MainWindow?.Close();
            BackgroundApi?.Stop();
            Exit();
            // await Task.Delay(100);
            // Environment.Exit(outputCode);
        }

        public void KillAndRestart()
        {
            Process.Start(CoreData.UniGetUIExecutableFile);
            Instance.MainWindow?.Close();
            Environment.Exit(0);
        }
    }
}
