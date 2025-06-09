using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine.SecureSettings;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public partial class SecureCheckboxCard : SettingsCard
    {
        public ToggleSwitch _checkbox;
        public TextBlock _textblock;
        public ProgressRing _loading;
        protected bool IS_INVERTED;

        protected string setting_name = "";
        public virtual string SettingName
        {
            set
            {
                _checkbox.IsEnabled = false;
                setting_name = value;
                IS_INVERTED = value.StartsWith("Disable");
                _checkbox.IsOn = SecureSettings.Get(setting_name) ^ IS_INVERTED ^ ForceInversion;
                _textblock.Opacity = _checkbox.IsOn ? 1 : 0.7;
                _checkbox.IsEnabled = true;
            }
        }

        public bool ForceInversion { get; set; }

        public bool Checked
        {
            get => _checkbox.IsOn;
        }
        public virtual event EventHandler<EventArgs>? StateChanged;

        public string Text
        {
            set => _textblock.Text = CoreTools.Translate(value);
        }

        public SecureCheckboxCard()
        {
            _checkbox = new ToggleSwitch()
            {
                Margin = new Thickness(0, 0, 8, 0)
            };

            _loading = new ProgressRing() { IsIndeterminate = true, Visibility = Visibility.Collapsed};
            _textblock = new TextBlock()
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            IS_INVERTED = false;
            Content = new StackPanel()
            {
                Spacing = 4,
                Orientation = Orientation.Horizontal,
                Children = { _loading, _checkbox },
            };
            Header = _textblock;

            _checkbox.HorizontalAlignment = HorizontalAlignment.Stretch;
            _checkbox.Toggled += (s, e) => _ = _checkbox_Toggled();
        }
        protected virtual async Task _checkbox_Toggled()
        {
            try
            {
                if (_checkbox.IsEnabled is false)
                    return;

                _loading.Visibility = Visibility.Visible;
                _checkbox.IsEnabled = false;
                StateChanged?.Invoke(this, EventArgs.Empty);
                await SecureSettings.TrySet(setting_name, _checkbox.IsOn ^ IS_INVERTED ^ ForceInversion);
                _textblock.Opacity = _checkbox.IsOn ? 1 : 0.7;
                _checkbox.IsOn = SecureSettings.Get(setting_name);
                _loading.Visibility = Visibility.Collapsed;
                _checkbox.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex);
                _checkbox.IsOn = SecureSettings.Get(setting_name);
                _loading.Visibility = Visibility.Collapsed;
                _checkbox.IsEnabled = true;
            }
        }
    }
}
