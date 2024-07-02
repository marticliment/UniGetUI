using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System.Collections.ObjectModel;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public class ComboCardEventArgs : EventArgs
    {

        public ComboCardEventArgs()
        {
        }
    }

    public sealed class ComboboxCard : SettingsCard
    {
        private readonly ComboBox _combobox;
        private readonly ObservableCollection<string> _elements;
        private readonly Dictionary<string, string> _values_ref;
        private readonly Dictionary<string, string> _inverted_val_ref;

        public string SettingName
        {
            get => (string)GetValue(SettingProperty);
            set => SetValue(SettingProperty, value);
        }

        public string Elements
        {
            get => (string)GetValue(SettingProperty);
            set => SetValue(SettingProperty, value);
        }

        readonly DependencyProperty SettingProperty = DependencyProperty.Register(
        nameof(SettingName),
        typeof(string),
        typeof(CheckboxCard),
        new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { })));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        readonly DependencyProperty TextProperty;

        public event EventHandler<ComboCardEventArgs>? ValueChanged;

        public ComboboxCard()
        {
            TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { Header = CoreTools.Translate((string)e.NewValue); })));

            _elements = [];
            _values_ref = [];
            _inverted_val_ref = [];

            _combobox = new ComboBox
            {
                MinWidth = 200
            };
            _combobox.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = _elements });

            DefaultStyleKey = typeof(CheckboxCard);
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
                string savedItem = Settings.GetValue(SettingName);
                _combobox.SelectedIndex = _elements.IndexOf(_inverted_val_ref[savedItem]);
            }
            catch
            {
                _combobox.SelectedIndex = 0;
            }
            _combobox.SelectionChanged += (sender, e) =>
            {
                try
                {
                    Settings.SetValue(SettingName, _values_ref[_combobox.SelectedItem?.ToString() ?? ""]);
                    ValueChanged?.Invoke(this, new ComboCardEventArgs());
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex);
                }
            };
        }
    }
}
