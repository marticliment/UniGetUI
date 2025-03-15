using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public partial class CheckboxCard : SettingsCard
    {
        public ToggleSwitch _checkbox;
        public TextBlock _textblock;
        protected bool IS_INVERTED;

        protected string setting_name = "";
        public virtual string SettingName
        {
            set
            {
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
        public virtual event EventHandler<EventArgs>? StateChanged;

        public string Text
        {
            set => _textblock.Text = CoreTools.Translate(value);
        }

        public CheckboxCard()
        {
            _checkbox = new ToggleSwitch()
            {
                Margin = new Thickness(0, 0, 8, 0)
            };
            _textblock = new TextBlock()
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            IS_INVERTED = false;
            Content = _checkbox;
            Header = _textblock;

            _checkbox.HorizontalAlignment = HorizontalAlignment.Stretch;
            _checkbox.Toggled += _checkbox_Toggled;
        }
        protected virtual void _checkbox_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.Set(setting_name, _checkbox.IsOn ^ IS_INVERTED ^ ForceInversion);
            StateChanged?.Invoke(this, EventArgs.Empty);
            _textblock.Opacity = _checkbox.IsOn ? 1 : 0.7;
        }
    }

    public partial class CheckboxCard_Dict : CheckboxCard
    {
        public override event EventHandler<EventArgs>? StateChanged;

        private string _dictName = "";
        public string DictionaryName 
        {
            set
            {
                _dictName = value;
                IS_INVERTED = value.StartsWith("Disable");
                if (setting_name != "")
                {
                    _checkbox.IsOn = Settings.GetDictionaryItem<string, bool>(_dictName, setting_name) ^ IS_INVERTED ^ ForceInversion;
                    _textblock.Opacity = _checkbox.IsOn ? 1 : 0.7;
                }
            }
        }

        public override string SettingName
        {
            set
            {
                setting_name = value;
                if (_dictName != "")
                {
                    _checkbox.IsOn = Settings.GetDictionaryItem<string, bool>(_dictName, setting_name) ^ IS_INVERTED ^ ForceInversion;
                    _textblock.Opacity = _checkbox.IsOn ? 1 : 0.7;
                }
            }
        }

        public CheckboxCard_Dict() : base()
        {
        }

        protected override void _checkbox_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.SetDictionaryItem(_dictName, setting_name, _checkbox.IsOn ^ IS_INVERTED ^ ForceInversion);
            StateChanged?.Invoke(this, EventArgs.Empty);
            _textblock.Opacity = _checkbox.IsOn ? 1 : 0.7;
        }
    }
}
