using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniGetUI.Core;
using UniGetUI.Core.Data;
using UniGetUI.Interface.Dialogs;
using UniGetUI.Interface.Pages;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.Core.Logging;
using Windows.UI.Core;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.PackageEngine.PackageClasses;
using System.Reflection.Emit;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.SoftwarePages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface
{
    public sealed partial class MainView : UserControl
    {
        public SettingsInterface SettingsPage;
        public NewDiscoverSoftwarePage DiscoverPage;
        public NewSoftwareUpdatesPage UpdatesPage;
        public NewInstalledPackagesPage InstalledPage;
        public HelpDialog? HelpPage;
        public PackageBundlePage BundlesPage;
        public Page? OldPage;
        public Page? CurrentPage;
        public InfoBadge UpdatesBadge;
        public InfoBadge BundleBadge;
        public StackPanel OperationStackPanel;
        private Dictionary<Page, NavButton> PageButtonReference = new();

        private AboutUniGetUI AboutPage;
        private IgnoredUpdatesManager IgnoredUpdatesPage;

        public MainView()
        {
            InitializeComponent();
            UpdatesBadge = __updates_count_badge;
            BundleBadge = __bundle_count_badge;
            OperationStackPanel = __operations_list_stackpanel;
            DiscoverPage = new NewDiscoverSoftwarePage();
            UpdatesPage = new NewSoftwareUpdatesPage();
            UpdatesPage.ExternalCountBadge = UpdatesBadge;
            InstalledPage = new NewInstalledPackagesPage();
            BundlesPage = new PackageBundlePage();
            SettingsPage = new SettingsInterface();
            AboutPage = new AboutUniGetUI();
            IgnoredUpdatesPage = new IgnoredUpdatesManager();

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

            if (!Settings.Get("AlreadyWarnedAboutNameChange"))
            {
                Settings.Set("AlreadyWarnedAboutNameChange", true);
                WarnAboutNewName();
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
                if (e.Key == Windows.System.VirtualKey.Tab && InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
                {
                    if(CurrentPage != null)
                        if (!InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
                        {
                            if (NextPageReference.ContainsKey(CurrentPage))
                                NextPageReference[CurrentPage].ForceClick();
                            else
                                DiscoverNavButton.ForceClick();
                        }
                        else
                        {
                            if (NextPageReference.ContainsKey(CurrentPage))
                                PreviousTabReference[CurrentPage].ForceClick();
                            else
                                DiscoverNavButton.ForceClick();
                        }
                }
            };
        }

        private void DiscoverNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            NavigateToPage(DiscoverPage);
        }

        private void InstalledNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            NavigateToPage(InstalledPage);
        }

        private void UpdatesNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            NavigateToPage(UpdatesPage);
        }

        private void BundlesNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            NavigateToPage(BundlesPage);
        }

        private void MoreNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {

            foreach (NavButton button in MainApp.Instance.MainWindow.NavButtonList)
                button.ToggleButton.IsChecked = false;
            MoreNavButton.ToggleButton.IsChecked = true;

            (VersionMenuItem as MenuFlyoutItem).Text = CoreTools.Translate("WingetUI Version {0}").Replace("{0}", CoreData.VersionName);
            MoreNavButtonMenu.ShowAt(MoreNavButton, new FlyoutShowOptions() { ShowMode = FlyoutShowMode.Standard });

            MoreNavButtonMenu.Closed += (s, e) =>
            {
                foreach (NavButton button in MainApp.Instance.MainWindow.NavButtonList)
                    button.ToggleButton.IsChecked = (button == PageButtonReference[CurrentPage ?? DiscoverPage]);
            };
        }

        private void SettingsNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            NavigateToPage(SettingsPage);
        }

        private async void AboutNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            ContentDialog? AboutDialog = new();
            AboutDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            AboutDialog.XamlRoot = XamlRoot;
            AboutDialog.Resources["ContentDialogMaxWidth"] = 1200;
            AboutDialog.Resources["ContentDialogMaxHeight"] = 1000;
            AboutDialog.Content = AboutPage;
            AboutDialog.PrimaryButtonText = CoreTools.Translate("Close");
            foreach (NavButton button in MainApp.Instance.MainWindow.NavButtonList)
                button.ToggleButton.IsChecked = false;

            await MainApp.Instance.MainWindow.ShowDialogAsync(AboutDialog);

            AboutDialog.Content = null;
            foreach (NavButton button in MainApp.Instance.MainWindow.NavButtonList)
                button.ToggleButton.IsChecked = (button == PageButtonReference[CurrentPage ?? DiscoverPage]);
            AboutDialog = null;
        }

        public async Task ManageIgnoredUpdatesDialog()
        {
            ContentDialog? UpdatesDialog = new();
            UpdatesDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            UpdatesDialog.XamlRoot = XamlRoot;
            UpdatesDialog.Resources["ContentDialogMaxWidth"] = 1200;
            UpdatesDialog.Resources["ContentDialogMaxHeight"] = 1000;
            UpdatesDialog.SecondaryButtonText = CoreTools.Translate("Close");
            UpdatesDialog.PrimaryButtonText = CoreTools.Translate("Reset");
            UpdatesDialog.DefaultButton = ContentDialogButton.Secondary;
            UpdatesDialog.Title = CoreTools.Translate("Manage ignored updates");
            UpdatesDialog.PrimaryButtonClick += IgnoredUpdatesPage.ManageIgnoredUpdates_SecondaryButtonClick;
            UpdatesDialog.Content = IgnoredUpdatesPage;

            _ = IgnoredUpdatesPage.UpdateData();
            await MainApp.Instance.MainWindow.ShowDialogAsync(UpdatesDialog);

            UpdatesDialog.Content = null;
            UpdatesDialog = null;
        }

        public async void WarnAboutAdminRights()
        {
            ContentDialog AdminDialog = new();
            AdminDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;

            while (XamlRoot == null)
            {
                await Task.Delay(100);
            }

            AdminDialog.XamlRoot = XamlRoot;
            AdminDialog.PrimaryButtonText = CoreTools.Translate("I understand");
            AdminDialog.DefaultButton = ContentDialogButton.Primary;
            AdminDialog.Title = CoreTools.Translate("Administrator privileges");
            AdminDialog.SecondaryButtonClick += IgnoredUpdatesPage.ManageIgnoredUpdates_SecondaryButtonClick;
            AdminDialog.Content = CoreTools.Translate("WingetUI has been ran as administrator, which is not recommended. When running WingetUI as administrator, EVERY operation launched from WingetUI will have administrator privileges. You can still use the program, but we highly recommend not running WingetUI with administrator privileges.");

            await MainApp.Instance.MainWindow.ShowDialogAsync(AdminDialog);
        }

        public async void WarnAboutNewName()
        {
            ContentDialog AdminDialog = new();
            AdminDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;

            while (XamlRoot == null)
            {
                await Task.Delay(100);
            }

            string NEW_NAME = "UnigetUI";

            AdminDialog.XamlRoot = XamlRoot;
            AdminDialog.PrimaryButtonText = CoreTools.Translate("I understand");
            AdminDialog.DefaultButton = ContentDialogButton.Primary;
            AdminDialog.SecondaryButtonClick += IgnoredUpdatesPage.ManageIgnoredUpdates_SecondaryButtonClick;
            StackPanel p = new() { Spacing = 16 };
            AdminDialog.Content = p;

            p.Children.Add(new Image() { Source = new BitmapImage() { UriSource = new Uri("ms-appx:///Assets/Images/icon.png") }, Height = 96 });

            Paragraph par = new();
            par.Inlines.Add(new Run() { Text = CoreTools.Translate("WingetUI will become {newname} soon!").Replace("{newname}", NEW_NAME), FontSize = 24, FontWeight = new Windows.UI.Text.FontWeight(700), FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe UI Variable Display Bold") });
            par.Inlines.Add(new LineBreak());
            par.Inlines.Add(new LineBreak());
            par.Inlines.Add(new Run() { Text = CoreTools.Translate("WingetUI will soon be named {newname}. This will not represent any change in the application. I (the developer) will continue the development of this project as I am doing right now, but under a different name.").Replace("{newname}", NEW_NAME) });
            par.Inlines.Add(new LineBreak());
            par.Inlines.Add(new LineBreak());
            par.Inlines.Add(new Run() { Text = CoreTools.Translate("WingetUI is being renamed in order to emphasize the difference between WingetUI (the interface you are using right now) and Winget (a package manager developed by Microsoft with which I am not related)"), FontSize = 12, FontStyle = Windows.UI.Text.FontStyle.Italic });
            par.Inlines.Add(new LineBreak());
            par.Inlines.Add(new Run() { Text = CoreTools.Translate("While Winget can be used within WingetUI, WingetUI can be used with other package managers, which can be confusing. In the past, WingetUI was designed to work only with Winget, but this is not true anymore, and therefore WingetUI does not represent what this project aims to become."), FontSize = 12, FontStyle = Windows.UI.Text.FontStyle.Italic });

            RichTextBlock text = new();
            text.Blocks.Add(par);
            p.Children.Add(text);

            await MainApp.Instance.MainWindow.ShowDialogAsync(AdminDialog);
        }


        public async Task<bool> ShowInstallationSettingsForPackageAndContinue(Package package, OperationType Operation)
        {
            InstallOptionsPage OptionsPage = new(package, Operation);

            ContentDialog? OptionsDialog = new();
            OptionsDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            OptionsDialog.XamlRoot = XamlRoot;
            OptionsDialog.Resources["ContentDialogMaxWidth"] = 1200;
            OptionsDialog.Resources["ContentDialogMaxHeight"] = 1000;
            if (Operation == OperationType.Install)
                OptionsDialog.SecondaryButtonText = CoreTools.Translate("Install");
            else if (Operation == OperationType.Update)
                OptionsDialog.SecondaryButtonText = CoreTools.Translate("Update");
            else if (Operation == OperationType.Uninstall)
                OptionsDialog.SecondaryButtonText = CoreTools.Translate("Uninstall");
            else
                OptionsDialog.SecondaryButtonText = "";
            OptionsDialog.PrimaryButtonText = CoreTools.Translate("Save and close");
            OptionsDialog.DefaultButton = ContentDialogButton.Secondary;
            OptionsDialog.Title = CoreTools.Translate("{0} installation options").Replace("{0}", package.Name);
            OptionsDialog.Content = OptionsPage;

            ContentDialogResult result = await MainApp.Instance.MainWindow.ShowDialogAsync(OptionsDialog);
            OptionsPage.SaveToDisk();

            OptionsDialog.Content = null;
            OptionsDialog = null;

            return result == ContentDialogResult.Secondary;

        }

        public async Task<InstallationOptions> UpdateInstallationSettings(Package package, InstallationOptions options)
        {
            InstallOptionsPage OptionsPage = new(package, options);

            ContentDialog? OptionsDialog = new();
            OptionsDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            OptionsDialog.XamlRoot = XamlRoot;
            OptionsDialog.Resources["ContentDialogMaxWidth"] = 1200;
            OptionsDialog.Resources["ContentDialogMaxHeight"] = 1000;
            OptionsDialog.SecondaryButtonText = "";
            OptionsDialog.PrimaryButtonText = CoreTools.Translate("Save and close");
            OptionsDialog.DefaultButton = ContentDialogButton.Secondary;
            OptionsDialog.Title = CoreTools.Translate("{0} installation options").Replace("{0}", package.Name);
            OptionsDialog.Content = OptionsPage;
            await MainApp.Instance.MainWindow.ShowDialogAsync(OptionsDialog);

            OptionsDialog.Content = null;
            OptionsDialog = null;

            return await OptionsPage.GetUpdatedOptions();

        }

        private void NavigateToPage(Page TargetPage)
        {
            foreach (Page page in PageButtonReference.Keys)
                if (page.Visibility == Visibility.Visible)
                    OldPage = page;
            if (!PageButtonReference.ContainsKey(TargetPage))
            {
                PageButtonReference.Add(TargetPage, MoreNavButton);
                Grid.SetColumn(TargetPage, 0);
                Grid.SetRow(TargetPage, 0);
                MainContentPresenterGrid.Children.Add(TargetPage);
            }
            foreach (NavButton button in MainApp.Instance.MainWindow.NavButtonList)
            {

                button.ToggleButton.IsChecked = (button == PageButtonReference[TargetPage]);
            }

            foreach (Page page in PageButtonReference.Keys)
                page.Visibility = (page == TargetPage) ? Visibility.Visible : Visibility.Collapsed;

            CurrentPage = TargetPage;

            if (CurrentPage == DiscoverPage)
                DiscoverPage.PackageList.Focus(FocusState.Programmatic);
            else if (CurrentPage == UpdatesPage)
                UpdatesPage.PackageList.Focus(FocusState.Programmatic);
            else if (CurrentPage == InstalledPage)
                InstalledPage.PackageList.Focus(FocusState.Programmatic);
            else if (CurrentPage == BundlesPage)
                BundlesPage.PackageList.Focus(FocusState.Programmatic);
        }

        private async void ReleaseNotesMenu_Click(object sender, RoutedEventArgs e)
        {

            ContentDialog? NotesDialog = new();
            NotesDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            NotesDialog.XamlRoot = XamlRoot;
            NotesDialog.Resources["ContentDialogMaxWidth"] = 12000;
            NotesDialog.Resources["ContentDialogMaxHeight"] = 10000;
            NotesDialog.CloseButtonText = CoreTools.Translate("Close");
            NotesDialog.Title = CoreTools.Translate("Release notes");
            ReleaseNotes? notes = new();
            NotesDialog.Content = notes;
            NotesDialog.SizeChanged += (s, e) =>
            {
                notes.MinWidth = ActualWidth - 300;
                notes.MinHeight = ActualHeight - 200;
            };

            await MainApp.Instance.MainWindow.ShowDialogAsync(NotesDialog);

            notes.Dispose();
            notes = null;
            NotesDialog = null;
        }

        public async Task ShowPackageDetails(Package package, OperationType ActionOperation)
        {
            PackageDetailsPage? DetailsPage = new(package, ActionOperation);

            ContentDialog? DetailsDialog = new();
            DetailsDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            DetailsDialog.XamlRoot = XamlRoot;
            DetailsDialog.Resources["ContentDialogMaxWidth"] = 8000;
            DetailsDialog.Resources["ContentDialogMaxHeight"] = 4000;
            DetailsDialog.Content = DetailsPage;
            DetailsDialog.SizeChanged += (s, e) =>
            {
                int hOffset = (ActualWidth < 1300) ? 100 : 300;
                DetailsPage.MinWidth = ActualWidth - hOffset;
                DetailsPage.MinHeight = ActualHeight - 100;
                DetailsPage.MaxWidth = ActualWidth - hOffset;
                DetailsPage.MaxHeight = ActualHeight - 100;
            };

            DetailsPage.Close += (s, e) => { DetailsDialog.Hide(); };

            await MainApp.Instance.MainWindow.ShowDialogAsync(DetailsDialog);

            DetailsDialog.Content = null;
            DetailsDialog = null;

        }

        private void OperationHistoryMenu_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new Logger_LogPage(Logger_LogType.OperationHistory));
        }

        private void ManagerLogsMenu_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new Logger_LogPage(Logger_LogType.ManagerLogs));
        }

        public void UniGetUILogs_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new Logger_LogPage(Logger_LogType.UniGetUILog));
        }


        private void HelpMenu_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
        public void ShowHelp()
        {
            if (HelpPage == null)
                HelpPage = new HelpDialog();
            NavigateToPage(HelpPage);
        }

        private void QuitUniGetUI_Click(object sender, RoutedEventArgs e)
        {
            MainApp.Instance.DisposeAndQuit();
        }
    }
}
