using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public sealed partial class NavButton : UserControl
    {
        public string Text
        {
            set {
                string val = CoreTools.Translate(value);
                int count = val.Count(x => x == ' ');
                TextBlock.Text = val.Replace(" ", "\x0a");
                ToggleButton.Content = val.Replace(" ", "\x0a");
            }
        }

        public string Glyph
        {
            set => IconBlock.Glyph = value;
        }

        public event EventHandler<EventArgs>? Click;

        public NavButton()
        {
            InitializeComponent();
            DefaultStyleKey = typeof(NavButton);
            MainApp.Instance.MainWindow.NavButtonList.Add(this);
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, EventArgs.Empty);
        }

        public void ForceClick()
        {
            ToggleButton.IsChecked = true;
            Click?.Invoke(this, EventArgs.Empty);
        }
    }
}