using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using UniGetUI.Core.Logging;

namespace UniGetUI.Interface.Widgets
{
    public class LocalIcon : FontIcon
    {
        private static string GetGlyphForName(string icon_name)
        {
            string val = icon_name switch
            {
                "add_to" => "\uE900",
                "android" => "\uE901",
                "backward" => "\uE902",
                "bucket" => "\uE903",
                "buggy" => "\uE904",
                "checksum" => "\uE905",
                "choco" => "\uE906",
                "clipboard_list" => "\uE907",
                "close_round" => "\uE908",
                "collapse" => "\uE909",
                "console" => "\uE90A",
                "copy" => "\uE90B",
                "cross" => "\uE90C",
                "delete" => "\uE90D",
                "disk" => "\uE90E",
                "dotnet" => "\uE90F",
                "download" => "\uE910",
                "empty" => "\uE911",
                "expand" => "\uE912",
                "experimental" => "\uE913",
                "forward" => "\uE914",
                "gog" => "\uE915",
                "help" => "\uE916",
                "history" => "\uE917",
                "home" => "\uE918",
                "id" => "\uE919",
                "info_round" => "\uE91A",
                "installed" => "\uE91B",
                "installed_filled" => "\uE91C",
                "interactive" => "\uE91D",
                "launch" => "\uE91E",
                "loading" => "\uE91F",
                "loading_filled" => "\uE920",
                "local_pc" => "\uE921",
                "megaphone" => "\uE922",
                "ms_store" => "\uE923",
                "node" => "\uE924",
                "open_folder" => "\uE925",
                "options" => "\uE926",
                "package" => "\uE927",
                "pin" => "\uE928",
                "pin_filled" => "\uE929",
                "powershell" => "\uE92A",
                "python" => "\uE92B",
                "reload" => "\uE92C",
                "sandclock" => "\uE92D",
                "save_as" => "\uE92E",
                "scoop" => "\uE92F",
                "search" => "\uE930",
                "settings" => "\uE931",
                "share" => "\uE932",
                "skip" => "\uE933",
                "steam" => "\uE934",
                "sys_tray" => "\uE935",
                "uac" => "\uE936",
                "undelete" => "\uE937",
                "update" => "\uE938",
                "upgradable" => "\uE939",
                "upgradable_filled" => "\uE93A",
                "uplay" => "\uE93B",
                "version" => "\uE93C",
                "warning" => "\uE93D",
                "warning_filled" => "\uE93E",
                "warning_round" => "\uE93F",
                "winget" => "\uE940",
                _ => "\u0000"
            };
            if(val == "\u0000") Logger.Error($"Invalid icon {icon_name}");
            return val;
        }

        public string IconName
        {
            get => Glyph;
            set => Glyph = GetGlyphForName(value);
        }

        public LocalIcon()
        {
            FontFamily = (FontFamily)Application.Current.Resources["SymbolFont"];
        }

        public LocalIcon(string iconName) : this()
        {
            Glyph = GetGlyphForName(iconName);
        }
    }
}
