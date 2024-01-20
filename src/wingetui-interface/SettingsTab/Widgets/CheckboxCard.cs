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

    public class CheckBoxEventArgs : EventArgs
    {
        public bool IsChecked { get; set;}
        public CheckBoxEventArgs(bool _checked)
        {
            IsChecked = _checked;
        }
    }
    public sealed class CheckboxCard : SettingsCard
    {
        public CheckBox _checkbox;
        private MainAppBindings bindings = MainAppBindings.Instance;

        public string SettingName
        {
            get => (string)GetValue(SettingProperty);
            set => SetValue(SettingProperty, value);
        }


        public bool Checked
        {
            get => (bool)_checkbox.IsChecked;
        }

        public event EventHandler<CheckBoxEventArgs> StateChanged;

        DependencyProperty SettingProperty;

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        DependencyProperty TextProperty;

        DependencyProperty IsCheckBoxChecked = DependencyProperty.Register(
        nameof(Checked),
        typeof(bool),
        typeof(CheckboxCard),
        new PropertyMetadata(default(bool), new PropertyChangedCallback((d, e) => {})));

        public CheckboxCard()
        {
            TextProperty = DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { _checkbox.Content = bindings.Translate((string)e.NewValue); })));

            SettingProperty = DependencyProperty.Register(
                nameof(SettingName),
                typeof(string),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { _checkbox.IsChecked = bindings.GetSettings((string)e.NewValue) ^ ((string)e.NewValue).StartsWith("Disable"); })));


            ContentAlignment = ContentAlignment.Left;
            HorizontalAlignment = HorizontalAlignment.Stretch;

            _checkbox = new CheckBox();
            this.DefaultStyleKey = typeof(CheckboxCard);
            this.Content = _checkbox;
            _checkbox.HorizontalAlignment = HorizontalAlignment.Stretch;
            _checkbox.Checked += (s, e) => { bindings.SetSettings(SettingName, true ^ SettingName.StartsWith("Disable")); StateChanged?.Invoke(this, new CheckBoxEventArgs(true ^ SettingName.StartsWith("Disable"))); };
            _checkbox.Unchecked += (s, e) => { bindings.SetSettings(SettingName, false ^ SettingName.StartsWith("Disable")); StateChanged?.Invoke(this, new CheckBoxEventArgs(false ^ SettingName.StartsWith("Disable"))); };
        }
    }
}