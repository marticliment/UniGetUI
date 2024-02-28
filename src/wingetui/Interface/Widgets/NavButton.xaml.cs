using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using ModernWindow.Structures;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface.Widgets
{
    public sealed partial class NavButton : UserControl
    {

        private AppTools Tools = AppTools.Instance;
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
            InitializeComponent();

            DefaultStyleKey = typeof(NavButton);


            TextProperty = DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(NavButton),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) =>
                {
                    string val = (string)e.NewValue;
                    if (val.Contains(" "))
                    {
                        Height = 58 + 14;
                        VariableGrid.RowDefinitions[1].Height = new GridLength(26 + 14);
                        TextBlock.Height = 18 + 14;
                        TextBlock.HorizontalAlignment = HorizontalAlignment.Center;
                        TextBlock.VerticalAlignment = VerticalAlignment.Center;
                        TextBlock.TextAlignment = TextAlignment.Center;
                    }
                    else
                    {
                        Height = 58;
                        TextBlock.Height = 18;
                        VariableGrid.RowDefinitions[1].Height = new GridLength(26);
                    }
                    TextBlock.Text = Tools.Translate(val).Replace(" ", "\x0a");
                }))
            );

            GlyphProperty = DependencyProperty.Register(
                nameof(Glyph),
                typeof(string),
                typeof(NavButton),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { IconBlock.Glyph = (string)e.NewValue; }))
            );

            Tools.App.mainWindow.NavButtonList.Add(this);
        }
        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, new NavButtonEventArgs());
        }

        public void ForceClick()
        {
            ToggleButton.IsChecked = true;
            Click?.Invoke(this, new NavButtonEventArgs());
        }
    }
}


/*


*/