using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Pages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HelpPage : Page, IDisposable, IEnterLeaveListener
    {
        private bool Initialized;
        private WebView2? webView;
        private Uri? lastUri;

        public HelpPage()
        {
            InitializeComponent();
            _ = InitializeWebView();
        }

        private async Task InitializeWebView()
        {
            webView = new();
            WebViewBorder.Child = webView;
            webView.NavigationStarting += (_, e) =>
            {
                ProgressBar.Visibility = Visibility.Visible;
                lastUri = new Uri(e.Uri);
                if (e.Uri.ToString().Contains("marticliment.com") && !e.Uri.ToString().Contains("isWingetUIIframe"))
                {
                    e.Cancel = true;
                    if (e.Uri.ToString().Contains('?'))
                    {
                        webView.Source = new Uri(e.Uri.ToString() + "&isWingetUIIframe");
                    }
                    else
                    {
                        webView.Source = new Uri(e.Uri.ToString() + "?isWingetUIIframe");
                    }
                }
            };
            webView.NavigationCompleted += (_, _) =>
            {
                ProgressBar.Visibility = Visibility.Collapsed;
            };

            await webView.EnsureCoreWebView2Async();
            NavigateTo("", skipWait: true);
            Initialized = true;
        }

        public void NavigateTo(string piece, bool skipWait = false)
            => _ = _navigateTo(piece, skipWait);

        private async Task _navigateTo(string piece, bool skipWait)
        {
            while (!Initialized && !skipWait) await Task.Delay(50);
            ArgumentNullException.ThrowIfNull(webView);
            webView.Source = new Uri("https://marticliment.com/unigetui/help/" + piece);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Initialized && webView is not null && webView.CanGoBack)
            {
                webView.GoBack();
            }
        }

        private void RightButton_Click(object sender, RoutedEventArgs e)
        {

            if (Initialized && webView is not null && webView.CanGoForward)
            {
                webView.GoForward();
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Initialized || webView is null)
                return;

            webView.Source = new Uri("https://marticliment.com/unigetui/help");
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Initialized || webView is null)
                return;

            webView.Reload();
        }

        private void BrowserButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Initialized || webView is null)
                return;

            string uri = webView.Source.ToString().Replace("?isWingetUIIframe", "").Replace("&isWingetUIIframe", "");
            CoreTools.Launch(uri);
        }

        public void Dispose()
        {
            webView?.Close();
            WebViewBorder.Child = new UserControl();
            webView = null;
            Initialized = false;
        }

        public void OnEnter()
        {
            if (webView is null) _ = InitializeWebView();
        }

        public void OnLeave()
        {
            webView?.Close();
            WebViewBorder.Child = new UserControl();
            webView = null;
            Initialized = false;
        }
    }
}
