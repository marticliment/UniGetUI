using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Pages.DialogPages;
using UniGetUI.Pages.SettingsPages.GeneralPages;

namespace UniGetUI.Services
{
    public partial class PointButton: Button
    {
        public PointButton()
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        }
    }

    public partial class UserAvatar: UserControl
    {
        public UserAvatar()
        {
            VerticalContentAlignment = VerticalAlignment.Center;
            HorizontalContentAlignment = HorizontalAlignment.Center;
            _ = RefreshStatus();
            GitHubAuthService.AuthStatusChanged += GitHubAuthService_AuthStatusChanged;
        }

        private void GitHubAuthService_AuthStatusChanged(object? sender, EventArgs e)
        {
            _ = RefreshStatus();
        }

        public async Task RefreshStatus()
        {
            SetLoading();
            var client = new GitHubAuthService();
            // await Task.Delay(1000);
            if (client.IsAuthenticated())
            {
                Content = await GenerateLogoutControl();
            }
            else
            {
                Content = GenerateLoginControl();
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            SetLoading();
            try
            {
                var client = new GitHubAuthService();
                if (client.IsAuthenticated())
                {
                    Logger.Warn("Login invoked when the client was already logged in!");
                    return;
                }

                await client.SignInAsync();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowDismissableBalloon(
                    CoreTools.Translate("Error"),
                    CoreTools.Translate("Log in failed: ") + ex.Message
                );
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            SetLoading();
            try
            {
                var client = new GitHubAuthService();
                if (client.IsAuthenticated())
                {
                    client.SignOut();
                }
            }
            catch (Exception ex)
            {
                DialogHelper.ShowDismissableBalloon(
                    CoreTools.Translate("Error"),
                    CoreTools.Translate("Log out failed: ") + ex.Message
                );
            }
        }

        private void SetLoading()
        {
            this.Content = new ProgressRing() { IsIndeterminate = true, Width = 24, Height = 24 };
        }

        private PointButton GenerateLoginControl()
        {
            var personPicture = new PersonPicture
            {
                Width = 36,
                Height = 36,
            };

            var translatedTextBlock = new TextBlock
            {
                Margin = new Thickness(4),
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = CoreTools.Translate("Log in with GitHub to enable cloud package backup.")
            };

            var hyperlinkButton = new HyperlinkButton
            {
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Content = CoreTools.Translate("More details"),
                NavigateUri = new Uri("https://www.marticliment.com/unigetui/help/cloud-backup-overview/"),
                FontSize = 12
            };

            var loginButton = new PointButton
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Content = CoreTools.Translate("Log in")
            };
            loginButton.Click += LoginButton_Click;

            var stackPanel = new StackPanel
            {
                MaxWidth = 200,
                Margin = new Thickness(-8),
                Orientation = Orientation.Vertical,
                Spacing = 8
            };
            stackPanel.Children.Add(translatedTextBlock);
            stackPanel.Children.Add(hyperlinkButton);
            stackPanel.Children.Add(loginButton);

            var flyout = new Flyout
            {
                LightDismissOverlayMode = LightDismissOverlayMode.Off,
                Placement = FlyoutPlacementMode.Bottom,
                Content = stackPanel
            };

            return new PointButton
            {
                Margin = new Thickness(0),
                Padding = new Thickness(4),
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(100),
                Content = personPicture,
                Flyout = flyout
            };
        }

        private async Task<PointButton> GenerateLogoutControl()
        {
            var authClient = new GitHubAuthService();
            var GHClient = authClient.CreateGitHubClient();
            if(GHClient is null)
            {
                Logger.Error("Client did not report valid authentication");
                return GenerateLoginControl();
            }

            var user = await GHClient.User.Current();

            var personPicture = new PersonPicture
            {
                Width = 36,
                Height = 36,
                ProfilePicture = new BitmapImage(new Uri(user.AvatarUrl))
            };

            var text1 = new TextBlock
            {
                Margin = new Thickness(4),
                TextWrapping = TextWrapping.WrapWholeWords,
                Text = CoreTools.Translate("You are logged in as {0} (@{1})", user.Name, user.Login)
            };

            var text2 = new TextBlock
            {
                Margin = new Thickness(4),
                TextWrapping = TextWrapping.WrapWholeWords,
                FontSize = 12,
                FontWeight = new(500),
                Text = CoreTools.Translate("If you have cloud backup enabled, it will be saved as a GitHub Gist on this account")
            };

            var hyperlinkButton = new HyperlinkButton
            {
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Content = "Backup settings",
                FontSize = 12
            };
            hyperlinkButton.Click += (_, _) => MainApp.Instance.MainWindow.NavigationPage.OpenSettingsPage(typeof(Backup));

            var loginButton = new PointButton
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Content = "Log out",
                Background = new SolidColorBrush(ActualTheme is ElementTheme.Dark? Colors.DarkRed: Colors.PaleVioletRed),
                BorderThickness = new(0)
            };
            loginButton.Click += LogoutButton_Click;

            var stackPanel = new StackPanel
            {
                MaxWidth = 200,
                Margin = new Thickness(-8),
                Orientation = Orientation.Vertical,
                Spacing = 8
            };
            stackPanel.Children.Add(text1);
            stackPanel.Children.Add(text2);
            stackPanel.Children.Add(hyperlinkButton);
            stackPanel.Children.Add(loginButton);

            var flyout = new Flyout
            {
                LightDismissOverlayMode = LightDismissOverlayMode.Off,
                Placement = FlyoutPlacementMode.Bottom,
                Content = stackPanel
            };

            return new PointButton
            {
                Margin = new Thickness(0),
                Padding = new Thickness(4),
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(100),
                Content = personPicture,
                Flyout = flyout
            };
        }
    }
}
