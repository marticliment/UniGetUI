using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;

namespace UniGetUI.Interface.Widgets
{
    public class BetterMenu : MenuFlyout
    {
        private readonly Style menuyStyle = (Style)Application.Current.Resources["BetterContextMenu"];
        public BetterMenu() : base()
        {
            MenuFlyoutPresenterStyle = menuyStyle;
        }
    }

    public class BetterMenuItem : MenuFlyoutItem
    {
        private readonly Style menuyStyle = (Style)Application.Current.Resources["BetterMenuItem"];

        public string IconName
        {
            set
            {
                var icon = new LocalIcon(value);
                icon.FontSize = 24;
                Icon = icon;
            }
        }

        public new string Text
        {
            set => base.Text = CoreTools.Translate(value);
        }

        public BetterMenuItem() : base()
        {
            Style = menuyStyle;
        }
    }
}
