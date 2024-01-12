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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.SettingsTab.Widgets
{
    public sealed class CheckboxCard : SettingsCard
    {
        private static CheckBox _checkbox;
        private static TextBlock _textblock;
        private static MainAppBindings bindings = new MainAppBindings();

        public string SettingName
        {
            get => (string)GetValue(SettingProperty);
            set => SetValue(SettingProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public bool Checked
        {
            get => (bool)_checkbox.IsChecked;
        }

        DependencyProperty SettingProperty = DependencyProperty.Register(
        nameof(SettingName),
        typeof(string),
        typeof(CheckboxCard),
        new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { _checkbox.IsChecked = bindings.GetSettings((string)e.NewValue) ^ ((string)e.NewValue).StartsWith("Disable"); })));

        DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(CheckboxCard),
        new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { _textblock.Text = bindings.Translate((string)e.NewValue); })));


        DependencyProperty IsCheckBoxChecked = DependencyProperty.Register(
        nameof(Checked),
        typeof(bool),
        typeof(CheckboxCard),
        new PropertyMetadata(default(bool), new PropertyChangedCallback((d, e) => {})));

        public CheckboxCard()
        {
            ContentAlignment = ContentAlignment.Left;
            HorizontalAlignment = HorizontalAlignment.Stretch;

            _checkbox = new CheckBox();
            _textblock = new TextBlock();
            this.DefaultStyleKey = typeof(CheckboxCard);
            this.Content = _checkbox;
            _textblock.HorizontalAlignment = HorizontalAlignment.Stretch;
            _textblock.VerticalAlignment = VerticalAlignment.Center;
            _checkbox.HorizontalAlignment = HorizontalAlignment.Stretch;
            _checkbox.Content = _textblock;
            _checkbox.Checked += (s, e) => { bindings.SetSettings(SettingName, true ^ SettingName.StartsWith("Disable")); };
            _checkbox.Unchecked += (s, e) => { bindings.SetSettings(SettingName, false ^ SettingName.StartsWith("Disable")); };
        }
    }
}
