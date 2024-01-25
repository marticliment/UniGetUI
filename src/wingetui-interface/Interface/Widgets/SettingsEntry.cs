using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using Windows.Graphics.DirectX.Direct3D11;
using static System.Net.Mime.MediaTypeNames;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface.Widgets
{
    public sealed class SettingsEntry : SettingsExpander
    {
        public static MainAppBindings bindings = MainAppBindings.Instance;
        private InfoBar infoBar;
        private Button RestartButton;
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        DependencyProperty TextProperty;

        public string UnderText
        {
            get => (string)GetValue(UnderTextProperty);
            set => SetValue(UnderTextProperty, value);
        }
        DependencyProperty UnderTextProperty;

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
        DependencyProperty IconProperty;


        public SettingsEntry()
        {
            TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { this.Header = bindings.Translate((string)e.NewValue); })));

            UnderTextProperty = DependencyProperty.Register(
            nameof(UnderText),
            typeof(string),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { this.Description = bindings.Translate((string)e.NewValue); })));

            IconProperty = DependencyProperty.Register(
            nameof(Icon),
            typeof(string),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) =>
            {
                this.HeaderIcon = new LocalIcon((string)e.NewValue);
            })));


            CornerRadius = new CornerRadius(8);
            HorizontalAlignment = HorizontalAlignment.Stretch;

            infoBar = new InfoBar();
            infoBar.Severity = InfoBarSeverity.Warning;
            infoBar.Title = "";
            infoBar.Message = bindings.Translate("Restart WingetUI to fully apply changes");
            infoBar.CornerRadius = new CornerRadius(0);
            infoBar.BorderThickness = new Thickness(0);
            RestartButton = new Button();
            RestartButton.HorizontalAlignment = HorizontalAlignment.Right;
            infoBar.ActionButton = RestartButton;
            RestartButton.Content = bindings.Translate("Restart WingetUI");
            RestartButton.Click += (s, e) => { bindings.RestartApp(); };
            ItemsHeader = infoBar;

            DefaultStyleKey = typeof(SettingsExpander);
        }

        public void ShowRestartRequiredBanner()
        {
            infoBar.IsOpen = true;
        }
        public void HideRestartRequiredBanner()
        {
            infoBar.IsOpen = false;
        }
    }
}
