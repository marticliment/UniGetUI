using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using UniGetUI.Core;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{

    public class CheckBoxEventArgs : EventArgs
    {
        public bool IsChecked { get; set; }
        public CheckBoxEventArgs(bool _checked)
        {
            IsChecked = _checked;
        }
    }
    public sealed class CheckboxCard : SettingsCard
    {
        public CheckBox _checkbox;

        public string SettingName
        {
            get => (string)GetValue(SettingProperty);
            set => SetValue(SettingProperty, value);
        }


        public bool Checked
        {
            get => _checkbox.IsChecked ?? false;
        }

        public event EventHandler<CheckBoxEventArgs>? StateChanged;

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
        new PropertyMetadata(default(bool), new PropertyChangedCallback((d, e) => { })));

        public CheckboxCard()
        {
            _checkbox = new CheckBox();

            TextProperty = DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { _checkbox.Content = CoreTools.Translate((string)e.NewValue); })));

            SettingProperty = DependencyProperty.Register(
                nameof(SettingName),
                typeof(string),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { _checkbox.IsChecked = Settings.Get((string)e.NewValue) ^ ((string)e.NewValue).StartsWith("Disable"); })));


            ContentAlignment = ContentAlignment.Left;
            HorizontalAlignment = HorizontalAlignment.Stretch;

            DefaultStyleKey = typeof(CheckboxCard);
            Content = _checkbox;
            _checkbox.HorizontalAlignment = HorizontalAlignment.Stretch;
            _checkbox.Checked += (s, e) => { Settings.Set(SettingName, true ^ SettingName.StartsWith("Disable")); StateChanged?.Invoke(this, new CheckBoxEventArgs(true ^ SettingName.StartsWith("Disable"))); };
            _checkbox.Unchecked += (s, e) => { Settings.Set(SettingName, false ^ SettingName.StartsWith("Disable")); StateChanged?.Invoke(this, new CheckBoxEventArgs(false ^ SettingName.StartsWith("Disable"))); };
        }
    }
}