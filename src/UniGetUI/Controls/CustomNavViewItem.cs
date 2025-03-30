using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using UniGetUI.Core.Tools;
using UniGetUI.Interface;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Widgets;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.UI.WebUI;
using YamlDotNet.Core.Tokens;

namespace UniGetUI.Controls;
internal class CustomNavViewItem : NavigationViewItem
{

    public IconType LocalIcon
    {
        set => base.Icon = new LocalIcon(value);

    }
    public string GlyphIcon
    {
        set => base.Icon = new FontIcon() { Glyph = value };

    }

    public new IconElement Icon
    {
        set => base.Icon = value;
    }

    public int IconSize
    {
        set => this.Resources["NavigationViewItemOnLeftIconBoxHeight"] = value;
    }

    public new object Content
    {
        set
        {
            if (value is string text) base.Content = CoreTools.Translate(text);
            else base.Content = value;
        }
    }

    private PageType _pageType;
    public new PageType Tag
    {
        set => _pageType = value;
        get => _pageType;
    }

    public CustomNavViewItem()
    {
        Height = 60;
        IconSize = 28;
    }
}
