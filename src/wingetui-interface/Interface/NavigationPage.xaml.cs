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
        public NavigationPage()
        {
            this.InitializeComponent();
            UpdatesBadge = __updates_count_badge;
            SettingsPage = new SettingsInterface();
            DiscoverPage = new DiscoverPackagesPage();
            UpdatesPage = new SoftwareUpdatesPage();
            InstalledPage = new InstalledPackagesPage();
            DiscoverNavButton.ForceClick();
        }

        private void DiscoverNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            MainContentPresenter.Navigate(typeof(DiscoverPackagesPage), new DrillInNavigationTransitionInfo());
        }

        private void InstalledNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            MainContentPresenter.Navigate(typeof(InstalledPackagesPage), new DrillInNavigationTransitionInfo());
        }

        private void UpdatesNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            MainContentPresenter.Navigate(typeof(SoftwareUpdatesPage), new DrillInNavigationTransitionInfo());
        }

        private void MoreNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            MainContentPresenter.Navigate(typeof(Page), new DrillInNavigationTransitionInfo());
        }

        private void SettingsNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            MainContentPresenter.Navigate(typeof(SettingsInterface), new DrillInNavigationTransitionInfo());
        }

        private void AboutNavButton_Click(object sender, NavButton.NavButtonEventArgs e)
        {
            MainContentPresenter.Navigate(typeof(Page), new DrillInNavigationTransitionInfo());
        }
    }
}
