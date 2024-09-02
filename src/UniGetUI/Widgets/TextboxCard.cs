using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public sealed class TextboxCard : SettingsCard
    {
        private readonly TextBox _textbox = new();
        private readonly HyperlinkButton _helpbutton = new();

        private string setting_name = "";
        public string SettingName
        {
            set {
                setting_name = value;
                _textbox.Text = Settings.GetValue(setting_name);
                _textbox.TextChanged += (_, _) => SaveValue();
            }
        }

        public string Placeholder
        {
            set => _textbox.PlaceholderText = CoreTools.Translate(value);
        }

        public string Text
        {
            set => Header = CoreTools.Translate(value);
        }

        public Uri HelpUrl
        {
            set
            {
                _helpbutton.NavigateUri = value;
                _helpbutton.Visibility = Visibility.Visible;
                _helpbutton.Content = CoreTools.Translate("More info");
            }
        }

        public event EventHandler<EventArgs>? ValueChanged;

        public TextboxCard()
        {

            _helpbutton = new HyperlinkButton
            {
                Visibility = Visibility.Collapsed
            };

            _textbox = new TextBox
            {
                MinWidth = 200,
                MaxWidth = 300
            };

            StackPanel s = new()
            {
                Orientation = Orientation.Horizontal
            };
            s.Children.Add(_helpbutton);
            s.Children.Add(_textbox);

            DefaultStyleKey = typeof(TextboxCard);
            Content = s;
        }

        public void SaveValue()
        {
            string SanitizedText = _textbox.Text;

            if (setting_name.Contains("File"))
            {
                foreach (char rem in "#%&{}\\/<>*?$!'\":;@`|~")
                {
                    SanitizedText = SanitizedText.Replace(rem.ToString(), "");
                }
            }

            if (SanitizedText != "")
            {
                Settings.SetValue(setting_name, SanitizedText);
            }
            else
            {
                Settings.Set(setting_name, false);
            }

            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

    }
}
