using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Interface.Enums;

namespace UniGetUI.Interface.Widgets
{
    public class LocalIcon : FontIcon
    {
        public static FontFamily font = (FontFamily)Application.Current.Resources["SymbolFont"];

        public IconType Icon
        {
            set => Glyph = $"{(char)value}";
        }

        public LocalIcon()
        {
            FontFamily = font;
        }

        public LocalIcon(IconType icon) : this()
        {
            Glyph = $"{(char)icon}";
        }
    }
}
