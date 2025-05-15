using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Interface.Enums;

namespace UniGetUI.Interface.Widgets
{
    public partial class LocalIcon : FontIcon
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

    public static class IconBuilder
    {
        private static FontFamily customFont = null!;
        private static FontFamily symbolFont = null!;

        public static IconType SetIcon(this TextBlock block, IconType icon)
        {
            customFont ??= (FontFamily)Application.Current.Resources["SymbolFont"];
            block.Text = $"{(char)icon}";
            block.FontFamily = customFont;
            return icon;
        }

        public static IconType GetIcon(this TextBlock block)
        {
            return IconType.Help;
        }

        public static string SetGlyph(this TextBlock block, string glyph)
        {
            symbolFont ??= new FontFamily("Segoe Fluent Icons,Segoe MDL2 Assets");
            block.Text = glyph;
            block.FontFamily = symbolFont;
            return glyph;
        }

        public static string GetGlyph(this TextBlock block)
        {
            return block.Text;
        }
    }

    public partial class LocalIconSource : FontIconSource
    {
        public static FontFamily font = (FontFamily)Application.Current.Resources["SymbolFont"];

        public IconType Icon
        {
            set => Glyph = $"{(char)value}";
        }

        public LocalIconSource()
        {
            FontFamily = font;
        }

        public LocalIconSource(IconType icon) : this()
        {
            Glyph = $"{(char)icon}";
        }
    }
}
