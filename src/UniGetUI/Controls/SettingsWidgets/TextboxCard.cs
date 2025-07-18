using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public sealed partial class TextboxCard : SettingsCard
    {
        private readonly TextBox _textbox;
        private readonly HyperlinkButton _helpbutton;

        private Settings.K setting_name = Settings.K.Unset;
        public Settings.K SettingName
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

            Content = s;
        }

        public void SaveValue()
        {
            string SanitizedText = _textbox.Text;

            if (Settings.ResolveKey(setting_name).Contains("File"))
            {
                SanitizedText = CoreTools.MakeValidFileName(SanitizedText);
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
