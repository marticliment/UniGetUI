using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using Windows.UI.ApplicationSettings;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public sealed class CheckboxCard : SettingsCard
    {
        public CheckBox _checkbox;
        private bool IS_INVERTED;

        private string setting_name = "";
        public string SettingName
        {
            set {
                setting_name = value;
                IS_INVERTED = value.StartsWith("Disable");
                _checkbox.IsChecked = Settings.Get(setting_name) ^ IS_INVERTED;
            }
        }

        public bool Checked
        {
            get => _checkbox.IsChecked ?? false;
        }
        public event EventHandler<EventArgs>? StateChanged;

        public string Text
        {
            set => _checkbox.Content = CoreTools.Translate(value);
        }

        public CheckboxCard()
        {
            _checkbox = new CheckBox();
            IS_INVERTED = false;

            ContentAlignment = ContentAlignment.Left;
            HorizontalAlignment = HorizontalAlignment.Stretch;

            DefaultStyleKey = typeof(CheckboxCard);
            Content = _checkbox;
            _checkbox.HorizontalAlignment = HorizontalAlignment.Stretch;
            _checkbox.Checked += (s, e) => { Settings.Set(setting_name, true ^ IS_INVERTED); StateChanged?.Invoke(this, EventArgs.Empty); };
            _checkbox.Unchecked += (s, e) => { Settings.Set(setting_name, false ^ IS_INVERTED); StateChanged?.Invoke(this, EventArgs.Empty); };
        }
    }
}
