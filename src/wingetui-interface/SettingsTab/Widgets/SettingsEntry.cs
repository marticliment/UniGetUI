using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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

namespace ModernWindow.SettingsTab.Widgets
{
    public sealed class SettingsEntry : SettingsExpander
    {
        public static MainAppBindings bindings = new MainAppBindings();
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
        new DependencyProperty UnderTextProperty;

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
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { ((SettingsExpander)this).Header = bindings.Translate((string)e.NewValue); })));

            UnderTextProperty = DependencyProperty.Register(
            nameof(UnderText),
            typeof(string),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { ((SettingsExpander)this).Description = bindings.Translate((string)e.NewValue); })));

            IconProperty = DependencyProperty.Register(
            nameof(Icon),
            typeof(string),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => {
                BitmapIcon icon = new BitmapIcon();
                icon.UriSource = new Uri("ms-appx:///wingetui/resources/" + (string)e.NewValue + "_white.png");
                ((SettingsExpander)this).HeaderIcon = icon; 
            })));


            CornerRadius = new CornerRadius(8);
            HorizontalAlignment = HorizontalAlignment.Stretch;

            this.DefaultStyleKey = typeof(SettingsExpander);
        }
    }
}
