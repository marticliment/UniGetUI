using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsWebView
{
    public partial class Form1 : Form
    {
        private bool _loaded = false;
        private readonly bool _contextMenuEnabled = true;
        private readonly SemaphoreSlim _loadSemaphore = new SemaphoreSlim(1, 1);

        public Form1(bool contextMenuEnabled = true)
        {
            _contextMenuEnabled = contextMenuEnabled;
            InitializeComponent();
            Opacity = 0.0f;
            LoadWebView();
        }

        public int HWND => Handle.ToInt32(); // Simplified property

        public void UncoverWindow()
        {
            Opacity = 1.0f;
            Show();
        }

        private async Task WaitForLoadAsync()
        {
            await _loadSemaphore.WaitAsync();
            try
            {
                if (!_loaded)
                {
                    await Task.Delay(50);
                }
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }

        public async void NavigateTo(string url)
        {
            await WaitForLoadAsync();
            webView.Source = new Uri(url);
        }

        public async void NavigateToString(string content)
        {
            await WaitForLoadAsync();
            webView.NavigateToString(content);
        }

        public async void Stop()
        {
            await WaitForLoadAsync();
            webView.Stop();
        }

        public async void Reload()
        {
            await WaitForLoadAsync();
            webView.Reload();
        }

        public string Url => _loaded ? webView.Source.ToString() : string.Empty; // Simplified property

        private async void LoadWebView(string pathAppend = "")
        {
            await _loadSemaphore.WaitAsync();
            try
            {
                if (!_loaded)
                {
                    CoreWebView2Environment cwv2Environment = await CoreWebView2Environment.CreateAsync(null, Path.Combine(Path.GetTempPath(), pathAppend), new CoreWebView2EnvironmentOptions());
                    await webView.EnsureCoreWebView2Async(cwv2Environment);
                    webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = _contextMenuEnabled;
                    webView.CoreWebView2.NavigationCompleted += (sender, args) => _loaded = true;
                    webView.Source = new Uri("about:blank");
                }
            }
            catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException || ex is IOException)
            {
                LoadWebView("new");
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }
    }
}
