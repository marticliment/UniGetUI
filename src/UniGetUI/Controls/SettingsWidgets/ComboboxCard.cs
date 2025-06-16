using System.Collections.ObjectModel;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public sealed partial class ComboboxCard : SettingsCard
    {
        private readonly ComboBox _combobox = new();
        private readonly ObservableCollection<string> _elements = [];
        private readonly Dictionary<string, string> _values_ref = [];
        private readonly Dictionary<string, string> _inverted_val_ref = [];

        private Settings.K settings_name = Settings.K.Unset;
        public Settings.K SettingName
        {
            set
            {
                settings_name = value;
            }
        }

        public string Text
        {
            set => Header = CoreTools.Translate(value);
        }

        public event EventHandler<EventArgs>? ValueChanged;

        public ComboboxCard()
        {
            _combobox.MinWidth = 200;
            _combobox.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = _elements });
            Content = _combobox;
        }

        public void AddItem(string name, string value)
        {
            AddItem(name, value, true);
        }

        public void AddItem(string name, string value, bool translate)
        {
            if (translate)
            {
                name = CoreTools.Translate(name);
            }

            _elements.Add(name);
            _values_ref.Add(name, value);
            _inverted_val_ref.Add(value, name);
        }

        public void ShowAddedItems()
        {
            try
            {
                string savedItem = Settings.GetValue(settings_name);
                _combobox.SelectedIndex = _elements.IndexOf(_inverted_val_ref[savedItem]);
            }
            catch
            {
                _combobox.SelectedIndex = 0;
            }
            _combobox.SelectionChanged += (_, _) =>
            {
                try
                {
                    Settings.SetValue(settings_name, _values_ref[_combobox.SelectedItem?.ToString() ?? ""]);
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex);
                }
            };
        }

        public string SelectedValue() => _combobox.SelectedValue.ToString() ?? throw new InvalidCastException();
    }
}
