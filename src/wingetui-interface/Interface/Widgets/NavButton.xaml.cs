using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using ModernWindow.Structures;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface.Widgets
{
    public sealed partial class NavButton : UserControl
    {

        private MainAppBindings bindings = MainAppBindings.Instance;
        public class NavButtonEventArgs : EventArgs
        {
            public NavButtonEventArgs()
            {
            }
        }
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        DependencyProperty TextProperty;
        public string Glyph
        {
            get => (string)GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }
        DependencyProperty GlyphProperty;

        public event EventHandler<NavButtonEventArgs> Click;

        public NavButton()
        {
            this.InitializeComponent();

            this.DefaultStyleKey = typeof(NavButton);


            TextProperty = DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(NavButton),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => {
                    string val = (string)e.NewValue;
                    if(val.Contains(" "))
                    {
                        Height = 58 + 14;
                        VariableGrid.RowDefinitions[1].Height = new GridLength(26 + 14);
                        TextBlock.Height = 18 + 14;  
                    } else
                    {
                        Height = 58;
                        TextBlock.Height = 18;
                        VariableGrid.RowDefinitions[1].Height = new GridLength(26);
                    }
                    TextBlock.Text = val; 
                }))
            );

            GlyphProperty = DependencyProperty.Register(
                nameof(Glyph),
                typeof(string),
                typeof(NavButton),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { IconBlock.Glyph = (string)e.NewValue; }))
            );

            bindings.App.mainWindow.NavButtonList.Add(this);
            
        }
        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            foreach(NavButton button in bindings.App.mainWindow.NavButtonList)
            {
                button.ToggleButton.IsChecked = (button == this);
            }
            Click?.Invoke(this, new NavButtonEventArgs());
        }
    }
}


/*


*/