using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;
using UniGetUI.Interface;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Widgets;

namespace UniGetUI.Controls;
internal partial class CustomNavViewItem : NavigationViewItem
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
            if (value is string text) base.Content = new TextBlock()
            {
                Text = CoreTools.Translate(text).Trim('\n').Trim(' '),
                TextWrapping = TextWrapping.WrapWholeWords,
                VerticalAlignment = VerticalAlignment.Center,
            };
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
