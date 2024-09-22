using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Activation;
using CommunityToolkit.WinUI.Helpers;
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
using UniGetUI.PackageEngine.Operations;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;

namespace UniGetUI
{
    public partial class MainApp
    {
        public class __tooltip_options
        {
            private int _errors_occurred;
            public int ErrorsOccurred { get { return _errors_occurred; } set { _errors_occurred = value; Instance.MainWindow.UpdateSystemTrayStatus(); } }
            private bool _restart_required;
            public bool RestartRequired { get { return _restart_required; } set { _restart_required = value; Instance.MainWindow.UpdateSystemTrayStatus(); } }
            private int _operations_in_progress;
            public int OperationsInProgress { get { return _operations_in_progress; } set { _operations_in_progress = value; Instance.MainWindow.UpdateSystemTrayStatus(); } }
            private int _available_updates;
            public int AvailableUpdates { get { return _available_updates; } set { _available_updates = value; Instance.MainWindow.UpdateSystemTrayStatus(); } }
        }

        public List<AbstractOperation> OperationQueue = [];

        public bool RaiseExceptionAsFatal = true;

        public SettingsInterface settings = null!;
        public MainWindow MainWindow = null!;
        public ThemeListener ThemeListener = null!;

        private readonly BackgroundApiRunner BackgroundApi = new();
        public static MainApp Instance = null!;
        public __tooltip_options TooltipStatus = new();

        public MainApp()
        {
            try
            {
                Instance = this;

                InitializeComponent();

                string preferredTheme = Settings.GetValue("PreferredTheme");
                if (preferredTheme == "dark")
                {
                    RequestedTheme = ApplicationTheme.Dark;
                }
                else if (preferredTheme == "light")
                {
                    RequestedTheme = ApplicationTheme.Light;
                }
                ThemeListener = new ThemeListener();

                LoadGSudo();
                RegisterErrorHandling();
                SetUpWebViewUserDataFolder();
                InitializeMainWindow();
                RegisterNotificationService();

                LoadComponentsAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                CoreTools.ReportFatalException(e);
            }
        }

        private static async void LoadGSudo()
        {
            if (Settings.Get("UseUserGSudo"))
            {
                Tuple<bool, string> gsudo_result = await CoreTools.WhichAsync("gsudo.exe");
                if (gsudo_result.Item1 != false)
                {
                    Logger.Info($"Using System GSudo at {gsudo_result.Item2}");
                    CoreData.GSudoPath = gsudo_result.Item2;
                }
                else
                {
                    Logger.Error("System GSudo enabled but not found!");
                    CoreData.GSudoPath = Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "gsudo.exe");
                }
            }
            else
            {
                CoreData.GSudoPath = Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "gsudo.exe");
                Logger.Info($"Using bundled GSudo at {CoreData.GSudoPath}");
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
                Logger.Error(e.Message);
                Logger.Error(e.Exception);
                Logger.Error("  ⚠️⚠️⚠️  END OF UNHANDLED ERROR TRACE  ⚠️⚠️⚠️");
                Logger.Error(" -");
                Logger.Error(" -");
                if (Environment.GetCommandLineArgs().Contains("--report-all-errors") || RaiseExceptionAsFatal || MainWindow is null)
                {
                    CoreTools.ReportFatalException(e.Exception);
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

        public void AddOperationToList(AbstractOperation operation)
        {
            MainWindow.NavigationPage.OperationStackPanel.Children.Add(operation);
        }

        /// <summary>
        /// Background component loader
        /// </summary>
        private async Task LoadComponentsAsync()
        {
            try
            {
                // Run other initializations asynchronously
                if (!Settings.Get("DisableAutoUpdateWingetUI"))
                {
                    UpdateUniGetUIIfPossible();
                }

                IconDatabase.InitializeInstance();
                IconDatabase.Instance.LoadIconAndScreenshotsDatabase();

                // Bind the background api to the main interface

                BackgroundApi.OnOpenWindow += (_, _) => MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    MainWindow.Activate();
                });

                BackgroundApi.OnOpenUpdatesPage += (_, _) => MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    MainWindow?.NavigationPage?.UpdatesNavButton.ForceClick();
                    MainWindow?.Activate();
                });

                BackgroundApi.OnShowSharedPackage += (_, package) => MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    MainWindow?.NavigationPage?.DiscoverPage.ShowSharedPackage_ThreadSafe(package.Key, package.Value);
                    MainWindow?.Activate();
                });

                BackgroundApi.OnUpgradeAll += (_, _) => MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    MainWindow?.NavigationPage?.UpdatesPage.UpdateAll();
                });

                BackgroundApi.OnUpgradeAllForManager += (_, manager) => MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    MainWindow?.NavigationPage?.UpdatesPage.UpdateAllPackagesForManager(manager);
                });

                BackgroundApi.OnUpgradePackage += (_, package) => MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    MainWindow?.NavigationPage?.UpdatesPage.UpdatePackageForId(package);
                });

                if (!Settings.Get("DisableApi"))
                {
                    _ = BackgroundApi.Start();
                }

                _ = MainWindow.DoEntryTextAnimationAsync();

                // Load package managers
                await Task.Run(() => PEInterface.Initialize());

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
                CoreTools.ReportFatalException(e);
            }
        }

        private async Task CheckForMissingDependencies()
        {
            // Check for missing dependencies on package managers
            List<ManagerDependency> missing_deps = [];
            foreach (IPackageManager imanager in PEInterface.Managers)
            {
                if (imanager is not PackageManager manager) continue;

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
                        if (Settings.Get($"SkippedInstalling{dependency.Name}"))
                        {
                            Logger.Error($"Dependency {dependency.Name} was not found, and the user set it to not be reminded of the midding dependency");
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

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (!CoreData.IsDaemon)
            {
                await ShowMainWindowFromLaunchAsync();
            }

            CoreData.IsDaemon = false;
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

        public async Task ShowMainWindowFromLaunchAsync()
        {
            while (MainWindow is null)
                await Task.Delay(100);

            MainWindow.DispatcherQueue.TryEnqueue(MainWindow.Activate);
        }

        public async void DisposeAndQuit(int outputCode = 0)
        {
            Logger.Warn("Quitting...");
            MainWindow?.Close();
            BackgroundApi?.Stop();
            Exit();
            await Task.Delay(100);
            Environment.Exit(outputCode);
        }

        private async void UpdateUniGetUIIfPossible(int round = 0)
        {
            InfoBar? banner = null;
            try
            {
                Logger.Debug("Starting update check");

                string fileContents;

                using (HttpClient client = new(CoreData.GenericHttpClientParameters))
                {
                    client.Timeout = TimeSpan.FromSeconds(600);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(CoreData.UserAgentString);
                    fileContents = await client.GetStringAsync("https://www.marticliment.com/versions/unigetui.ver");
                }

                if (!fileContents.Contains("///"))
                {
                    throw new FormatException("The updates file does not follow the FloatVersion///Sha256Hash format");
                }

                float LatestVersion = float.Parse(fileContents.Split("///")[0].Replace("\n", "").Trim(), CultureInfo.InvariantCulture);
                string InstallerHash = fileContents.Split("///")[1].Replace("\n", "").Trim().ToLower();

                if (LatestVersion > CoreData.VersionNumber)
                {
                    Logger.Info("Updates found, downloading installer...");
                    Logger.Info("Current version: " + CoreData.VersionNumber.ToString(CultureInfo.InvariantCulture));
                    Logger.Info("Latest version : " + LatestVersion.ToString(CultureInfo.InvariantCulture));

                    banner = MainWindow.UpdatesBanner;
                    banner.Title = CoreTools.Translate("WingetUI version {0} is being downloaded.", LatestVersion.ToString(CultureInfo.InvariantCulture));
                    banner.Message = CoreTools.Translate("This may take a minute or two");
                    banner.Severity = InfoBarSeverity.Informational;
                    banner.IsOpen = true;
                    banner.IsClosable = false;

                    Uri DownloadUrl = new("https://github.com/marticliment/WingetUI/releases/latest/download/UniGetUI.Installer.exe");
                    string InstallerPath = Path.Join(Directory.CreateTempSubdirectory().FullName, "unigetui-updater.exe");

                    using (HttpClient client = new(CoreData.GenericHttpClientParameters))
                    {
                        client.DefaultRequestHeaders.UserAgent.ParseAdd(CoreData.UserAgentString);
                        HttpResponseMessage result = await client.GetAsync(DownloadUrl);
                        using FileStream fs = new(InstallerPath, FileMode.CreateNew);
                        await result.Content.CopyToAsync(fs);
                    }

                    string Hash = "";
                    SHA256 Sha256 = SHA256.Create();
                    using (FileStream stream = File.OpenRead(InstallerPath))
                    {
                        Hash = Convert.ToHexString(Sha256.ComputeHash(stream)).ToLower();
                    }

                    if (Hash == InstallerHash)
                    {

                        banner.Title = CoreTools.Translate("WingetUI {0} is ready to be installed.", LatestVersion.ToString(CultureInfo.InvariantCulture));
                        banner.Message = CoreTools.Translate("The update will be installed upon closing WingetUI");
                        banner.ActionButton = new Button
                        {
                            Content = CoreTools.Translate("Update now")
                        };
                        banner.ActionButton.Click += (_, _) => { MainWindow.HideWindow(); };
                        banner.Severity = InfoBarSeverity.Success;
                        banner.IsOpen = true;
                        banner.IsClosable = true;

                        if (MainWindow.Visible)
                        {
                            Logger.Debug("Waiting for mainWindow to be hidden");
                        }

                        while (MainWindow.Visible)
                        {
                            await Task.Delay(100);
                        }

                        if (Settings.Get("DisableAutoUpdateWingetUI"))
                        {
                            Logger.Warn("User disabled updates!");
                            return;
                        }

                        Logger.ImportantInfo("The hash matches the expected value, starting update process...");
                        Process p = new();
                        p.StartInfo.FileName = "cmd.exe";
                        p.StartInfo.Arguments = $"/c start /B \"\" \"{InstallerPath}\" /silent";
                        p.StartInfo.UseShellExecute = true;
                        p.StartInfo.CreateNoWindow = true;
                        p.Start();
                        DisposeAndQuit();
                    }
                    else
                    {
                        Logger.Error("Hash mismatch, not updating!");
                        Logger.Error("Current hash : " + Hash);
                        Logger.Error("Expected hash: " + InstallerHash);
                        File.Delete(InstallerPath);

                        banner.Title = CoreTools.Translate("The installer hash does not match the expected value.");
                        banner.Message = CoreTools.Translate("The update will not continue.");
                        banner.Severity = InfoBarSeverity.Error;
                        banner.IsOpen = true;
                        banner.IsClosable = true;

                        await Task.Delay(3600000); // Check again in 1 hour
                        UpdateUniGetUIIfPossible();
                    }
                }
                else
                {
                    Logger.Info("UniGetUI is up to date");
                    await Task.Delay(3600000); // Check again in 1 hour
                    UpdateUniGetUIIfPossible();
                }
            }
            catch (Exception e)
            {
                if (banner is not null)
                {
                    banner.Title = CoreTools.Translate("An error occurred when checking for updates: ");
                    banner.Message = e.Message;
                    banner.Severity = InfoBarSeverity.Error;
                    banner.IsOpen = true;
                    banner.IsClosable = true;
                }

                Logger.Error(e);

                if (round >= 3)
                {
                    return;
                }

                await Task.Delay(600000); // Try again in 10 minutes
                UpdateUniGetUIIfPossible(round + 1);
            }
        }

        public void KillAndRestart()
        {
            Process.Start(CoreData.UniGetUIExecutableFile);
            MainApp.Instance.MainWindow?.Close();
            Environment.Exit(0);
        }
    }
}
