using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core.Data;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Dialogs;
using UniGetUI.Interface.Pages;
using UniGetUI.Interface.Pages.LogPage;
using UniGetUI.Interface.SoftwarePages;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Serializable;
using Windows.UI.Core;

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
        public InfoBadge UpdatesBadge;
        public InfoBadge BundleBadge;
        public StackPanel OperationStackPanel;
        private readonly Dictionary<Page, NavButton> PageButtonReference = [];

        public MainView()
        {
            InitializeComponent();
            UpdatesBadge = __updates_count_badge;
            BundleBadge = __bundle_count_badge;
            OperationStackPanel = __operations_list_stackpanel;
            DiscoverPage = new DiscoverSoftwarePage();
            UpdatesPage = new SoftwareUpdatesPage
            {
                ExternalCountBadge = UpdatesBadge
            };
            InstalledPage = new InstalledPackagesPage();
            BundlesPage = new PackageBundlesPage();
            SettingsPage = new SettingsInterface();

            int i = 0;
            foreach (Page page in new Page[] { DiscoverPage, UpdatesPage, InstalledPage, SettingsPage, BundlesPage })
            {
                Grid.SetColumn(page, 0);
                Grid.SetRow(page, 0);
                MainContentPresenterGrid.Children.Add(page);
                i++;
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
                WarnAboutAdminRights();
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
                    if (CurrentPage != null)
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

            MoreNavButtonMenu.Closed += (s, e) =>
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
            ContentDialog? AboutDialog = new();
            AboutUniGetUI AboutPage = new();
            AboutDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            AboutDialog.XamlRoot = XamlRoot;
            AboutDialog.Resources["ContentDialogMaxWidth"] = 1200;
            AboutDialog.Resources["ContentDialogMaxHeight"] = 1000;
            AboutDialog.Content = AboutPage;
            AboutDialog.PrimaryButtonText = CoreTools.Translate("Close");
            AboutPage.Close += (s, e) => { AboutDialog.Hide(); };
            foreach (NavButton button in MainApp.Instance.MainWindow.NavButtonList)
            {
                button.ToggleButton.IsChecked = false;
            }

            await MainApp.Instance.MainWindow.ShowDialogAsync(AboutDialog);

            AboutDialog.Content = null;
            foreach (NavButton button in MainApp.Instance.MainWindow.NavButtonList)
            {
                button.ToggleButton.IsChecked = button == PageButtonReference[CurrentPage ?? DiscoverPage];
            }

            AboutDialog = null;
        }

        public async Task ManageIgnoredUpdatesDialog()
        {
            ContentDialog? UpdatesDialog = new()
            {
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                XamlRoot = XamlRoot
            };
            UpdatesDialog.Resources["ContentDialogMaxWidth"] = 1200;
            UpdatesDialog.Resources["ContentDialogMaxHeight"] = 1000;
            UpdatesDialog.SecondaryButtonText = CoreTools.Translate("Close");
            UpdatesDialog.PrimaryButtonText = CoreTools.Translate("Reset");
            UpdatesDialog.DefaultButton = ContentDialogButton.Secondary;
            UpdatesDialog.Title = CoreTools.Translate("Manage ignored updates");
            IgnoredUpdatesManager IgnoredUpdatesPage = new();
            UpdatesDialog.PrimaryButtonClick += IgnoredUpdatesPage.ManageIgnoredUpdates_SecondaryButtonClick;
            UpdatesDialog.Content = IgnoredUpdatesPage;
            IgnoredUpdatesPage.Close += (s, e) => { UpdatesDialog.Hide(); };

            _ = IgnoredUpdatesPage.UpdateData();
            await MainApp.Instance.MainWindow.ShowDialogAsync(UpdatesDialog);

            UpdatesDialog.Content = null;
            UpdatesDialog = null;
        }

        public async Task<ContentDialogResult> ShowOperationFailedDialog(
            IEnumerable<string> processOutput,
            string dialogTitle,
            string shortDescription)
        {
            ContentDialog dialog = new()
            {
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                XamlRoot = XamlRoot
            };
            dialog.Resources["ContentDialogMaxWidth"] = 850;
            dialog.Resources["ContentDialogMaxHeight"] = 800;
            dialog.Title = dialogTitle;

            Grid grid = new()
            {
                RowSpacing = 16,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            TextBlock headerContent = new()
            {
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = $"{shortDescription}. "
                        + CoreTools.Translate("Please see the Command-line Output or refer to the Operation History for further information about the issue.")
            };

            StackPanel HeaderPanel = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };

            HeaderPanel.Children.Add(new LocalIcon(Enums.IconType.Console)
            {
                VerticalAlignment = VerticalAlignment.Center,
                Height = 24,
                Width = 24,
                HorizontalAlignment = HorizontalAlignment.Left
            });

            HeaderPanel.Children.Add(new TextBlock
            {
                Text = CoreTools.Translate("Command-line Output"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

            RichTextBlock CommandLineOutput = new()
            {
                FontFamily = new FontFamily("Consolas"),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            ScrollViewer ScrollView = new()
            {
                BorderBrush = new SolidColorBrush(),
                Content = CommandLineOutput,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            Grid OutputGrid = new();
            OutputGrid.Children.Add(ScrollView);
            OutputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            OutputGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(ScrollView, 0);
            Grid.SetRow(ScrollView, 0);

            Expander expander = new()
            {
                Header = HeaderPanel,
                Content = OutputGrid,
                CornerRadius = new CornerRadius(8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            Paragraph par = new();
            foreach (string line in processOutput)
            {
                par.Inlines.Add(new Run { Text = line + "\x0a" });
            }

            CommandLineOutput.Blocks.Add(par);

            grid.Children.Add(headerContent);
            grid.Children.Add(expander);
            Grid.SetRow(headerContent, 0);
            Grid.SetRow(expander, 1);

            dialog.Content = grid;
            dialog.PrimaryButtonText = CoreTools.Translate("Retry");
            dialog.CloseButtonText = CoreTools.Translate("Close");
            dialog.DefaultButton = ContentDialogButton.Primary;

            return await MainApp.Instance.MainWindow.ShowDialogAsync(dialog);
        }

        public async void WarnAboutAdminRights()
        {
            ContentDialog AdminDialog = new()
            {
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style
            };

            while (XamlRoot == null)
            {
                await Task.Delay(100);
            }

            AdminDialog.XamlRoot = XamlRoot;
            AdminDialog.PrimaryButtonText = CoreTools.Translate("I understand");
            AdminDialog.DefaultButton = ContentDialogButton.Primary;
            AdminDialog.Title = CoreTools.Translate("Administrator privileges");
            AdminDialog.Content = CoreTools.Translate("WingetUI has been ran as administrator, which is not recommended. When running WingetUI as administrator, EVERY operation launched from WingetUI will have administrator privileges. You can still use the program, but we highly recommend not running WingetUI with administrator privileges.");

            await MainApp.Instance.MainWindow.ShowDialogAsync(AdminDialog);
        }

        /// <summary>
        /// Will update the Installation Options for the given Package, and will return whether the user choose to continue
        /// </summary>
        public async Task<bool> ShowInstallationSettingsAndContinue(IPackage package, OperationType operation)
        {
            var options = (await InstallationOptions.FromPackageAsync(package)).AsSerializable();

            var result = await ShowInstallOptionsDialog(package, operation, options);
            InstallationOptions newOptions = await InstallationOptions.FromPackageAsync(package);
            newOptions.FromSerializable(result.Item1);
            await newOptions.SaveToDiskAsync();

            return result.Item2 == ContentDialogResult.Secondary;
        }

        /// <summary>
        /// Will update the Installation Options for the given imported package
        /// </summary>
        public async Task<(SerializableInstallationOptions_v1, ContentDialogResult)> ShowInstallOptionsDialog_ImportedPackage(ImportedPackage importedPackage)
        {
            var result = await ShowInstallOptionsDialog(importedPackage, OperationType.None, importedPackage.installation_options);
            importedPackage.installation_options = result.Item1;
            return result;
        }

        private async Task<(SerializableInstallationOptions_v1, ContentDialogResult)> ShowInstallOptionsDialog(
            IPackage package,
            OperationType operation,
            SerializableInstallationOptions_v1 options)
        {
            InstallOptionsPage OptionsPage = new(package, options);

            ContentDialog? OptionsDialog = new()
            {
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                XamlRoot = XamlRoot
            };
            OptionsDialog.Resources["ContentDialogMaxWidth"] = 1200;
            OptionsDialog.Resources["ContentDialogMaxHeight"] = 1000;

            OptionsDialog.SecondaryButtonText = operation switch
            {
                OperationType.Install => CoreTools.Translate("Install"),
                OperationType.Uninstall => CoreTools.Translate("Uninstall"),
                OperationType.Update => CoreTools.Translate("Update"),
                _ => ""
            };

            OptionsDialog.PrimaryButtonText = CoreTools.Translate("Save and close");
            OptionsDialog.DefaultButton = ContentDialogButton.Secondary;
            OptionsDialog.Title = CoreTools.Translate("{0} installation options", package.Name);
            OptionsDialog.Content = OptionsPage;
            OptionsPage.Close += (s, e) => { OptionsDialog.Hide(); };

            ContentDialogResult result = await MainApp.Instance.MainWindow.ShowDialogAsync(OptionsDialog);
            return (await OptionsPage.GetUpdatedOptions(), result);
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

        private async void ReleaseNotesMenu_Click(object sender, RoutedEventArgs e)
        {

            ContentDialog? NotesDialog = new()
            {
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                XamlRoot = XamlRoot
            };
            NotesDialog.Resources["ContentDialogMaxWidth"] = 12000;
            NotesDialog.Resources["ContentDialogMaxHeight"] = 10000;
            NotesDialog.CloseButtonText = CoreTools.Translate("Close");
            NotesDialog.Title = CoreTools.Translate("Release notes");
            ReleaseNotes? notes = new();
            notes.Close += (s, e) => NotesDialog.Hide(); 
            NotesDialog.Content = notes;
            NotesDialog.SizeChanged += (s, e) =>
            {
                notes.MinWidth = Math.Abs(ActualWidth - 300);
                notes.MinHeight = Math.Abs(ActualHeight - 200);
            };

            await MainApp.Instance.MainWindow.ShowDialogAsync(NotesDialog);

            notes.Dispose();
            notes = null;
            NotesDialog = null;
        }

        public async Task ShowPackageDetails(IPackage package, OperationType ActionOperation)
        {
            PackageDetailsPage? DetailsPage = new(package, ActionOperation);

            ContentDialog? DetailsDialog = new()
            {
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                XamlRoot = XamlRoot
            };
            DetailsDialog.Resources["ContentDialogMaxWidth"] = 8000;
            DetailsDialog.Resources["ContentDialogMaxHeight"] = 4000;
            DetailsDialog.Content = DetailsPage;
            DetailsDialog.SizeChanged += (s, e) =>
            {
                int hOffset = (ActualWidth < 1300) ? 100 : 300;
                DetailsPage.MinWidth = Math.Abs(ActualWidth - hOffset);
                DetailsPage.MinHeight = Math.Abs(ActualHeight - 100);
                DetailsPage.MaxWidth = Math.Abs(ActualWidth - hOffset);
                DetailsPage.MaxHeight = Math.Abs(ActualHeight - 100);
            };

            DetailsPage.Close += (s, e) => { DetailsDialog.Hide(); };

            await MainApp.Instance.MainWindow.ShowDialogAsync(DetailsDialog);

            DetailsDialog.Content = null;
            DetailsDialog = null;

        }

        public async Task<bool> ConfirmUninstallation(IPackage package)
        {
            ContentDialog dialog = new()
            {
                XamlRoot = XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = CoreTools.Translate("Are you sure?"),
                PrimaryButtonText = CoreTools.Translate("No"),
                SecondaryButtonText = CoreTools.Translate("Yes"),
                DefaultButton = ContentDialogButton.Primary,
                Content = CoreTools.Translate("Do you really want to uninstall {0}?", package.Name)
            };

            return await MainApp.Instance.MainWindow.ShowDialogAsync(dialog) == ContentDialogResult.Secondary;
        }

        public async Task<bool> ConfirmUninstallation(IEnumerable<IPackage> packages)
        {
            if (!packages.Any())
            {
                return false;
            }

            if (packages.Count() == 1)
            {
                return await ConfirmUninstallation(packages.First());
            }

            ContentDialog dialog = new()
            {
                XamlRoot = XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = CoreTools.Translate("Are you sure?"),
                PrimaryButtonText = CoreTools.Translate("No"),
                SecondaryButtonText = CoreTools.Translate("Yes"),
                DefaultButton = ContentDialogButton.Primary
            };

            StackPanel p = new();
            p.Children.Add(new TextBlock
            {
                Text = CoreTools.Translate("Do you really want to uninstall the following {0} packages?", packages.Count()),
                Margin = new Thickness(0, 0, 0, 5)
            });

            string pkgList = "";
            foreach (Package package in packages)
            {
                pkgList += " ● " + package.Name + "\x0a";
            }

            TextBlock PackageListTextBlock = new() { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"), Text = pkgList };
            p.Children.Add(new ScrollView { Content = PackageListTextBlock, MaxHeight = 200 });

            dialog.Content = p;

            return await MainApp.Instance.MainWindow.ShowDialogAsync(dialog) == ContentDialogResult.Secondary;
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
            if (HelpPage == null)
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
