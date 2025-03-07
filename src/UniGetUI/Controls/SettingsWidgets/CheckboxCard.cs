using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public sealed partial class CheckboxCard : SettingsCard
    {
        public ToggleSwitch _checkbox;
        public TextBlock _textblock;
        private bool IS_INVERTED;

        private string setting_name = "";
        public string SettingName
        {
            set {
                setting_name = value;
                IS_INVERTED = value.StartsWith("Disable");
                _checkbox.IsOn = Settings.Get(setting_name) ^ IS_INVERTED ^ ForceInversion;
                _textblock.Opacity = _checkbox.IsOn ? 1 : 0.7;
            }
        }

        public bool ForceInversion { get; set; }

        public bool Checked
        {
            get => _checkbox.IsOn;
        }
        public event EventHandler<EventArgs>? StateChanged;

        public string Text
        {
            set => _textblock.Text = CoreTools.Translate(value);
        }

        public CheckboxCard()
        {
            _checkbox = new ToggleSwitch()
            {
                Margin = new Thickness(-8, 0, 0, 0)
            };
            _textblock = new TextBlock()
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new(12, 0, 8, 0),
                TextWrapping = TextWrapping.Wrap
            };

            _checkbox.OnContent = _checkbox.OffContent = "";
            IS_INVERTED = false;

            ContentAlignment = ContentAlignment.Left;
            HorizontalAlignment = HorizontalAlignment.Stretch;

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            g.Children.Add(_checkbox);
            g.Children.Add(_textblock);
            Grid.SetColumn(_textblock, 1);
            Content = g;

            _checkbox.HorizontalAlignment = HorizontalAlignment.Stretch;
            _checkbox.Toggled += (_, _) => {

                Settings.Set(setting_name, _checkbox.IsOn ^ IS_INVERTED ^ ForceInversion);
                StateChanged?.Invoke(this, EventArgs.Empty);
                _textblock.Opacity = _checkbox.IsOn ? 1 : 0.7;

            };
            // _checkbox.Unchecked += (_, _) => { Settings.Set(setting_name, false ^ IS_INVERTED ^ ForceInversion); StateChanged?.Invoke(this, EventArgs.Empty); };
        }
    }
}
