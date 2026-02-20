using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using Windows.UI.Text;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public sealed partial class CheckboxButtonCard : SettingsCard
    {
        public ToggleSwitch _checkbox;
        public TextBlock _textblock;
        public ButtonBase Button;
        private bool IS_INVERTED;
        private string _translatedCheckboxText = string.Empty;
        private string _translatedButtonText = string.Empty;

        private void UpdateAutomationNames()
        {
            if (!string.IsNullOrWhiteSpace(_translatedCheckboxText))
            {
                AutomationProperties.SetName(_checkbox, _translatedCheckboxText);
            }

            if (!string.IsNullOrWhiteSpace(_translatedButtonText))
            {
                string buttonName = string.IsNullOrWhiteSpace(_translatedCheckboxText)
                    ? _translatedButtonText
                    : $"{_translatedButtonText}. {_translatedCheckboxText}";
                AutomationProperties.SetName(Button, buttonName);
            }

            string cardName = _translatedCheckboxText;
            if (!string.IsNullOrWhiteSpace(_translatedButtonText))
            {
                cardName = string.IsNullOrWhiteSpace(cardName)
                    ? _translatedButtonText
                    : $"{cardName}. {_translatedButtonText}";
            }

            if (!string.IsNullOrWhiteSpace(cardName))
            {
                AutomationProperties.SetName(this, cardName);
            }
        }

        private Settings.K setting_name = Settings.K.Unset;
        public Settings.K SettingName
        {
            set {
                setting_name = value;
                IS_INVERTED = Settings.ResolveKey(value).StartsWith("Disable");
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
            set
            {
                _translatedCheckboxText = CoreTools.Translate(value);
                _textblock.Text = _translatedCheckboxText;
                UpdateAutomationNames();
            }
        }

        public string ButtonText
        {
            set
            {
                _translatedButtonText = CoreTools.Translate(value);
                Button.Content = _translatedButtonText;
                UpdateAutomationNames();
            }
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
            Button = new Button()
            {
                Margin = new Thickness(0, 8, 0, 0)

            };
            _checkbox = new ToggleSwitch()
            {
                 Margin = new Thickness(0, 0, 8, 0),
                OnContent = new TextBlock() { Text = CoreTools.Translate("Enabled") },
                OffContent = new TextBlock() { Text = CoreTools.Translate("Disabled") },
            };
            _textblock = new TextBlock()
            {
                Margin = new Thickness(2,0,0,0),
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Style = (Style)Application.Current.Resources["BaseTextBlockStyle"],
                FontWeight = new FontWeight(450),
                Foreground = (SolidColorBrush)Application.Current.Resources["ButtonForeground"]
            };
            IS_INVERTED = false;

            Content = _checkbox;
            Header = _textblock;
            Description = Button;
            _checkbox.Toggled += (_, _) =>
            {
                Settings.Set(setting_name, _checkbox.IsOn ^ IS_INVERTED ^ ForceInversion);
                StateChanged?.Invoke(this, EventArgs.Empty);
                Button.IsEnabled = _checkbox.IsOn ? true : _buttonAlwaysOn;
                _textblock.Opacity = _checkbox.IsOn ? 1 : 0.7;
            };

            Button.Click += (s, e) => Click?.Invoke(s, e);
            UpdateAutomationNames();
        }
    }
}
