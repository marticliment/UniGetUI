using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModernWindow.PackageEngine;

namespace ModernWindow.Interface.Widgets
{
    public class MenuForPackage : MenuFlyout
    {
        public event EventHandler<Package> AboutToShow;
        DependencyProperty PackageProperty;

        public MenuForPackage() : base()
        {
            PackageProperty = DependencyProperty.Register(
                nameof(Package),
                typeof(Package),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { })));

            Opening += (s, e) => { AboutToShow?.Invoke(this, Package); };
        }

        public Package Package
        {
            get => (Package)GetValue(PackageProperty);
            set => SetValue(PackageProperty, value);
        }
    }
    public class MenuItemForPackage : MenuFlyoutItem
    {
        public event EventHandler<Package> Invoked;

        DependencyProperty PackageProperty;

        public Package Package
        {
            get => (Package)GetValue(PackageProperty);
            set => SetValue(PackageProperty, value);
        }

        DependencyProperty IconNameProperty;

        public string IconName
        {
            get => (string)GetValue(IconNameProperty);
            set => SetValue(IconNameProperty, (string)value);
        }

        public MenuItemForPackage() : base()
        {
            PackageProperty = DependencyProperty.Register(
                nameof(Package),
                typeof(Package),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { })));

            IconNameProperty = DependencyProperty.Register(
                nameof(IconName),
                typeof(string),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => {
                    Icon = new LocalIcon(e.NewValue as string);
                })));

            Click += (s, e) => { Invoked?.Invoke(this, Package); };
        }

    }
}
