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
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Documents;
using ModernWindow.Structures;
using System.Diagnostics;
using Windows.Web.Http;
using Windows.UI.Text;
using Microsoft.UI.Text;
using Microsoft.Windows.ApplicationModel.DynamicDependency.BootstrapCS;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.System.UserProfile;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface.Widgets
{
    public sealed partial class Announcer : UserControl
    {
        MainAppBindings binder = MainAppBindings.Instance;
        public Uri Url
        {
            get => (Uri)GetValue(UrlProperty);
            set => SetValue(UrlProperty, value);
        }

        DependencyProperty UrlProperty;


        private static HttpClient NetClient = new HttpClient();


        public Announcer()
        {
            UrlProperty = DependencyProperty.Register(
            nameof(UrlProperty),
            typeof(Uri),
            typeof(CheckboxCard),
            new PropertyMetadata(default(Uri), new PropertyChangedCallback((d, e) => { LoadAnnouncements(); })));

            this.InitializeComponent();
            this.DefaultStyleKey = typeof(Announcer);
            this.BringIntoViewRequested += (s, e) => { LoadAnnouncements(); };
            SetText(binder.Translate("Fetching latest announcements, please wait..."));
            _textblock.TextWrapping = TextWrapping.Wrap;
        }

        public async void LoadAnnouncements(bool retry = false)
        {
            try
            {
                Uri announcement_url = Url;
                if (retry)
                    announcement_url = new Uri(Url.ToString().Replace("https://", "http://"));

                HttpResponseMessage response = await NetClient.GetAsync(announcement_url);
                if (response.IsSuccessStatusCode)
                {
                    string text = response.Content.ToString().Split("////")[0];
                    Uri imageUrl = new Uri(response.Content.ToString().Split("////")[1]);
                    SetText(text);
                    SetImage(imageUrl);
                }
                else
                {
                    SetText(binder.Translate("Could not load announcements - HTTP status code is $CODE").Replace("$CODE", response.StatusCode.ToString()));
                    SetImage(new Uri("ms-appx:///wingetui/resources/warn.png"));
                    if (!retry)
                        LoadAnnouncements(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not load announcements: " + ex.ToString());
                SetText(binder.Translate("Could not load announcements - ") + ex.ToString());
                SetImage(new Uri("ms-appx:///wingetui/resources/warn.png"));
            }
        }

        public void SetText_Safe(string text)
        {
            ((MainApp)Application.Current).mainWindow.DispatcherQueue.TryEnqueue(() => {
                SetText(text);
            });
        }

        public void SetText(string text)
        {
            Paragraph paragraph = new Paragraph();
            foreach (string line in text.Split("\n"))
            {
                if(line.Contains("<h1>"))
                    paragraph.Inlines.Add(new Run() { Text = line.Replace("<h1>", "").Replace("</h1>", ""), FontSize = 24, FontWeight = new FontWeight(650) });
                else
                    paragraph.Inlines.Add(new Run() { Text = line.Replace("<p>", "").Replace("</p>", "").Replace("<br>", "") });

                paragraph.Inlines.Add(new LineBreak());

            }
            _textblock.Blocks.Clear();
            _textblock.Blocks.Add(paragraph);
        }

        public void SetImage(Uri url)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.UriSource = url;
            _image.Source = bitmapImage;

        }
    }
}
