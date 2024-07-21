using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public class TextboxEventArgs : EventArgs
    {

        public TextboxEventArgs()
        {
        }
    }

    public sealed class TextboxCard : SettingsCard
    {
        private readonly TextBox _textbox = new();
        private readonly HyperlinkButton _helpbutton = new();

        public string SettingName
        {
            get => (string)GetValue(SettingProperty);
            set => SetValue(SettingProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public Uri HelpUrl
        {
            get => (Uri)GetValue(HelpUrlProperty);
            set => SetValue(HelpUrlProperty, value);
        }

        private readonly DependencyProperty PlaceholderProperty;

        private readonly DependencyProperty SettingProperty;

        private readonly DependencyProperty TextProperty;

        private readonly DependencyProperty HelpUrlProperty;

        public event EventHandler<TextboxEventArgs>? ValueChanged;

        public TextboxCard()
        {
            TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { Header = CoreTools.Translate((string)e.NewValue); })));

            PlaceholderProperty = DependencyProperty.Register(
            nameof(Placeholder),
            typeof(string),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { _textbox.PlaceholderText = CoreTools.Translate((string)e.NewValue); })));

            SettingProperty = DependencyProperty.Register(
            nameof(SettingName),
            typeof(string),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) =>
            {
                _textbox.Text = Settings.GetValue((string)e.NewValue);
                _textbox.TextChanged += (sender, e) => { SaveValue(); };
            })));

            HelpUrlProperty = DependencyProperty.Register(
            nameof(HelpUrl),
            typeof(Uri),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) =>
            {
                _helpbutton.NavigateUri = (Uri)e.NewValue;
                _helpbutton.Visibility = Visibility.Visible;
                _helpbutton.Content = CoreTools.Translate("More info");
            })));

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

            if (SettingName.Contains("File"))
            {
                foreach (char rem in "#%&{}\\/<>*?$!'\":;@`|~")
                {
                    SanitizedText = SanitizedText.Replace(rem.ToString(), "");
                }
            }

            if (SanitizedText != "")
            {
                Settings.SetValue(SettingName, SanitizedText);
            }
            else
            {
                Settings.Set(SettingName, false);
            }

            TextboxEventArgs args = new();
            ValueChanged?.Invoke(this, args);
        }

    }
}
