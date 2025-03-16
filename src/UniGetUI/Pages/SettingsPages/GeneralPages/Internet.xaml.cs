using System.Security;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Pages.SettingsPages.GeneralPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Internet : Page, ISettingsPage
    {
        public Internet()
        {
            this.InitializeComponent();

            UsernameBox.PlaceholderText = CoreTools.Translate("Username");
            PasswordBox.PlaceholderText = CoreTools.Translate("Password");

            var creds = Settings.GetProxyCredentials();
            if (creds is not null)
            {
                UsernameBox.Text = creds.UserName;
                PasswordBox.Password = creds.Password;
            }
        }

        public bool CanGoBack => true;
        public string ShortTitle => CoreTools.Translate("Internet connection settings");

        public event EventHandler? RestartRequired;
        public event EventHandler<Type>? NavigationRequested;

        public void ShowRestartBanner(object sender, EventArgs e)
            => RestartRequired?.Invoke(this, e);

        private void UsernameBox_TextChanged(object sender, RoutedEventArgs e)
        {
            Settings.SetProxyCredentials(UsernameBox.Text, PasswordBox.Password);
        }

        private void UsernameBox_TextChanged(object sender, TextChangedEventArgs e)
            => UsernameBox_TextChanged(sender, new RoutedEventArgs());
    }
}
