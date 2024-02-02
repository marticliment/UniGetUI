using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface.Widgets
{
    public sealed partial class TranslatedTextBlock : UserControl
    {
        static AppTools bindings = AppTools.Instance;

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        public string Suffix
        {
            get => (string)GetValue(SuffixProperty);
            set => SetValue(SuffixProperty, value);

        }
        public string Prefix
        {
            get => (string)GetValue(PrefixProperty);
            set => SetValue(PrefixProperty, value);
        }

        public TextWrapping WrappingMode
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        DependencyProperty TextProperty;
        DependencyProperty PrefixProperty;
        DependencyProperty SuffixProperty;
        DependencyProperty TextWrappingProperty;

        public TranslatedTextBlock()
        {
            TextWrappingProperty = DependencyProperty.Register(
                nameof(WrappingMode),
                typeof(TextWrapping),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { __textblock.TextWrapping = WrappingMode; })));

            this.InitializeComponent();

            TextProperty = DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { __textblock.Text = Prefix + bindings.Translate((string)e.NewValue) + Suffix; })));

            PrefixProperty = DependencyProperty.Register(
                nameof(Prefix),
                typeof(string),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { __textblock.Text = (string)e.NewValue + bindings.Translate(Text) + Suffix; })));

            SuffixProperty = DependencyProperty.Register(
                nameof(Suffix),
                typeof(string),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { __textblock.Text = Prefix + bindings.Translate(Text) + (string)e.NewValue; })));

        }
    }
}
