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
using UniGetUI.Interface.Widgets;
using Windows.UI.Core;
using UniGetUI.Pages.DialogPages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface
{
    public sealed partial class MainView : UserControl
    {
        public SettingsInterface SettingsPage;
        public DiscoverSoftwarePage DiscoverPage;
        public SoftwareUpdatesPage UpdatesPage;
        public InstalledPackagesPage InstalledPage;
        public HelpDialog? HelpPage;
        public PackageBundlesPage BundlesPage;
        public Page? OldPage;
        public Page? CurrentPage;
        private readonly Dictionary<Page, NavButton> PageButtonReference = [];

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
            SettingsPage = new SettingsInterface();

            foreach (Page page in new Page[] { DiscoverPage, UpdatesPage, InstalledPage, SettingsPage, BundlesPage })
            {
                Grid.SetColumn(page, 0);
                Grid.SetRow(page, 0);
                MainContentPresenterGrid.Children.Add(page);
            }

            PageButtonReference.Add(DiscoverPage, DiscoverNavButton);
            PageButtonReference.Add(UpdatesPage, UpdatesNavButton);
            PageButtonReference.Add(InstalledPage, InstalledNavButton);
            PageButtonReference.Add(SettingsPage, SettingsNavButton);
            PageButtonReference.Add(BundlesPage, BundlesNavButton);

            DiscoverNavButton.ForceClick();

            if (CoreTools.IsAdministrator() && !Settings.Get("AlreadyWarnedAboutAdmin"))
            {
                Settings.Set("AlreadyWarnedAboutAdmin", true);
                DialogHelper.WarnAboutAdminRights();
            }

            Dictionary<Page, NavButton> NextPageReference = new()
            {
                { DiscoverPage, UpdatesNavButton },
                { UpdatesPage, InstalledNavButton },
                { InstalledPage, BundlesNavButton },
                { BundlesPage, SettingsNavButton },
                { SettingsPage, DiscoverNavButton },
            };

            Dictionary<Page, NavButton> PreviousTabReference = new()
            {
                { DiscoverPage, SettingsNavButton },
                { UpdatesPage, DiscoverNavButton },
                { InstalledPage, UpdatesNavButton },
                { BundlesPage, InstalledNavButton },
                { SettingsPage, BundlesNavButton },
            };

            KeyUp += (s, e) =>
            {
                bool IS_CONTROL_PRESSED = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
                bool IS_SHIFT_PRESSED = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

                if (e.Key == Windows.System.VirtualKey.Tab && IS_CONTROL_PRESSED)
                {
                    if (CurrentPage is not null)
                    {
                        if (!IS_SHIFT_PRESSED)
                        {
                            if (NextPageReference.TryGetValue(CurrentPage, out var nextPage))
                            {
                                nextPage.ForceClick();
                            }
                            else
                            {
                                DiscoverNavButton.ForceClick();
                            }
                        }
                        else
                        {
                            if (PreviousTabReference.TryGetValue(CurrentPage, out var prevTab))
                            {
                                prevTab.ForceClick();
                            }
                            else
                            {
                                DiscoverNavButton.ForceClick();
                            }
                        }
                    }
                }
                else if (e.Key == Windows.System.VirtualKey.F1)
                {
                    HelpMenu_Click(s, e);
                }
                else if (IS_CONTROL_PRESSED && (e.Key == Windows.System.VirtualKey.Q || e.Key == Windows.System.VirtualKey.W))
                {
                    MainApp.Instance.MainWindow.Close();
                }
                else if (e.Key == Windows.System.VirtualKey.F5 || (IS_CONTROL_PRESSED && e.Key == Windows.System.VirtualKey.R))
                {
                    (CurrentPage as IPageWithKeyboardShortcuts)?.ReloadTriggered();
                }
                else if (IS_CONTROL_PRESSED && e.Key == Windows.System.VirtualKey.F)
                {
                    (CurrentPage as IPageWithKeyboardShortcuts)?.SearchTriggered();
                }
                else if (IS_CONTROL_PRESSED && e.Key == Windows.System.VirtualKey.A)
                {
                    (CurrentPage as IPageWithKeyboardShortcuts)?.SelectAllTriggered();
                }
            };
        }

        private void DiscoverNavButton_Click(object sender, EventArgs e)
        {
            NavigateToPage(DiscoverPage);
        }

        private void InstalledNavButton_Click(object sender, EventArgs e)
        {
            NavigateToPage(InstalledPage);
        }

        private void UpdatesNavButton_Click(object sender, EventArgs e)
        {
            NavigateToPage(UpdatesPage);
        }

        private void BundlesNavButton_Click(object sender, EventArgs e)
        {
            NavigateToPage(BundlesPage);
        }

        private void MoreNavButton_Click(object sender, EventArgs e)
        {

            foreach (NavButton button in MainApp.Instance.MainWindow.NavButtonList)
            {
                button.ToggleButton.IsChecked = false;
            }

            MoreNavButton.ToggleButton.IsChecked = true;

            (VersionMenuItem as MenuFlyoutItem).Text = CoreTools.Translate("WingetUI Version {0}", CoreData.VersionName);
            MoreNavButtonMenu.ShowAt(MoreNavButton, new FlyoutShowOptions { ShowMode = FlyoutShowMode.Standard });

            MoreNavButtonMenu.Closed += (_, _) =>
            {
                foreach (NavButton button in MainApp.Instance.MainWindow.NavButtonList)
                {
                    button.ToggleButton.IsChecked = button == PageButtonReference[CurrentPage ?? DiscoverPage];
                }
            };
        }

        private void SettingsNavButton_Click(object sender, EventArgs e)
        {
            NavigateToPage(SettingsPage);
        }

        private async void AboutNavButton_Click(object sender, EventArgs e)
        {

            foreach (NavButton button in MainApp.Instance.MainWindow.NavButtonList)
            {
                button.ToggleButton.IsChecked = false;
            }

            await DialogHelper.ShowAboutUniGetUI();

            foreach (NavButton button in MainApp.Instance.MainWindow.NavButtonList)
            {
                button.ToggleButton.IsChecked = button == PageButtonReference[CurrentPage ?? DiscoverPage];
            }
        }

        private void NavigateToPage(Page TargetPage)
        {
            if (!PageButtonReference.TryGetValue(TargetPage, out var pageButton))
            {
                PageButtonReference.Add(TargetPage, MoreNavButton);
                Grid.SetColumn(TargetPage, 0);
                Grid.SetRow(TargetPage, 0);
                MainContentPresenterGrid.Children.Add(TargetPage);
            }
            foreach (NavButton button in MainApp.Instance.MainWindow.NavButtonList)
            {

                button.ToggleButton.IsChecked = button == pageButton;
            }

            foreach (Page page in PageButtonReference.Keys)
            {
                page.Visibility = (page == TargetPage) ? Visibility.Visible : Visibility.Collapsed;
            }

            OldPage = CurrentPage;
            CurrentPage = TargetPage;

            (CurrentPage as AbstractPackagesPage)?.FocusPackageList();
        }

        private void ReleaseNotesMenu_Click(object sender, RoutedEventArgs e)
        {
            DialogHelper.ShowReleaseNotes();
        }

        private void OperationHistoryMenu_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new OperationHistoryPage());
        }

        private void ManagerLogsMenu_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new ManagerLogsPage());
        }

        public void UniGetUILogs_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new AppLogPage());
        }

        private void HelpMenu_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
        public void ShowHelp()
        {
            if (HelpPage is null)
            {
                HelpPage = new HelpDialog();
            }

            NavigateToPage(HelpPage);
        }

        private void QuitUniGetUI_Click(object sender, RoutedEventArgs e)
        {
            MainApp.Instance.DisposeAndQuit();
        }
    }
}
