
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;

namespace UniGetUI.Interface.Widgets
{
    public partial class BetterMenu : MenuFlyout
    {
        private readonly Style menuyStyle = (Style)Application.Current.Resources["BetterContextMenu"];
        public BetterMenu()
        {
            MenuFlyoutPresenterStyle = menuyStyle;
        }
    }

    public partial class BetterMenuItem : MenuFlyoutItem
    {
        private readonly Style menuStyle = (Style)Application.Current.Resources["BetterMenuItem"];

        public IconType IconName
        {
            set
            {
                var icon = new LocalIcon(value) { FontSize = 24 };
                Icon = icon;
            }
        }

        public new string Text
        {
            set => base.Text = CoreTools.Translate(value);
        }

        public BetterMenuItem()
        {
            Style = menuStyle;
        }
    }

    public partial class BetterToggleMenuItem : ToggleMenuFlyoutItem
    {
        private readonly Style menuStyle = (Style)Application.Current.Resources["BetterToggleMenuItem"];

        public IconType IconName
        {
            set
            {
                var icon = new LocalIcon(value) { FontSize = 24 };
                Icon = icon;
            }
        }

        public new string Text
        {
            set => base.Text = CoreTools.Translate(value);
        }

        public BetterToggleMenuItem()
        {
            Style = menuStyle;
        }
    }
}
