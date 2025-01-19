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
using UniGetUI.PackageEngine.Operations;
using CommunityToolkit.WinUI.Controls;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine;
using UniGetUI.PackageOperations;

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
        private DiscoverSoftwarePage DiscoverPage;
        private SoftwareUpdatesPage UpdatesPage;
        private InstalledPackagesPage InstalledPage;
        private PackageBundlesPage BundlesPage;
        private SettingsPage? SettingsPage;
        private UniGetUILogPage? UniGetUILogPage;
        private ManagerLogsPage? ManagerLogPage;
        private OperationHistoryPage? OperationHistoryPage;
        private HelpPage? HelpPage;

        private PageType OldPage_t = PageType.Null;
        private PageType CurrentPage_t = PageType.Null;
        private Page CurrentPage = null!;
        private readonly HashSet<Page> AddedPages = new();

        public MainView()
        {
            InitializeComponent();
            OperationList.ItemContainerTransitions = null;
            OperationList.ItemsSource = MainApp.Operations._operationList;
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

            UpdateOperationsLayout();
            MainApp.Operations._operationList.CollectionChanged += (_, _) => UpdateOperationsLayout();
        }

        public page_t RequestPageIntoView<page_t>(PageType page) where page_t: AbstractPackagesPage
        {
            NavigateTo(page);
            return CurrentPage as page_t ?? throw new InvalidCastException("Invalid page_t");
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
                _ => MainApp.Tooltip.AvailableUpdates > 0 ? PageType.Updates : PageType.Discover
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

            CurrentPage = NewPage;
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


        private bool ResizingOPLayout;
        private int OpListChanges;


        bool isCollapsed;

        private void UpdateOperationsLayout()
        {
            OpListChanges++;

            ResizingOPLayout = true;
            int OpCount = MainApp.Operations._operationList.Count;
            int maxHeight = Math.Max((OpCount * 58) - 7, 0);

            MainContentPresenterGrid.RowDefinitions[2].MaxHeight = maxHeight;

            if (OpCount > 0)
            {
                if(isCollapsed)
                {
                    MainContentPresenterGrid.RowDefinitions[2].Height = new GridLength(0);
                    MainContentPresenterGrid.RowDefinitions[1].Height = new GridLength(16);
                    OperationSplitter.Visibility = Visibility.Visible;
                    OperationSplitterMenuButton.Visibility = Visibility.Visible;
                    // OperationScrollView.Visibility = Visibility.Collapsed;
                    OperationSplitter.IsEnabled = false;
                }
                else
                {
                    //if (int.TryParse(Settings.GetValue("OperationHistoryPreferredHeight"), out int setHeight) && setHeight < maxHeight)
                    //    MainContentPresenterGrid.RowDefinitions[2].Height = new GridLength(setHeight);
                    //else
                        MainContentPresenterGrid.RowDefinitions[2].Height = new GridLength(Math.Min(maxHeight, 200));
                    MainContentPresenterGrid.RowDefinitions[1].Height = new GridLength(16);
                    OperationSplitter.Visibility = Visibility.Visible;
                    OperationSplitterMenuButton.Visibility = Visibility.Visible;
                    // OperationScrollView.Visibility = Visibility.Visible;
                    OperationSplitter.IsEnabled = true;
                }
            }
            else
            {
                MainContentPresenterGrid.RowDefinitions[1].Height = new GridLength(0);
                MainContentPresenterGrid.RowDefinitions[2].Height = new GridLength(0);
                OperationSplitter.Visibility = Visibility.Collapsed;
                OperationSplitterMenuButton.Visibility = Visibility.Collapsed;
                // OperationScrollView.Visibility = Visibility.Collapsed;
            }
            ResizingOPLayout = false;
        }

        // int lastSaved = -1;
        private async void OperationScrollView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ResizingOPLayout)
                return;

            if(OpListChanges > 0)
            {
                OpListChanges--;
                return;
            }

            //lastSaved = (int)e.NewSize.Height;
            //await Task.Delay(100);
            //if ((int)e.NewSize.Height == lastSaved)
            //    Settings.SetValue("OperationHistoryPreferredHeight", lastSaved.ToString());
        }

        private void OperationSplitterMenuButton_Click(object sender, RoutedEventArgs e)
        {
            OperationListMenu.ShowAt(OperationSplitterMenuButton, new FlyoutShowOptions { ShowMode = FlyoutShowMode.Standard });
        }

        private void ExpandCollapseOpList_Click(object sender, RoutedEventArgs e)
        {
            if (isCollapsed)
            {
                isCollapsed = false;
                ExpandCollapseOpList.Content = new FontIcon() { Glyph = "\uE96E", FontSize = 14 };
                UpdateOperationsLayout();
            }
            else
            {
                isCollapsed = true;
                ExpandCollapseOpList.Content = new FontIcon() { Glyph = "\uE96D", FontSize = 14 };
                UpdateOperationsLayout();
            }
        }

        private void CancellAllOps_Click(object sender, RoutedEventArgs e)
        {
            foreach (var widget in MainApp.Operations._operationList)
            {
                var operation = widget.Operation;
                if (operation.Status is OperationStatus.InQueue or OperationStatus.Running)
                    operation.Cancel();
            }
        }

        private void RetryFailedOps_Click(object sender, RoutedEventArgs e)
        {
            foreach (var widget in MainApp.Operations._operationList)
            {
                var operation = widget.Operation;
                if (operation.Status is OperationStatus.Failed)
                    operation.Retry(AbstractOperation.RetryMode.Retry);
            }
        }

        private void ClearSuccessfulOps_Click(object sender, RoutedEventArgs e)
        {
            foreach (var widget in MainApp.Operations._operationList.ToArray())
            {
                var operation = widget.Operation;
                if (operation.Status is OperationStatus.Succeeded)
                    widget.Close();
            }
        }
    }
}
