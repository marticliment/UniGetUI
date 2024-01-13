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
using Windows.Devices.Geolocation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.SettingsTab.Widgets
{
    public class ButtonCardEventArgs : EventArgs
    {
        public ButtonCardEventArgs()
        {
        }
    }


    public sealed class ButtonCard : SettingsCard
    {
        private static Button _button;
        private static MainAppBindings bindings = new MainAppBindings();
        
        public string ButtonText
        {
            get => (string)GetValue(ButtonProperty);
            set => SetValue(ButtonProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        DependencyProperty TextProperty;

        DependencyProperty ButtonProperty = DependencyProperty.Register(
        nameof(ButtonText),
        typeof(string),
        typeof(ButtonCard),
        new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { _button.Content = bindings.Translate((string)e.NewValue); })));

        public event EventHandler<ButtonCardEventArgs> ButtonClicked;

        protected void __button_clicked()
        {
            ButtonClicked.Invoke(this, new ButtonCardEventArgs());
        }

        public ButtonCard()
        {
            TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(ButtonCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { ((SettingsCard)this).Header = bindings.Translate((string)e.NewValue); })));

            _button = new Button();
            _button.MinWidth = 200;
            _button.Click += (s, e) => { __button_clicked(); };

            this.DefaultStyleKey = typeof(ButtonCard);
            this.Content = _button;
        }
    }
}
