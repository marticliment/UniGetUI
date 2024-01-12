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
        private static TextBlock _textblock;
        private static MainAppBindings bindings = new MainAppBindings();
        private ObservableCollection<string> _elements;
        private Dictionary<string, string> _values_ref;
        private Dictionary<string, string> _inverted_val_ref;

        public string SettingName
        {
            get => (string)GetValue(SettingProperty);
            set => SetValue(SettingProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
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

        DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(CheckboxCard),
        new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { _textblock.Text = bindings.Translate((string)e.NewValue); })));

        public ComboboxCard()
        { 

            _elements = new ObservableCollection<string>();
            _values_ref = new Dictionary<string, string>();
            _inverted_val_ref = new Dictionary<string, string>();

            ContentAlignment = ContentAlignment.Left;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            _combobox = new ComboBox();
            _combobox.HorizontalAlignment = HorizontalAlignment.Right;
            _combobox.Width = 200;
            _combobox.SetBinding(ComboBox.ItemsSourceProperty, new Binding() { Source = _elements });
            _textblock = new TextBlock();
            _textblock.VerticalAlignment = VerticalAlignment.Center;
            _textblock.HorizontalAlignment = HorizontalAlignment.Left;

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(200, GridUnitType.Pixel) });
            Grid.SetColumn(_combobox, 1);
            Grid.SetColumn(_textblock, 0);
            g.ColumnSpacing = 10;

            g.HorizontalAlignment = HorizontalAlignment.Stretch;
            g.VerticalAlignment = VerticalAlignment.Center;
            g.Children.Add(_textblock);
            g.Children.Add(_combobox);


            this.DefaultStyleKey = typeof(CheckboxCard);
            this.Content = g;
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
                    Console.WriteLine("Setting item "+_combobox.SelectedItem.ToString());
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
