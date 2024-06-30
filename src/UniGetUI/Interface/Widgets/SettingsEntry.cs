using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public sealed class SettingsEntry : SettingsExpander
    {
        private readonly InfoBar infoBar;
        private readonly Button RestartButton;
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        readonly DependencyProperty TextProperty;

        public string UnderText
        {
            get => (string)GetValue(UnderTextProperty);
            set => SetValue(UnderTextProperty, value);
        }
        readonly DependencyProperty UnderTextProperty;

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
        readonly DependencyProperty IconProperty;


        public SettingsEntry()
        {
            TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { Header = CoreTools.Translate((string)e.NewValue); })));

            UnderTextProperty = DependencyProperty.Register(
            nameof(UnderText),
            typeof(string),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) => { Description = CoreTools.Translate((string)e.NewValue); })));

            IconProperty = DependencyProperty.Register(
            nameof(Icon),
            typeof(string),
            typeof(CheckboxCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) =>
            {
                HeaderIcon = new LocalIcon((string)e.NewValue);
            })));


            CornerRadius = new CornerRadius(8);
            HorizontalAlignment = HorizontalAlignment.Stretch;

            infoBar = new InfoBar();
            infoBar.Severity = InfoBarSeverity.Warning;
            infoBar.Title = "";
            infoBar.Message = CoreTools.Translate("Restart WingetUI to fully apply changes");
            infoBar.CornerRadius = new CornerRadius(0);
            infoBar.BorderThickness = new Thickness(0);
            RestartButton = new Button();
            RestartButton.HorizontalAlignment = HorizontalAlignment.Right;
            infoBar.ActionButton = RestartButton;
            RestartButton.Content = CoreTools.Translate("Restart WingetUI");
            RestartButton.Click += (s, e) => { MainApp.Instance.KillAndRestart(); };
            ItemsHeader = infoBar;

            DefaultStyleKey = typeof(SettingsExpander);
        }

        public void ShowRestartRequiredBanner()
        {
            infoBar.IsOpen = true;
        }
        public void HideRestartRequiredBanner()
        {
            infoBar.IsOpen = false;
        }
    }
}
