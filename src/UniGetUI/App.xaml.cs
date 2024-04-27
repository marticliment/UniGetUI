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
using UniGetUI.Core.SettingsEngine;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Managers.ScoopManager;
using UniGetUI.PackageEngine.Managers.WingetManager;
using UniGetUI.PackageEngine.Managers.ChocolateyManager;
using UniGetUI.PackageEngine.Managers.PowerShellManager;
using UniGetUI.PackageEngine.Managers.DotNetManager;
using UniGetUI.PackageEngine.Managers.PipManager;
using UniGetUI.PackageEngine.Managers.NpmManager;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Net.Http;
using System.ComponentModel;
using UniGetUI.PackageEngine.Operations;
using System.ComponentModel.Design;
using CommunityToolkit.WinUI.Helpers;

namespace UniGetUI
{
    public partial class MainApp : Application
    {
        public class __tooltip_options
        {
            private int _errors_occurred = 0;
            public int ErrorsOccurred { get { return _errors_occurred; } set { _errors_occurred = value; MainApp.Instance.MainWindow.UpdateSystemTrayStatus(); } }
            private bool _restart_required = false;
            public bool RestartRequired { get { return _restart_required; } set { _restart_required = value; MainApp.Instance.MainWindow.UpdateSystemTrayStatus(); } }
            private int _operations_in_progress = 0;
            public int OperationsInProgress { get { return _operations_in_progress; } set { _operations_in_progress = value; MainApp.Instance.MainWindow.UpdateSystemTrayStatus(); } }
            private int _available_updates = 0;
            public int AvailableUpdates { get { return _available_updates; } set { _available_updates = value; MainApp.Instance.MainWindow.UpdateSystemTrayStatus(); } }
        }

        public static Scoop Scoop;
        public static WinGet Winget;
        public static Chocolatey Choco;
        public static Pip Pip;
        public static Npm Npm;
        public static DotNet Dotnet;
        public static PowerShell PowerShell;
        public List<AbstractOperation> OperationQueue = new();

        public bool RaiseExceptionAsFatal = true;
        public readonly List<PackageManager> PackageManagerList = new();

        public Interface.SettingsInterface settings;
        public MainWindow MainWindow;
        public string GSudoPath;
        public ThemeListener ThemeListener;


        private BackgroundApiRunner BackgroundApi = new();
        private const int ManagerLoadTimeout = 10000; // 10 seconds timeout for Package Manager initialization
        public static MainApp Instance;
        public __tooltip_options TooltipStatus = new();

        public MainApp() : base()
        {
            Instance = this;
            try
            {
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
                ClearNotificationHistory_Safe();
                RegisterNotificationActivationEvent_Safe();

                LoadComponentsAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                CoreTools.ReportFatalException(e);
            }
        }

        private async void LoadGSudo()
        {
            var gsudo_result = await CoreTools.Which("gsudo.exe");
            if (Settings.Get("UseUserGSudo"))
            {
                if (gsudo_result.Item1 != false)
                {
                    Logger.Log($"Using System GSudo at {gsudo_result.Item2}");
                    GSudoPath = gsudo_result.Item2;
                }
                else
                {
                    Logger.Log("System GSudo enabled but not found!");
                    GSudoPath = Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "gsudo.exe");
                }
            }
            else
            {
                GSudoPath = Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "gsudo.exe");
                Logger.Log($"Using bundled GSudo at {GSudoPath}");
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
                    MainWindow.ErrorBanner.Title = CoreTools.Translate("Something went wrong");
                    MainWindow.ErrorBanner.Message = CoreTools.Translate("An interal error occurred. Please view the Logger.Log for further details.");
                    MainWindow.ErrorBanner.IsOpen = true;
                    Button button = new()
                    {
                        Content = CoreTools.Translate("WingetUI Logger.Log"),
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

        public void AddOperationToList(AbstractOperation operation)
        {
            MainWindow.NavigationPage.OperationStackPanel.Children.Add(operation);
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
                UpdateUniGetUIIfPossible();
                _ = IconDatabase.Instance.LoadIconAndScreenshotsDatabase();
                if (!Settings.Get("DisableApi"))
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
            Winget = new WinGet();
            Scoop = new Scoop();
            Choco = new Chocolatey();
            Pip = new Pip();
            Npm = new Npm();
            Dotnet = new DotNet();
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

        private async void UpdateUniGetUIIfPossible(int round = 0)
        {
            InfoBar? banner = null;
            try
            {
                Logger.Log("Starting update check");

                string fileContents = "";

                using (HttpClient client = new())
                    fileContents = await client.GetStringAsync("https://www.marticliment.com/versions/unigetui.ver");


                if (!fileContents.Contains("///"))
                    throw new FormatException("The updates file does not follow the FloatVersion///Sha256Hash format");

                float LatestVersion = float.Parse(fileContents.Split("///")[0].Replace("\n", "").Trim(), CultureInfo.InvariantCulture);
                string InstallerHash = fileContents.Split("///")[1].Replace("\n", "").Trim().ToLower();

                if (LatestVersion > CoreData.VersionNumber)
                {
                    Logger.Log("Updates found, downloading installer...");
                    Logger.Log("Current version: " + CoreData.VersionNumber.ToString(CultureInfo.InvariantCulture));
                    Logger.Log("Latest version : " + LatestVersion.ToString(CultureInfo.InvariantCulture));

                    banner = MainWindow.UpdatesBanner;
                    banner.Title = CoreTools.Translate("WingetUI version {0} is being downloaded.").Replace("{0}", LatestVersion.ToString(CultureInfo.InvariantCulture));
                    banner.Message = CoreTools.Translate("This may take a minute or two");
                    banner.Severity = InfoBarSeverity.Informational;
                    banner.IsOpen = true;
                    banner.IsClosable = false;

                    Uri DownloadUrl = new("https://github.com/marticliment/WingetUI/releases/latest/download/UniGetUI.Installer.exe");
                    string InstallerPath = Path.Join(Directory.CreateTempSubdirectory().FullName, "unigetui-updater.exe");

                    using (HttpClient client = new())
                    {
                        HttpResponseMessage result = await client.GetAsync(DownloadUrl);
                        using (FileStream fs = new(InstallerPath, FileMode.CreateNew))
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

                        banner.Title = CoreTools.Translate("WingetUI {0} is ready to be installed.").Replace("{0}", LatestVersion.ToString(CultureInfo.InvariantCulture));
                        banner.Message = CoreTools.Translate("The update will be installed upon closing WingetUI");
                        banner.ActionButton = new Button();
                        banner.ActionButton.Content = CoreTools.Translate("Update now");
                        banner.ActionButton.Click += (sender, args) => { MainWindow.HideWindow(); };
                        banner.Severity = InfoBarSeverity.Success;
                        banner.IsOpen = true;
                        banner.IsClosable = true;

                        if (MainWindow.Visible)
                            Logger.Log("Waiting for mainWindow to be hidden");

                        while (MainWindow.Visible)
                            await Task.Delay(100);

                        Logger.Log("Hash ok, starting update");
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
                        Logger.Log("Hash mismatch, not updating!");
                        Logger.Log("Current hash : " + Hash);
                        Logger.Log("Expected hash: " + InstallerHash);
                        File.Delete(InstallerPath);

                        banner.Title = CoreTools.Translate("The installer hash does not match the expected value.");
                        banner.Message = CoreTools.Translate("The update will not continue.");
                        banner.Severity = InfoBarSeverity.Error;
                        banner.IsOpen = true;
                        banner.IsClosable = true;

                        await Task.Delay(7200000); // Check again in 2 hours
                        UpdateUniGetUIIfPossible();
                    }
                }
                else
                {
                    Logger.Log("UniGetUI is up to date");
                    await Task.Delay(7200000); // Check again in 2 hours
                    UpdateUniGetUIIfPossible();
                }
            }
            catch (Exception e)
            {
                if (banner != null)
                {
                    banner.Title = CoreTools.Translate("An error occurred when checking for updates: ");
                    banner.Message = e.Message;
                    banner.Severity = InfoBarSeverity.Error;
                    banner.IsOpen = true;
                    banner.IsClosable = true;
                }

                Logger.Log(e);

                if (round >= 3)
                    return;

                await Task.Delay(600000); // Try again in 10 minutes
                UpdateUniGetUIIfPossible(round + 1);
            }
        }

        public void RestartApp()
        {
            Logger.Log(Environment.GetCommandLineArgs()[0].Replace(".dll", ".exe"));
            System.Diagnostics.Process.Start(Environment.GetCommandLineArgs()[0].Replace(".dll", ".exe"));
            Environment.Exit(0);
        }
    }
}
