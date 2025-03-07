using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using Windows.UI.Text;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public sealed class CheckboxButtonCard : SettingsCard
    {
        public ToggleSwitch _checkbox;
        public TextBlock _textblock;
        public Button Button;
        private bool IS_INVERTED;

        private string setting_name = "";
        public string SettingName
        {
            set {
                setting_name = value;
                IS_INVERTED = value.StartsWith("Disable");
                _checkbox.IsOn = Settings.Get(setting_name) ^ IS_INVERTED ^ ForceInversion;
                _textblock.Opacity = _checkbox.IsOn ? 1 : 0.7;
                Button.IsEnabled = (_checkbox.IsOn) || _buttonAlwaysOn ;
            }
        }

        public bool ForceInversion { get; set; }

        public bool Checked
        {
            get => _checkbox.IsOn;
        }
        public event EventHandler<EventArgs>? StateChanged;
        public new event EventHandler<RoutedEventArgs>? Click;

        public string CheckboxText
        {
            set => _textblock.Text = CoreTools.Translate(value);
        }

        public string ButtonText
        {
            set => Button.Content = CoreTools.Translate(value);
        }

        private bool _buttonAlwaysOn;
        public bool ButtonAlwaysOn
        {
            set
            {
                _buttonAlwaysOn = value;
                Button.IsEnabled = (_checkbox.IsOn) || _buttonAlwaysOn ;
            }
        }

        public CheckboxButtonCard()
        {
            Button = new Button();
            _checkbox = new ToggleSwitch()
            {
                Margin = new Thickness(-8, 0, 0, 0)
            };
            _textblock = new TextBlock()
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new(12, 0, 8, 0),
                TextWrapping = TextWrapping.Wrap,
                Style = (Style)Application.Current.Resources["BaseTextBlockStyle"],
                FontWeight = new FontWeight(450),
                Foreground = (SolidColorBrush)Application.Current.Resources["ButtonForeground"]
            };
            IS_INVERTED = false;
            _checkbox.OnContent = _checkbox.OffContent = "";

            //ContentAlignment = ContentAlignment.Left;
            //HorizontalAlignment = HorizontalAlignment.Stretch;

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            g.Children.Add(_checkbox);
            g.Children.Add(_textblock);
            Grid.SetColumn(_textblock, 1);

            Description = g;
            Content = Button;
            g.HorizontalAlignment = HorizontalAlignment.Stretch;
            _checkbox.Toggled += (_, _) =>
            {
                Settings.Set(setting_name, _checkbox.IsOn ^ IS_INVERTED ^ ForceInversion);
                StateChanged?.Invoke(this, EventArgs.Empty);
                Button.IsEnabled = _checkbox.IsOn ? true : _buttonAlwaysOn;
                _textblock.Opacity = _checkbox.IsOn ? 1 : 0.7;
            };


            Button.MinWidth = 200;
            Button.Click += (s, e) => Click?.Invoke(s, e);
        }
    }
}
