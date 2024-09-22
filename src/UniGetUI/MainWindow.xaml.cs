using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Web;
using H.NotifyIcon;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Classes.Manager.Classes;
using UniGetUI.PackageEngine.Interfaces;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.Windows.AppNotifications;
using UniGetUI.Core.Classes;
using UniGetUI.Interface.Enums;
using UniGetUI.Pages.DialogPages;

namespace UniGetUI.Interface
{
    public sealed partial class MainWindow : Window
    {

        public XamlRoot XamlRoot
        {
            get => MainContentGrid.XamlRoot;
        }

        private TaskbarIcon? TrayIcon;
        private bool HasLoadedLastGeometry;

        public MainView NavigationPage = null!;
        public bool BlockLoading;
        public readonly ContentDialog LoadingSthDalog;

        public int LoadingDialogCount;

        public List<ContentDialog> DialogQueue = [];

        public List<NavButton> NavButtonList = [];

        public static readonly ObservableQueue<string> ParametersToProcess = new();

        public MainWindow()
        {
            DialogHelper.Window = this;

            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(ContentRoot);

            SizeChanged += (_, _) => { SaveGeometry(); };
            AppWindow.SetIcon(Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Images", "icon.ico"));

            LoadTrayMenu();
            ApplyTheme();

            if (CoreTools.IsAdministrator())
            {
                Title = "UniGetUI " + CoreTools.Translate("[RAN AS ADMINISTRATOR]");
                AppTitle.Text = Title;
            }

#if DEBUG
            Title = Title + " - DEBUG BUILD";
            AppTitle.Text = Title;
#endif

            LoadingSthDalog = new ContentDialog
            {
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = CoreTools.Translate("Please wait"),
                Content = new ProgressBar { IsIndeterminate = true, Width = 300 }
            };

            foreach (var arg in Environment.GetCommandLineArgs())
            {
                ParametersToProcess.Enqueue(arg);
            }
        }

        public void HandleNotificationActivation(AppNotificationActivatedEventArgs args)
        {
            args.Arguments.TryGetValue("action", out string? action);
            if (action is null) action = "";

            if (action == NotificationArguments.UpdateAllPackages)
            {
                NavigationPage.UpdatesPage.UpdateAll();
            }
            else if (action == NotificationArguments.ShowOnUpdatesTab)
            {
                NavigationPage.UpdatesNavButton.ForceClick();
                Activate();
            }
            else if (action == NotificationArguments.Show)
            {
                Activate();
            }
            else
            {
                throw new ArgumentException(
                    "args.Argument was not set to a value present in Enums.NotificationArguments");
            }

            Logger.Debug("Notification activated: " + args.Arguments);
        }

        /// <summary>
        /// Handle the window closing event, and divert it when the window must be hidden.
        /// </summary>
        public async void HandleClosingEvent(AppWindow sender, AppWindowClosingEventArgs args)
        {
            SaveGeometry(Force: true);
            if (!Settings.Get("DisableSystemTray"))
            {
                args.Cancel = true;
                try
                {
                    this.Hide(enableEfficiencyMode: true);
                }
                catch (Exception ex)
                {
                    // Somewhere, Sometimes, MS Window Efficiency mode just crashes
                    Logger.Debug("Windows efficiency mode API crashed, but this was expected");
                    Logger.Debug(ex);
                    this.Hide(enableEfficiencyMode: false);
                }
            }
            else
            {
                if (MainApp.Instance.OperationQueue.Count > 0)
                {
                    args.Cancel = true;
                    ContentDialog d = new()
                    {
                        XamlRoot = NavigationPage.XamlRoot,
                        Title = CoreTools.Translate("Operation in progress"),
                        Content =
                            CoreTools.Translate(
                                "There are ongoing operations. Quitting WingetUI may cause them to fail. Do you want to continue?"),
                        PrimaryButtonText = CoreTools.Translate("Quit"),
                        SecondaryButtonText = CoreTools.Translate("Cancel"),
                        DefaultButton = ContentDialogButton.Secondary
                    };

                    ContentDialogResult result = await ShowDialogAsync(d);
                    if (result == ContentDialogResult.Primary)
                    {
                        MainApp.Instance.DisposeAndQuit();
                    }
                }
            }
        }

        /// <summary>
        /// For a given deep link, perform the appropiate action
        /// </summary>
        /// <param name="link">the unigetui:// deep link to handle</param>
        private void HandleDeepLink(string link)
        {
            string baseUrl = Uri.UnescapeDataString(link[11..]);
            Logger.ImportantInfo("Begin handle of deep link with body " + baseUrl);

            if (baseUrl.StartsWith("showPackage"))
            {
                string Id = Regex.Match(baseUrl, "id=([^&]+)").Value.Split("=")[^1];
                string CombinedManagerName = Regex.Match(baseUrl, "combinedManagerName=([^&]+)").Value.Split("=")[^1];
                string ManagerName = Regex.Match(baseUrl, "managerName=([^&]+)").Value.Split("=")[^1];
                string SourceName = Regex.Match(baseUrl, "sourceName=([^&]+)").Value.Split("=")[^1];


                if (Id != "" && CombinedManagerName != "" && ManagerName == "" && SourceName == "")
                {
                    Logger.Warn($"URI {link} follows old scheme");
                    NavigationPage.DiscoverPage.ShowSharedPackage_ThreadSafe(Id, CombinedManagerName);
                }
                else if (Id != "" && ManagerName != "" && SourceName != "")
                {
                    NavigationPage.DiscoverPage.ShowSharedPackage_ThreadSafe(Id, ManagerName, SourceName);
                }
                else
                {
                    Logger.Error(new UriFormatException($"Malformed URL {link}"));
                }
            }
            else if (baseUrl.StartsWith("showUniGetUI"))
            {
                /* Skip */
            }
            else if (baseUrl.StartsWith("showDiscoverPage"))
            {
                NavigationPage.DiscoverNavButton.ForceClick();
            }
            else if (baseUrl.StartsWith("showUpdatesPage"))
            {
                NavigationPage.UpdatesNavButton.ForceClick();
            }
            else if (baseUrl.StartsWith("showInstalledPage"))
            {
                NavigationPage.InstalledNavButton.ForceClick();
            }
            else
            {
                Logger.Error(new UriFormatException($"Malformed URL {link}"));
            }
        }

        /// <summary>
        /// Will process any remaining CLI parameter stored on MainWindow.ParametersToProcess
        /// </summary>
        public void ProcessCommandLineParameters()
        {
            while (ParametersToProcess.Count > 0)
            {
                string param = ParametersToProcess.Dequeue().Trim('\'').Trim('"');
                if (param.Length > 2 && param[0] == '-' && param[1] == '-')
                {
                    if (param == "--help")
                    {
                        NavigationPage.ShowHelp();
                    }
                    else if (new[]
                             {
                                 "--daemon", "--updateapps", "--report-all-errors", "--uninstall-unigetui",
                                 "--migrate-wingetui-to-unigetui"
                             }.Contains(param))
                    {
                        /* Skip */
                    }
                    else
                    {
                        Logger.Warn("Unknown parameter " + param);
                    }
                }
                else if (param.Length > 11 && param.ToLower().StartsWith("unigetui://"))
                {
                    HandleDeepLink(param);
                }
                else if (Path.IsPathFullyQualified(param) && File.Exists(param))
                {
                    if (param.EndsWith(".ubundle") || param.EndsWith(".json") || param.EndsWith(".xml") ||
                        param.EndsWith(".yaml"))
                    {
                        // Handle potential JSON files
                        Logger.ImportantInfo("Begin attempt to open the package bundle " + param);
                        NavigationPage.BundlesNavButton.ForceClick();
                        _ = NavigationPage.BundlesPage.OpenFromFile(param);
                    }
                    else if (param.EndsWith("UniGetUI.exe") || param.EndsWith("UniGetUI.dll"))
                    {
                        /* Skip */
                    }
                    else
                    {
                        Logger.Warn("Attempted to open the unrecognized file " + param);
                    }
                }
                else if (param.EndsWith("UniGetUI.exe") || param.EndsWith("UniGetUI.dll"))
                {
                    /* Skip */
                }
                else
                {
                    Logger.Warn("Did not know how to handle the parameter " + param);
                }
            }
        }

        public new void Activate()
        {
            if (!HasLoadedLastGeometry)
            {
                RestoreGeometry();
                HasLoadedLastGeometry = true;
            }

            NativeHelpers.SetForegroundWindow(GetWindowHandle());
            if (!PEInterface.InstalledPackagesLoader.IsLoading)
            {
                _ = PEInterface.InstalledPackagesLoader.ReloadPackagesSilently();
            }

            (this as Window).Activate();
        }

        public void HideWindow()
        {
            this.Hide();
        }

        private void LoadTrayMenu()
        {
            MenuFlyout TrayMenu = new();

            XamlUICommand DiscoverPackages = new();
            XamlUICommand AvailableUpdates = new();
            XamlUICommand InstalledPackages = new();
            XamlUICommand AboutUniGetUI = new();
            XamlUICommand ShowUniGetUI = new();
            XamlUICommand QuitUniGetUI = new();

            Dictionary<XamlUICommand, string> Labels = new()
            {
                { DiscoverPackages, "Discover Packages" },
                { AvailableUpdates, "Available Updates" },
                { InstalledPackages, "Installed Packages" },
                { AboutUniGetUI, "WingetUI Version {0}" },
                { ShowUniGetUI, "Show WingetUI" },
                { QuitUniGetUI, "Quit" },
            };

            foreach (KeyValuePair<XamlUICommand, string> item in Labels)
            {
                item.Key.Label = CoreTools.Translate(item.Value);
            }

            Dictionary<XamlUICommand, string> Icons = new()
            {
                { DiscoverPackages, "\uF6FA" },
                { AvailableUpdates, "\uE977" },
                { InstalledPackages, "\uE895" },
                { AboutUniGetUI, "\uE946" },
                { ShowUniGetUI, "\uE8A7" },
                { QuitUniGetUI, "\uE711" },
            };

            foreach (KeyValuePair<XamlUICommand, string> item in Icons)
            {
                item.Key.IconSource = new FontIconSource { Glyph = item.Value };
            }

            DiscoverPackages.ExecuteRequested += (_, _) =>
            {
                NavigationPage.DiscoverNavButton.ForceClick();
                Activate();
            };
            AvailableUpdates.ExecuteRequested += (_, _) =>
            {
                NavigationPage.UpdatesNavButton.ForceClick();
                Activate();
            };
            InstalledPackages.ExecuteRequested += (_, _) =>
            {
                NavigationPage.InstalledNavButton.ForceClick();
                Activate();
            };
            AboutUniGetUI.Label = CoreTools.Translate("WingetUI Version {0}", CoreData.VersionName);
            ShowUniGetUI.ExecuteRequested += (_, _) => { Activate(); };
            QuitUniGetUI.ExecuteRequested += (_, _) => { MainApp.Instance.DisposeAndQuit(); };

            TrayMenu.Items.Add(new MenuFlyoutItem { Command = DiscoverPackages });
            TrayMenu.Items.Add(new MenuFlyoutItem { Command = AvailableUpdates });
            TrayMenu.Items.Add(new MenuFlyoutItem { Command = InstalledPackages });
            TrayMenu.Items.Add(new MenuFlyoutSeparator());
            MenuFlyoutItem _about = new() { Command = AboutUniGetUI, IsEnabled = false };
            TrayMenu.Items.Add(_about);
            TrayMenu.Items.Add(new MenuFlyoutSeparator());
            TrayMenu.Items.Add(new MenuFlyoutItem { Command = ShowUniGetUI });
            TrayMenu.Items.Add(new MenuFlyoutItem { Command = QuitUniGetUI });

            TrayMenu.AreOpenCloseAnimationsEnabled = false;

            TrayIcon = new TaskbarIcon();
            ContentRoot.Children.Add(TrayIcon);
            Closed += (_, _) => TrayIcon.Dispose();
            TrayIcon.ContextMenuMode = H.NotifyIcon.ContextMenuMode.PopupMenu;

            XamlUICommand ShowHideCommand = new();
            ShowHideCommand.ExecuteRequested += (_, _) =>
            {
                if (MainApp.Instance.TooltipStatus.AvailableUpdates > 0)
                {
                    MainApp.Instance?.MainWindow?.NavigationPage?.UpdatesNavButton?.ForceClick();
                }

                Activate();
            };

            TrayIcon.LeftClickCommand = ShowHideCommand;
            TrayIcon.DoubleClickCommand = ShowHideCommand;
            TrayIcon.NoLeftClickDelay = true;
            TrayIcon.ContextFlyout = TrayMenu;
            UpdateSystemTrayStatus();
        }

        public void UpdateSystemTrayStatus()
        {
            string modifier = "_empty";
            string tooltip = CoreTools.Translate("Everything is up to date") + " - " + Title;

            if (MainApp.Instance.TooltipStatus.OperationsInProgress > 0)
            {
                modifier = "_blue";
                tooltip = CoreTools.Translate("Operation in progress") + " - " + Title;
            }
            else if (MainApp.Instance.TooltipStatus.ErrorsOccurred > 0)
            {
                modifier = "_orange";
                tooltip = CoreTools.Translate("Attention required") + " - " + Title;
            }
            else if (MainApp.Instance.TooltipStatus.RestartRequired)
            {
                modifier = "_turquoise";
                tooltip = CoreTools.Translate("Restart required") + " - " + Title;
            }
            else if (MainApp.Instance.TooltipStatus.AvailableUpdates > 0)
            {
                modifier = "_green";
                if (MainApp.Instance.TooltipStatus.AvailableUpdates == 1)
                {
                    tooltip = CoreTools.Translate("1 update is available") + " - " + Title;
                }
                else
                {
                    tooltip = CoreTools.Translate("{0} updates are available",
                        MainApp.Instance.TooltipStatus.AvailableUpdates) + " - " + Title;
                }
            }

            if (TrayIcon is null)
            {
                Logger.Warn("Attempting to update a null taskbar icon tray, aborting!");
                return;
            }

            TrayIcon.ToolTipText = tooltip;

            ApplicationTheme theme = ApplicationTheme.Light;
            string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            string RegistryValueName = "SystemUsesLightTheme";
            RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            object? registryValueObject = key?.GetValue(RegistryValueName) ?? null;
            if (registryValueObject is not null)
            {
                int registryValue = (int)registryValueObject;
                theme = registryValue > 0 ? ApplicationTheme.Light : ApplicationTheme.Dark;
            }

            if (theme == ApplicationTheme.Light)
            {
                modifier += "_black";
            }
            else
            {
                modifier += "_white";
            }

            string FullIconPath = Path.Join(CoreData.UniGetUIExecutableDirectory,
                "\\Assets\\Images\\tray" + modifier + ".ico");

            TrayIcon.SetValue(TaskbarIcon.IconSourceProperty, new BitmapImage { UriSource = new Uri(FullIconPath) });

            if (Settings.Get("DisableSystemTray"))
            {
                TrayIcon.Visibility = Visibility.Collapsed;
            }
            else
            {
                TrayIcon.Visibility = Visibility.Visible;
            }
        }

        public void SwitchToInterface()
        {
            SetTitleBar(__app_titlebar);
            ContentRoot = ContentRoot;

            NavigationPage = new MainView();
            Grid.SetRow(NavigationPage, 3);
            Grid.SetColumn(NavigationPage, 0);
            MainContentGrid.Children.Add(NavigationPage);

            ColumnDefinition ContentColumn = ContentRoot.ColumnDefinitions[1];
            ContentColumn.Width = new GridLength(1, GridUnitType.Star);

            ColumnDefinition SpashScreenColumn = ContentRoot.ColumnDefinitions[0];
            SpashScreenColumn.Width = new GridLength(0, GridUnitType.Pixel);
        }

        public void ApplyTheme()
        {
            string preferredTheme = Settings.GetValue("PreferredTheme");
            if (preferredTheme == "dark")
            {
                MainApp.Instance.ThemeListener.CurrentTheme = ApplicationTheme.Dark;
                ContentRoot.RequestedTheme = ElementTheme.Dark;
            }
            else if (preferredTheme == "light")
            {
                MainApp.Instance.ThemeListener.CurrentTheme = ApplicationTheme.Light;
                ContentRoot.RequestedTheme = ElementTheme.Light;
            }
            else
            {
                if (ContentRoot.ActualTheme == ElementTheme.Dark)
                {
                    MainApp.Instance.ThemeListener.CurrentTheme = ApplicationTheme.Dark;
                }
                else
                {
                    MainApp.Instance.ThemeListener.CurrentTheme = ApplicationTheme.Light;
                }

                ContentRoot.RequestedTheme = ElementTheme.Default;
            }

            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                if (MainApp.Instance.ThemeListener.CurrentTheme == ApplicationTheme.Light)
                {
                    AppWindow.TitleBar.ButtonForegroundColor = Colors.Black;
                }
                else
                {
                    AppWindow.TitleBar.ButtonForegroundColor = Colors.White;
                }
            }
            else
            {
                Logger.Info("Taskbar foreground color customization is not available");
            }
        }

        public void SharePackage(IPackage? package)
        {
            if (package is null)
            {
                return;
            }

            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            NativeHelpers.IDataTransferManagerInterop interop =
                DataTransferManager.As<NativeHelpers.IDataTransferManagerInterop>();

            IntPtr result = interop.GetForWindow(hWnd, NativeHelpers._dtm_iid);
            DataTransferManager dataTransferManager = WinRT.MarshalInterface
                <DataTransferManager>.FromAbi(result);

            dataTransferManager.DataRequested += (_, args) =>
            {
                DataRequest dataPackage = args.Request;
                Uri ShareUrl = new("https://marticliment.com/unigetui/share?"
                                   + "name=" + HttpUtility.UrlEncode(package.Name)
                                   + "&id=" + HttpUtility.UrlEncode(package.Id)
                                   + "&sourceName=" + HttpUtility.UrlEncode(package.Source.Name)
                                   + "&managerName=" + HttpUtility.UrlEncode(package.Manager.DisplayName));

                dataPackage.Data.SetWebLink(ShareUrl);
                dataPackage.Data.Properties.Title = "Sharing " + package.Name;
                dataPackage.Data.Properties.ApplicationName = "WingetUI";
                dataPackage.Data.Properties.ContentSourceWebLink = ShareUrl;
                dataPackage.Data.Properties.Description = "Share " + package.Name + " with your friends";
                dataPackage.Data.Properties.PackageFamilyName = "WingetUI";
            };

            interop.ShowShareUIForWindow(hWnd);

        }

        public IntPtr GetWindowHandle()
        {
            return WinRT.Interop.WindowNative.GetWindowHandle(this);
        }

        public async Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog, bool HighPriority = false)
        {
            try
            {
                if (HighPriority && DialogQueue.Count >= 1)
                {
                    DialogQueue.Insert(1, dialog);
                }
                else
                {
                    DialogQueue.Add(dialog);
                }

                while (DialogQueue[0] != dialog)
                {
                    await Task.Delay(100);
                }

                dialog.RequestedTheme = ContentRoot.RequestedTheme;
                ContentDialogResult result = await dialog.ShowAsync();
                DialogQueue.Remove(dialog);
                return result;
            }
            catch (Exception e)
            {
                Logger.Error("An error occurred while showing a ContentDialog via ShowDialogAsync()");
                Logger.Error(e);
                if (DialogQueue.Contains(dialog))
                {
                    DialogQueue.Remove(dialog);
                }

                return ContentDialogResult.None;
            }
        }

        public async Task HandleMissingDependencies(IEnumerable<ManagerDependency> dependencies)
        {
            int current = 1;
            int total = dependencies.Count();
            foreach (ManagerDependency dependency in dependencies)
            {
                await DialogHelper.ShowMissingDependency(dependency.Name, dependency.InstallFileName,
                    dependency.InstallArguments, dependency.FancyInstallCommand, current++, total);
            }
        }

        public async Task DoEntryTextAnimationAsync()
        {
            InAnimation_Border.Start();
            InAnimation_Text.Start();
            await Task.Delay(700);
            LoadingIndicator.Visibility = Visibility.Visible;
        }

        private async void SaveGeometry(bool Force = false)
        {
            if (!Force)
            {
                int old_width = AppWindow.Size.Width;
                int old_height = AppWindow.Size.Height;
                await Task.Delay(100);

                if (old_height != AppWindow.Size.Height || old_width != AppWindow.Size.Width)
                {
                    return;
                }
            }

            int windowState = 0;
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                if (presenter.State == OverlappedPresenterState.Maximized)
                {
                    windowState = 1;
                }
            }
            else
            {
                Logger.Warn("MainWindow.AppWindow.Presenter is not OverlappedPresenter presenter!");
            }

            string geometry =
                $"{AppWindow.Position.X},{AppWindow.Position.Y},{AppWindow.Size.Width},{AppWindow.Size.Height},{windowState}";

            Logger.Debug($"Saving window geometry {geometry}");
            Settings.SetValue("WindowGeometry", geometry);
        }

        private void RestoreGeometry()
        {

            string geometry = Settings.GetValue("WindowGeometry");
            string[] items = geometry.Split(",");
            if (items.Length != 5)
            {
                Logger.Warn($"The restored geometry did not have exactly 5 items (found length was {items.Length})");
                return;
            }

            int X, Y, Width, Height, State;
            try
            {
                X = int.Parse(items[0]);
                Y = int.Parse(items[1]);
                Width = int.Parse(items[2]);
                Height = int.Parse(items[3]);
                State = int.Parse(items[4]);
            }
            catch (Exception ex)
            {
                Logger.Error("Could not parse window geometry integers");
                Logger.Error(ex);
                return;
            }

            if (State == 1)
            {
                if (AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.Maximize();
                }
                else
                {
                    Logger.Warn("MainWindow.AppWindow.Presenter is not OverlappedPresenter presenter!");
                }
            }
            else if (IsRectangleFullyVisible(X, Y, Width, Height))
            {
                AppWindow.Resize(new Windows.Graphics.SizeInt32(Width, Height));
                AppWindow.Move(new Windows.Graphics.PointInt32(X, Y));
            }
            else
            {
                Logger.Warn("Restored geometry was outside of desktop bounds");
            }
        }

        private static bool IsRectangleFullyVisible(int x, int y, int width, int height)
        {
            List<NativeHelpers.MONITORINFO> monitorInfos = [];

            NativeHelpers.MonitorEnumDelegate callback =
                (IntPtr hMonitor, IntPtr _, ref NativeHelpers.RECT _, IntPtr _) =>
                {
                    NativeHelpers.MONITORINFO monitorInfo = new()
                    {
                        cbSize = Marshal.SizeOf(typeof(NativeHelpers.MONITORINFO))
                    };
                    if (NativeHelpers.GetMonitorInfo(hMonitor, ref monitorInfo))
                    {
                        monitorInfos.Add(monitorInfo);
                    }

                    return true;
                };

            NativeHelpers.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            foreach (NativeHelpers.MONITORINFO monitorInfo in monitorInfos)
            {
                if (monitorInfo.rcMonitor.Left < minX)
                {
                    minX = monitorInfo.rcMonitor.Left;
                }

                if (monitorInfo.rcMonitor.Top < minY)
                {
                    minY = monitorInfo.rcMonitor.Top;
                }

                if (monitorInfo.rcMonitor.Right > maxX)
                {
                    maxX = monitorInfo.rcMonitor.Right;
                }

                if (monitorInfo.rcMonitor.Bottom > maxY)
                {
                    maxY = monitorInfo.rcMonitor.Bottom;
                }
            }

            if (x + 10 < minX || x + width - 10 > maxX
                              || y + 10 < minY || y + height - 10 > maxY)
            {
                return false;
            }

            return true;
        }

    }

    public static class NativeHelpers
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [ComImport]
        [Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IDataTransferManagerInterop
        {
            IntPtr GetForWindow([In] IntPtr appWindow, [In] ref Guid riid);
            void ShowShareUIForWindow(IntPtr appWindow);
        }

        public static readonly Guid _dtm_iid = new(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d,
            0xa0, 0x0c);

        public const int MONITORINFOF_PRIMARY = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        public delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor,
            IntPtr dwData);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum,
            IntPtr dwData);

        [DllImport("user32.dll")]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
    }

}
