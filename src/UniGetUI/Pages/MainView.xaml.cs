using Windows.System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using UniGetUI.Core.Data;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Dialogs;
using UniGetUI.Interface.Pages;
using UniGetUI.Interface.Pages.LogPage;
using UniGetUI.Interface.SoftwarePages;
using Windows.UI.Core;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.Pages.DialogPages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface
{
    public enum PageType
    {
        Discover,
        Updates,
        Installed,
        Bundles,
        Settings,
        OwnLog,
        ManagerLog,
        OperationHistory,
        Help,
        Null // Used for initializers
    }

    public sealed partial class MainView : UserControl
    {
        public DiscoverSoftwarePage DiscoverPage;
        public SoftwareUpdatesPage UpdatesPage;
        public InstalledPackagesPage InstalledPage;
        public PackageBundlesPage BundlesPage;
        private SettingsPage? SettingsPage;
        private UniGetUILogPage? UniGetUILogPage;
        private ManagerLogsPage? ManagerLogPage;
        private OperationHistoryPage? OperationHistoryPage;
        private HelpPage? HelpPage;

        private PageType OldPage_t = PageType.Null;
        private PageType CurrentPage_t = PageType.Null;
        private readonly HashSet<Page> AddedPages = new();

        public MainView()
        {
            InitializeComponent();
            DiscoverPage = new DiscoverSoftwarePage();
            UpdatesPage = new SoftwareUpdatesPage
            {
                ExternalCountBadge = UpdatesBadge
            };
            InstalledPage = new InstalledPackagesPage();
            BundlesPage = new PackageBundlesPage();

            foreach (Page page in new Page[] { DiscoverPage, UpdatesPage, InstalledPage, BundlesPage })
            {
                Grid.SetColumn(page, 0);
                Grid.SetRow(page, 0);
                MainContentPresenterGrid.Children.Add(page);
                AddedPages.Add(page);
            }

            MoreNavButtonMenu.Closed += (_, _) => SelectNavButtonForPage(CurrentPage_t);
            KeyUp += (s, e) =>
            {
                bool IS_CONTROL_PRESSED = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
                bool IS_SHIFT_PRESSED = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

                Page currentPage = GetPageForType(CurrentPage_t);
                if (e.Key is VirtualKey.Tab && IS_CONTROL_PRESSED)
                {
                    NavigateTo(IS_SHIFT_PRESSED ? GetPreviousPage(CurrentPage_t) : GetNextPage(CurrentPage_t));
                }
                else if (e.Key == VirtualKey.F1)
                {
                    NavigateTo(PageType.Help);
                }
                else if ((e.Key is VirtualKey.Q or VirtualKey.W) && IS_CONTROL_PRESSED)
                {
                    MainApp.Instance.MainWindow.Close();
                }
                else if (e.Key is VirtualKey.F5 || (e.Key is VirtualKey.R && IS_CONTROL_PRESSED))
                {
                    (currentPage as IKeyboardShortcutListener)?.ReloadTriggered();
                }
                else if (e.Key is VirtualKey.F && IS_CONTROL_PRESSED)
                {
                    (currentPage as IKeyboardShortcutListener)?.SearchTriggered();
                }
                else if (e.Key is VirtualKey.A && IS_CONTROL_PRESSED)
                {
                    (currentPage as IKeyboardShortcutListener)?.SelectAllTriggered();
                }
            };

            LoadDefaultPage();

            if (CoreTools.IsAdministrator() && !Settings.Get("AlreadyWarnedAboutAdmin"))
            {
                Settings.Set("AlreadyWarnedAboutAdmin", true);
                DialogHelper.WarnAboutAdminRights();
            }
        }

        public void LoadDefaultPage()
        {
            PageType type = Settings.GetValue("StartupPage") switch
            {
                "discover" => PageType.Discover,
                "updates" => PageType.Updates,
                "installed" => PageType.Installed,
                "bundles" => PageType.Bundles,
                "settings" => PageType.Settings,
                _ => MainApp.Instance.TooltipStatus.AvailableUpdates > 0 ? PageType.Updates : PageType.Discover
            };
            NavigateTo(type);
        }

        private void DiscoverNavButton_Click(object sender, EventArgs e)
            => NavigateTo(PageType.Discover);

        private void InstalledNavButton_Click(object sender, EventArgs e)
            => NavigateTo(PageType.Installed);

        private void UpdatesNavButton_Click(object sender, EventArgs e)
            => NavigateTo(PageType.Updates);

        private void BundlesNavButton_Click(object sender, EventArgs e)
            => NavigateTo(PageType.Bundles);

        private void MoreNavButton_Click(object sender, EventArgs e)
        {
            SelectNavButtonForPage(PageType.OwnLog);
            (VersionMenuItem as MenuFlyoutItem).Text = CoreTools.Translate("WingetUI Version {0}", CoreData.VersionName);
            MoreNavButtonMenu.ShowAt(MoreNavButton, new FlyoutShowOptions { ShowMode = FlyoutShowMode.Standard });
        }

        private Page GetPageForType(PageType type)
            => type switch
            {
                PageType.Discover => DiscoverPage,
                PageType.Updates => UpdatesPage,
                PageType.Installed => InstalledPage,
                PageType.Bundles => BundlesPage,
                PageType.Settings => SettingsPage ??= new SettingsPage(),
                PageType.OwnLog => UniGetUILogPage ??= new UniGetUILogPage(),
                PageType.ManagerLog => ManagerLogPage ??= new ManagerLogsPage(),
                PageType.OperationHistory => OperationHistoryPage ??= new OperationHistoryPage(),
                PageType.Help => HelpPage ??= new HelpPage(),
                PageType.Null => throw new InvalidCastException("Page type is Null"),
                _ => throw new InvalidDataException($"Unknown page type {type}")
            };

        private static PageType GetNextPage(PageType type)
            => type switch
            {
                // Default loop
                PageType.Discover => PageType.Updates,
                PageType.Updates => PageType.Installed,
                PageType.Installed => PageType.Bundles,
                PageType.Bundles => PageType.Settings,
                PageType.Settings => PageType.Discover,

                // "Extra" pages
                PageType.OperationHistory => PageType.Discover,
                PageType.OwnLog => PageType.Discover,
                PageType.ManagerLog => PageType.Discover,
                PageType.Help => PageType.Discover,
                PageType.Null => PageType.Discover,
                _ => throw new InvalidDataException($"Unknown page type {type}")
            };

        private static PageType GetPreviousPage(PageType type)
            => type switch
            {
                // Default loop
                PageType.Discover => PageType.Settings,
                PageType.Updates => PageType.Discover,
                PageType.Installed => PageType.Updates,
                PageType.Bundles => PageType.Installed,
                PageType.Settings => PageType.Bundles,

                // "Extra" pages
                PageType.OperationHistory => PageType.Discover,
                PageType.OwnLog => PageType.Discover,
                PageType.ManagerLog => PageType.Discover,
                PageType.Help => PageType.Discover,
                PageType.Null => PageType.Discover,
                _ => throw new InvalidDataException($"Unknown page type {type}")
            };

        private void SettingsNavButton_Click(object sender, EventArgs e)
            => NavigateTo(PageType.Settings);

        private void SelectNavButtonForPage(PageType page)
        {
            DiscoverNavButton.IsChecked = page is PageType.Discover;
            UpdatesNavButton.IsChecked = page is PageType.Updates;
            InstalledNavButton.IsChecked = page is PageType.Installed;
            BundlesNavButton.IsChecked = page is PageType.Bundles;

            SettingsNavButton.IsChecked = page is PageType.Settings;
            AboutNavButton.IsChecked = false;
            MoreNavButton.IsChecked = page is PageType.Help or PageType.ManagerLog or PageType.OperationHistory or PageType.OwnLog;
        }

        private async void AboutNavButton_Click(object sender, EventArgs e)
        {
            SelectNavButtonForPage(PageType.Null);
            AboutNavButton.IsChecked = true;
            await DialogHelper.ShowAboutUniGetUI();
            SelectNavButtonForPage(CurrentPage_t);
        }

        public void NavigateTo(PageType NewPage_t)
        {
            SelectNavButtonForPage(NewPage_t);
            if (CurrentPage_t == NewPage_t) return;

            Page NewPage = GetPageForType(NewPage_t);

            if (!AddedPages.TryGetValue(NewPage, out _))
            {
                AddedPages.Add(NewPage);
                Grid.SetColumn(NewPage, 0);
                Grid.SetRow(NewPage, 0);
                MainContentPresenterGrid.Children.Add(NewPage);
            }

            foreach (Page page in AddedPages)
            {
                bool IS_MAIN_PAGE = (page == NewPage);
                page.Visibility =  IS_MAIN_PAGE? Visibility.Visible : Visibility.Collapsed;
                page.IsEnabled = IS_MAIN_PAGE;
            }

            OldPage_t = CurrentPage_t;
            CurrentPage_t = NewPage_t;

            (NewPage as AbstractPackagesPage)?.FocusPackageList();
            (NewPage as IEnterLeaveListener)?.OnEnter();
            if (OldPage_t is not PageType.Null)
            {
                Page oldPage = GetPageForType(OldPage_t);
                (oldPage as IEnterLeaveListener)?.OnLeave();
            }
        }

        private void ReleaseNotesMenu_Click(object sender, RoutedEventArgs e)
            => DialogHelper.ShowReleaseNotes();

        private void OperationHistoryMenu_Click(object sender, RoutedEventArgs e)
            => NavigateTo(PageType.OperationHistory);

        private void ManagerLogsMenu_Click(object sender, RoutedEventArgs e)
            => OpenManagerLogs();

        public void OpenManagerLogs(IPackageManager? manager = null)
        {
            NavigateTo(PageType.ManagerLog);
            if(manager is not null) ManagerLogPage?.LoadForManager(manager);
        }

        public void UniGetUILogs_Click(object sender, RoutedEventArgs e)
            => NavigateTo(PageType.OwnLog);

        private void HelpMenu_Click(object sender, RoutedEventArgs e)
            => ShowHelp();

        public void ShowHelp()
            => NavigateTo(PageType.Help);

        private void QuitUniGetUI_Click(object sender, RoutedEventArgs e)
            => MainApp.Instance.DisposeAndQuit();
    }
}
