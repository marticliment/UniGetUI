using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

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

            Brush SUCCESS_BG = (Brush)Application.Current.Resources["SystemFillColorSuccessBackgroundBrush"];
            Brush WARN_BG = (Brush)Application.Current.Resources["SystemFillColorCautionBackgroundBrush"];
            Brush ERROR_BG = (Brush)Application.Current.Resources["SystemFillColorCriticalBackgroundBrush"];
            Brush BORDER_FG = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
            Brush TEXT_FG = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];

            string no = CoreTools.Translate("No");
            string yes = CoreTools.Translate("Yes");
            string part = CoreTools.Translate("Partially");

            foreach (var manager in PEInterface.Managers)
            {
                ManagersPanel.Children.Add(new TextBlock()
                {
                    Text = manager.DisplayName,
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(3)
                });

                var level = manager.Capabilities.SupportsProxy;
                ProxyPanel.Children.Add(new Border()
                {
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(2),
                    Background = level is ProxySupport.No? ERROR_BG: (level is ProxySupport.Partially? WARN_BG: SUCCESS_BG),
                    BorderBrush = BORDER_FG,
                    BorderThickness = new Thickness(1),
                    Child = new TextBlock()
                    {
                        Text = (level is ProxySupport.No ? no : (level is ProxySupport.Partially ? part: yes)),
                        TextAlignment = TextAlignment.Center,
                        Foreground = TEXT_FG
                    }
                });

                AuthPanel.Children.Add(new Border()
                {
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(2),
                    Background = manager.Capabilities.SupportsProxyAuth ? SUCCESS_BG : ERROR_BG,
                    BorderBrush = BORDER_FG,
                    BorderThickness = new Thickness(1),
                    Child = new TextBlock()
                    {
                        Text = manager.Capabilities.SupportsProxyAuth ? yes : no,
                        TextAlignment = TextAlignment.Center,
                        Foreground = TEXT_FG
                    }
                });

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
