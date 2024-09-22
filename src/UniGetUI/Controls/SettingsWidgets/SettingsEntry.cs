using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;

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
            set => Header = CoreTools.Translate(value);
        }

        public string UnderText
        {
            set => Description = CoreTools.Translate(value);
        }

        public IconType Icon
        {
            set => HeaderIcon = new LocalIcon(value);
        }

        public SettingsEntry()
        {
            CornerRadius = new CornerRadius(8);
            HorizontalAlignment = HorizontalAlignment.Stretch;

            infoBar = new InfoBar
            {
                Severity = InfoBarSeverity.Warning,
                Title = "",
                Message = CoreTools.Translate("Restart WingetUI to fully apply changes"),
                CornerRadius = new CornerRadius(0),
                BorderThickness = new Thickness(0)
            };
            RestartButton = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Right
            };
            infoBar.ActionButton = RestartButton;
            RestartButton.Content = CoreTools.Translate("Restart WingetUI");
            RestartButton.Click += (_, _) => { MainApp.Instance.KillAndRestart(); };
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
