using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using ModernWindow.Interface.Widgets;
using Microsoft.UI.Composition;
using System.Numerics;
using System.Collections.ObjectModel;
using ModernWindow.PackageEngine;
using System.Threading.Tasks;
using ModernWindow.Structures;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface
{
    public sealed partial class NavigationPage : UserControl
    {
        public static AppTools bindings = AppTools.Instance;
        public SettingsInterface SettingsPage;
        public DiscoverPackagesPage DiscoverPage;
        public SoftwareUpdatesPage UpdatesPage;
        public InstalledPackagesPage InstalledPage;
        public Page OldPage;
        public Page CurrentPage;
        public InfoBadge UpdatesBadge;
        public StackPanel OperationStackPanel;
        private Dictionary<Page, NavButton> PageButtonReference = new();

        private AboutWingetUI AboutPage;
        private IgnoredUpdatesManager IgnoredUpdatesPage;

        public NavigationPage()
        {
            this.InitializeComponent();
            UpdatesBadge = __updates_count_badge;
            OperationStackPanel = __operations_list_stackpanel;
            SettingsPage = new SettingsInterface();
            DiscoverPage = new DiscoverPackagesPage();
            UpdatesPage = new SoftwareUpdatesPage();
            InstalledPage = new InstalledPackagesPage();
            AboutPage = new AboutWingetUI();
            IgnoredUpdatesPage = new IgnoredUpdatesManager();

            int i = 0;
            foreach (Page page in new Page[] { DiscoverPage, UpdatesPage, InstalledPage, SettingsPage })
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

            DiscoverNavButton.ForceClick();
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

        private void MoreNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        { }

        private void SettingsNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            NavigateToPage(SettingsPage);
        }

        private async void AboutNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            ContentDialog AboutDialog = new ContentDialog();
            AboutDialog.XamlRoot = this.XamlRoot;
            AboutDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            AboutDialog.Resources["ContentDialogMaxWidth"] = 1200;
            AboutDialog.Resources["ContentDialogMaxHeight"] = 1000;
            AboutDialog.Content = AboutPage;
            AboutDialog.PrimaryButtonText = bindings.Translate("Close");
            foreach (NavButton button in bindings.App.mainWindow.NavButtonList)
                button.ToggleButton.IsChecked = false;

            await AboutDialog.ShowAsync();

            AboutDialog.Content = null;
            foreach (NavButton button in bindings.App.mainWindow.NavButtonList)
                button.ToggleButton.IsChecked = (button == PageButtonReference[CurrentPage]);
            AboutDialog = null;
        }

        public async Task ManageIgnoredUpdatesDialog()
        {
            ContentDialog UpdatesDialog = new ContentDialog();
            UpdatesDialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            UpdatesDialog.XamlRoot = this.XamlRoot;
            UpdatesDialog.Resources["ContentDialogMaxWidth"] = 1200;
            UpdatesDialog.Resources["ContentDialogMaxHeight"] = 1000;
            UpdatesDialog.PrimaryButtonText = bindings.Translate("Close");
            UpdatesDialog.SecondaryButtonText = bindings.Translate("Reset");
            UpdatesDialog.DefaultButton = ContentDialogButton.Primary;
            UpdatesDialog.Title = bindings.Translate("Manage ignored updates");
            UpdatesDialog.SecondaryButtonClick += IgnoredUpdatesPage.ManageIgnoredUpdates_SecondaryButtonClick;
            UpdatesDialog.Content = IgnoredUpdatesPage;

            _ = IgnoredUpdatesPage.UpdateData();
            await UpdatesDialog.ShowAsync();

            UpdatesDialog.Content = null;
            UpdatesDialog = null;


        }

        private void NavigateToPage(Page TargetPage)
        {
            foreach (Page page in PageButtonReference.Keys)
                if (page.Visibility == Visibility.Visible)
                    OldPage = page;

            foreach (NavButton button in bindings.App.mainWindow.NavButtonList)
            {
                button.ToggleButton.IsChecked = (button == PageButtonReference[TargetPage]);
            }

            foreach (Page page in PageButtonReference.Keys)
                page.Visibility = (page == TargetPage) ? Visibility.Visible : Visibility.Collapsed;

            CurrentPage = TargetPage;
        }
    }
}
