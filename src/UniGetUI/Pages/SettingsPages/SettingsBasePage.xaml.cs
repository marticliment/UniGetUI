using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Pages.SettingsPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsBasePage : Page
    {
        public SettingsBasePage()
        {
            this.InitializeComponent();
            BackButton.Click += (_, _) => MainNavigationFrame.NavigateToType(typeof(SettingsHomepage), null, new());
            MainNavigationFrame.Navigated += MainNavigationFrame_Navigated;
            MainNavigationFrame.Navigating += MainNavigationFrame_Navigating;
            MainNavigationFrame.NavigateToType(typeof(SettingsHomepage), null, new());

            RestartRequired.Message = CoreTools.Translate("Restart WingetUI to fully apply changes");
            var RestartButton = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Content = CoreTools.Translate("Restart WingetUI"),
            };
            RestartButton.Click += (_, _) => MainApp.Instance.KillAndRestart();
            RestartRequired.ActionButton = RestartButton;

        }

        private void MainNavigationFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (MainNavigationFrame.Content is null) return;
            var page = MainNavigationFrame.Content as ISettingsPage;
            if (page is null) throw new InvalidCastException("Settings page does not inherit from ISettingsPage");

            page.NavigationRequested -= Page_NavigationRequested;
            page.RestartRequired -= Page_RestartRequired;
        }

        private void MainNavigationFrame_Navigated(object sender, NavigationEventArgs e)
        {
            var page = e.Content as ISettingsPage;
            if (page is null) throw new InvalidCastException("Settings page does not inherit from ISettingsPage");

            BackButton.Visibility = page.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
            SettingsTitle.Text = page.ShortTitle;
            page.NavigationRequested += Page_NavigationRequested;
            page.RestartRequired += Page_RestartRequired;
        }

        private void Page_RestartRequired(object? sender, EventArgs e)
        {
            RestartRequired.IsOpen = true;
        }

        private void Page_NavigationRequested(object? sender, Type e)
        {
            MainNavigationFrame.NavigateToType(e, null, new());
        }
    }
}
