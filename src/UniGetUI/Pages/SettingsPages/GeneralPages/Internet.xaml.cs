using System.Security;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface;

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

        private async void UsernameBox_TextChanged(object sender, RoutedEventArgs e)
        {
            SavingUserName.Opacity = 1;
            string oldusername = UsernameBox.Text;
            string oldpassword = PasswordBox.Password;
            await Task.Delay(500);
            if (oldusername != UsernameBox.Text) return;
            if (oldpassword != PasswordBox.Password) return;
            Settings.SetProxyCredentials(UsernameBox.Text, PasswordBox.Password);
            MainWindow.ApplyProxyVariableToProcess();
            SavingUserName.Opacity = 0;
        }

        private void UsernameBox_TextChanged(object sender, TextChangedEventArgs e)
            => UsernameBox_TextChanged(sender, new RoutedEventArgs());

        private void EnableProxy_OnStateChanged(object? sender, EventArgs e)
        {
            MainWindow.ApplyProxyVariableToProcess();
        }

        private void TextboxCard_OnValueChanged(object? sender, EventArgs e)
        {
            MainWindow.ApplyProxyVariableToProcess();
        }
    }
}
