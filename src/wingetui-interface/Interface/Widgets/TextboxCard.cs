using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using CommunityToolkit.WinUI.Controls;
using ModernWindow.Structures;
using CommunityToolkit.WinUI;
using Windows.Security.Cryptography.Certificates;
using Windows.Networking.XboxLive;
using System.Reflection.Emit;
using System.Numerics;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.VoiceCommands;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface.Widgets
{
    public class TextboxEventArgs : EventArgs
    {

        public TextboxEventArgs()
        {
        }
    }

    public sealed class TextboxCard : SettingsCard
    {
        private TextBox _textbox;
        private HyperlinkButton _helpbutton;
        private static MainAppBindings bindings = MainAppBindings.Instance;
        private static string _text;

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

        DependencyProperty PlaceholderProperty;

        DependencyProperty SettingProperty;

        DependencyProperty TextProperty;

        DependencyProperty HelpUrlProperty;

        public event EventHandler<TextboxEventArgs> ValueChanged;

        public TextboxCard()
        {
            TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { this.Header = bindings.Translate((string)e.NewValue); })));

            PlaceholderProperty = DependencyProperty.Register(
            nameof(Placeholder),
            typeof(string),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { _textbox.PlaceholderText = bindings.Translate((string)e.NewValue); })));

            SettingProperty = DependencyProperty.Register(
            nameof(SettingName),
            typeof(string),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) =>
            {
                _textbox.Text = bindings.GetSettingsValue((string)e.NewValue);
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
                _helpbutton.Content = bindings.Translate("More info");
            })));

            _helpbutton = new HyperlinkButton();
            _helpbutton.Visibility = Visibility.Collapsed;

            _textbox = new TextBox();
            _textbox.MinWidth = 200;

            StackPanel s = new StackPanel();
            s.Orientation = Orientation.Horizontal;
            s.Children.Add(_helpbutton);
            s.Children.Add(_textbox);

            DefaultStyleKey = typeof(TextboxCard);
            Content = s;
        }

        public void SaveValue()
        {
            string SanitizedText = _textbox.Text;

            if (SettingName.Contains("File"))
                foreach (char rem in "#%&{}\\/<>*?$!'\":;@`|~")
                    SanitizedText = SanitizedText.Replace(rem.ToString(), "");

            if (SanitizedText != "")
                bindings.SetSettingsValue(SettingName, SanitizedText);
            else
                bindings.SetSettings(SettingName, false);
            var args = new TextboxEventArgs();
            ValueChanged?.Invoke(this, args);
        }

    }
}
