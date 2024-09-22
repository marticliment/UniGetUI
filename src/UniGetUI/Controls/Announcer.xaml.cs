using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using Windows.UI.Text;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public sealed partial class Announcer : UserControl
    {
        public Uri Url
        {
            get => (Uri)GetValue(UrlProperty);
            set => SetValue(UrlProperty, value);
        }

        private readonly DependencyProperty UrlProperty;

        private static readonly HttpClient NetClient = new(CoreData.GenericHttpClientParameters);
        public Announcer()
        {
            NetClient.DefaultRequestHeaders.UserAgent.ParseAdd(CoreData.UserAgentString);
            UrlProperty = DependencyProperty.Register(
            nameof(UrlProperty),
            typeof(Uri),
            typeof(CheckboxCard),
            new PropertyMetadata(default(Uri), new PropertyChangedCallback((_, _) => { LoadAnnouncements(); })));

            InitializeComponent();
            DefaultStyleKey = typeof(Announcer);
            BringIntoViewRequested += (_, _) => { LoadAnnouncements(); };

            int i = 0;
            PointerPressed += (_, _) => { if (i++ % 3 != 0) { LoadAnnouncements(); } };

            SetText(CoreTools.Translate("Fetching latest announcements, please wait..."));
            _textblock.TextWrapping = TextWrapping.Wrap;
        }

        public async void LoadAnnouncements(bool retry = false)
        {
            try
            {
                Uri announcement_url = Url;
                if (retry)
                {
                    announcement_url = new Uri(Url.ToString().Replace("https://", "http://"));
                }

                HttpResponseMessage response = await NetClient.GetAsync(announcement_url);
                if (response.IsSuccessStatusCode)
                {
                    string[] response_body = (await response.Content.ReadAsStringAsync()).Split("////");
                    string title = response_body[0].Trim().Trim('\n').Trim();
                    string body = response_body[1].Trim().Trim('\n').Trim();
                    string linkId = response_body[2].Trim().Trim('\n').Trim();
                    string linkName = response_body[3].Trim().Trim('\n').Trim();
                    Uri imageUrl = new(response_body[4].Trim().Trim('\n').Trim());
                    SetText(title, body, linkId, linkName);
                    SetImage(imageUrl);
                }
                else
                {
                    SetText(CoreTools.Translate("Could not load announcements - HTTP status code is $CODE").Replace("$CODE", response.StatusCode.ToString()));
                    SetImage(new Uri("ms-appx:///Assets/Images/warn.png"));
                    if (!retry)
                    {
                        LoadAnnouncements(true);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Could not load announcements");
                Logger.Warn(ex);
                SetText(CoreTools.Translate("Could not load announcements - ") + ex.ToString());
                SetImage(new Uri("ms-appx:///Assets/Images/warn.png"));
            }
        }

        public void SetText_Safe(string title, string body, string linkId, string linkName)
        {
            ((MainApp)Application.Current).MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                SetText(title, body, linkId, linkName);
            });
        }

        public void SetText(string title, string body, string linkId, string linkName)
        {
            Paragraph paragraph = new();
            paragraph.Inlines.Add(new Run { Text = title, FontSize = 24, FontWeight = new FontWeight(700), FontFamily = new FontFamily("Segoe UI Variable Display") });
            _textblock.Blocks.Clear();
            _textblock.Blocks.Add(paragraph);

            paragraph = new();
            foreach (string line in body.Split("\n"))
            {
                paragraph.Inlines.Add(new Run { Text = line + " " });
                paragraph.Inlines.Add(new LineBreak());
            }
            Hyperlink link = new();
            link.Inlines.Add(new Run { Text = linkName });
            link.NavigateUri = new Uri("https://marticliment.com/redirect?" + linkId);
            paragraph.Inlines[^1] = link;
            paragraph.Inlines.Add(new LineBreak());

            _textblock.Blocks.Add(paragraph);
        }

        public void SetText(string body)
        {
            Paragraph paragraph = new();
            foreach (string line in body.Split("\n"))
            {
                paragraph.Inlines.Add(new Run { Text = line });
                paragraph.Inlines.Add(new LineBreak());
            }
            _textblock.Blocks.Clear();
            _textblock.Blocks.Add(paragraph);
        }

        public void SetImage(Uri url)
        {
            BitmapImage bitmapImage = new()
            {
                UriSource = url
            };
            _image.Source = bitmapImage;

        }
    }
}
