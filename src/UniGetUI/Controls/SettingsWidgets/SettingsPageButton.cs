using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Widgets
{
    public partial class SettingsPageButton : SettingsCard
    {
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

        public SettingsPageButton()
        {
            CornerRadius = new CornerRadius(8);
            HorizontalAlignment = HorizontalAlignment.Stretch;
            IsClickEnabled = true;
        }
    }
}
