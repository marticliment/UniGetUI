using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;
using UniGetUI.Interface;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Widgets;
using Windows.Devices.Bluetooth.Advertisement;

namespace UniGetUI.Controls;
internal class CustomNavViewItem: NavigationViewItem
{
    public IconType LocalIcon
    {
        set => base.Icon = new LocalIcon(value);
    }
    public string GlyphIcon
    {
        set => base.Icon = new FontIcon() { Glyph = value };
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
}
