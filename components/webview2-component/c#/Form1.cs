using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsWebView
{
    public partial class Form1 : Form
    {
        bool loaded = false;
        public Form1()
        {
            InitializeComponent();
            loadWV();
            Hide();
            Opacity = 0.0f;
        }

        public int getHWND() {
            return this.Handle.ToInt32();
        }

        public void uncoverWindow()
        {
            Opacity = 1.0f;
            Show();
        }

        public async void navigateTo(string url)
        {
            while(!loaded)
            {
                await Task.Delay(50);
            }
            webView.Source = new Uri(url);
        }

        public async void navigateToString(string content)
        {
            while (!loaded)
            {
                await Task.Delay(50);
            }
            webView.NavigateToString(content);
        }

        public async void stop()
        {
            while (!loaded)
            {
                await Task.Delay(50);
            }
            webView.Stop();
        }

        public async void reload()
        {
            while (!loaded)
            {
                await Task.Delay(50);
            }
            webView.Reload();
        }

        public string getUrl()
        {
            if (loaded)
                return webView.Location.ToString();
            else
                return "";
        }

        private async void loadWV(string pathAppend = "")
        {
            try
            {
                CoreWebView2Environment cwv2Environment = await CoreWebView2Environment.CreateAsync(null, Path.Combine(Path.GetTempPath(), pathAppend), new CoreWebView2EnvironmentOptions());
                await webView.EnsureCoreWebView2Async(cwv2Environment);
                loaded = true;
                try
                {
                    webView.Source = new Uri("about:blank");
                }
                catch
                {
                    webView.NavigateToString("<html><h1>Invalid arguments</h1></html>");
                }
                loaded = true;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                this.loadWV("new");
            }
            
        }
    }
}
