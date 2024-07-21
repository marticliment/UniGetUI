using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.Interface.Widgets
{
    public class BetterMenu : MenuFlyout
    {
        public BetterMenu()
        {
            MenuFlyoutPresenterStyle = (Style)Application.Current.Resources["BetterContextMenu"];
        }
    }

    public class BetterMenuItem : MenuFlyoutItem
    {
        private readonly DependencyProperty IconNameProperty;

        public string IconName
        {
            get => (string)GetValue(IconNameProperty);
            set => SetValue(IconNameProperty, value);
        }

        private new readonly DependencyProperty TextProperty;

        public new string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public BetterMenuItem()
        {
            Style = (Style)Application.Current.Resources["BetterMenuItem"];

            IconNameProperty = DependencyProperty.Register(
                nameof(IconName),
                typeof(string),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) =>
                {
                    Icon = new LocalIcon(e.NewValue as string ?? "");
                })));

            TextProperty = DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) =>
                {
                    (this as MenuFlyoutItem).Text = CoreTools.Translate(e.NewValue as string ?? "");
                })));
        }
    }

    public class MenuForPackage : MenuFlyout
    {
        public event EventHandler<Package>? AboutToShow;
        private readonly DependencyProperty PackageProperty;

        public MenuForPackage()
        {
            MenuFlyoutPresenterStyle = (Style)Application.Current.Resources["BetterContextMenu"];
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
        public event EventHandler<Package>? Invoked;

        private readonly DependencyProperty PackageProperty;

        public Package Package
        {
            get => (Package)GetValue(PackageProperty);
            set => SetValue(PackageProperty, value);
        }

        private readonly DependencyProperty IconNameProperty;

        public string IconName
        {
            get => (string)GetValue(IconNameProperty);
            set => SetValue(IconNameProperty, value);
        }

        public MenuItemForPackage()
        {
            Style = (Style)Application.Current.Resources["BetterMenuItem"];
            PackageProperty = DependencyProperty.Register(
                nameof(Package),
                typeof(Package),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { })));

            IconNameProperty = DependencyProperty.Register(
                nameof(IconName),
                typeof(string),
                typeof(CheckboxCard),
                new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) =>
                {
                    Icon = new LocalIcon(e.NewValue as string ?? "");
                })));

            Click += (s, e) => { Invoked?.Invoke(this, Package); };
        }

    }
}
