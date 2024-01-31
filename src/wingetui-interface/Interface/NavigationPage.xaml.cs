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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface
{
    public sealed partial class NavigationPage : UserControl
    {
        public SettingsInterface SettingsPage;
        public DiscoverPackagesPage DiscoverPage;
        public SoftwareUpdatesPage UpdatesPage;
        public InstalledPackagesPage InstalledPage;
        public Type OldPage;
        public InfoBadge UpdatesBadge;
        private Dictionary<Page, int> PageColumnReference = new();
        public ObservableCollection<InstallPackageOperation> OperationList = new();
        public NavigationPage()
        {
            this.InitializeComponent();
            UpdatesBadge = __updates_count_badge;
            SettingsPage = new SettingsInterface();
            DiscoverPage = new DiscoverPackagesPage();
            UpdatesPage = new SoftwareUpdatesPage();
            InstalledPage = new InstalledPackagesPage();

            int i = 0;
            foreach (Page page in new Page[] { DiscoverPage, UpdatesPage, InstalledPage, SettingsPage, })
            {
                Grid.SetColumn(page, 0);
                Grid.SetRow(page, 0);
                MainContentPresenterGrid.Children.Add(page);
                PageColumnReference.Add(page, i);
                i++;
            }

            DiscoverNavButton.ForceClick();
            _ = TestOperationWidget();
        }

        public async Task TestOperationWidget()
        {
            OperationList.Add(new InstallPackageOperation());
            await Task.Delay(5000);
            OperationList.Add(new InstallPackageOperation());
            await Task.Delay(5000);
            OperationList.Add(new InstallPackageOperation());
            await Task.Delay(10000);
            OperationList.Add(new InstallPackageOperation());
            await Task.Delay(2000);
            OperationList.RemoveAt(0);
            await Task.Delay(2000);
            OperationList.RemoveAt(0);
            await Task.Delay(2000);
            OperationList.RemoveAt(0);
            await Task.Delay(2000);
            OperationList.RemoveAt(0);
            await Task.Delay(2000);
            OperationList.Add(new InstallPackageOperation());
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
        {
            // MainContentPresenter.Navigate(typeof(Page), new DrillInNavigationTransitionInfo());
        }

        private void SettingsNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            NavigateToPage(SettingsPage);
        }

        private void AboutNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            // MainContentPresenter.Navigate(typeof(Page), new DrillInNavigationTransitionInfo());
        }

        private void NavigateToPage(Page targetPage)
        {
            var visiblePage = targetPage;
            foreach (Page page in PageColumnReference.Keys)
                if (page.Visibility == Visibility.Visible)
                    visiblePage = page;

            /*if (PageColumnReference[visiblePage] < PageColumnReference[targetPage])
                (targetPage as dynamic).ShowAnimationInitialPos = new Vector3(0, 100, 0);
            else
                (targetPage as dynamic).ShowAnimationInitialPos = new Vector3(0, -100, 0);*/


                foreach (Page page in PageColumnReference.Keys)
                page.Visibility = (page == targetPage) ? Visibility.Visible : Visibility.Collapsed;
                    //    MainContentPresenterGrid.ColumnDefinitions[PageColumnReference[page]].Width =
            //        (page != targetPage) ? new GridLength(0, GridUnitType.Pixel) : new GridLength(1, GridUnitType.Star);

        }
    }
}
