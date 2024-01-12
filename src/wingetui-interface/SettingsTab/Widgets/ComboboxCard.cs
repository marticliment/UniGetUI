using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using CommunityToolkit.WinUI.Controls;
using ModernWindow.Structures;
using CommunityToolkit.WinUI;
using Windows.Security.Cryptography.Certificates;
using Windows.Networking.XboxLive;
using System.Reflection.Emit;
using System.Numerics;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.SettingsTab.Widgets
{
    public sealed class ComboboxCard : SettingsCard
    {
        private ComboBox _combobox;
        private static MainAppBindings bindings = new MainAppBindings();
        private ObservableCollection<string> _elements;
        private Dictionary<string, string> _values_ref;
        private Dictionary<string, string> _inverted_val_ref;

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

        DependencyProperty SettingProperty = DependencyProperty.Register(
        nameof(SettingName),
        typeof(string),
        typeof(CheckboxCard),
        new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { })));

        public ComboboxCard()
        { 

            _elements = new ObservableCollection<string>();
            _values_ref = new Dictionary<string, string>();
            _inverted_val_ref = new Dictionary<string, string>();

            _combobox = new ComboBox();
            _combobox.MinWidth = 200;
            _combobox.SetBinding(ComboBox.ItemsSourceProperty, new Binding() { Source = _elements });

            this.DefaultStyleKey = typeof(CheckboxCard);
            this.Content = _combobox;
        }

        public void AddItem(string name, string value)
        {
            AddItem(name, value, true);
        }
        public void AddItem(string name, string value, bool translate)
        {
            if(translate)
                name = bindings.Translate(name);
            _elements.Add(name);
            _values_ref.Add(name, value);
            _inverted_val_ref.Add(value, name);
        }

        public void ShowAddedItems()
        {
            try
            {
                string savedItem = bindings.GetSettingsValue(SettingName);
                _combobox.SelectedIndex = _elements.IndexOf(_inverted_val_ref[savedItem]);
            }
            catch
            {
                _combobox.SelectedIndex = 0;
            }
            _combobox.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                try
                {
                    bindings.SetSettingsValue(SettingName, _values_ref[_combobox.SelectedItem.ToString()]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }; 
        }
    }
}
