using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using UniGetUI.Core.Tools;
using UniGetUI.Interface;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine.Managers.WingetManager;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.UI.WebUI;
using YamlDotNet.Core.Tokens;
using static System.Net.Mime.MediaTypeNames;

namespace UniGetUI.Controls;
internal class CustomNavViewItem : NavigationViewItem
{
    int _iconSize = 28;
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

    public bool IsLoading
    {
        set
        {
            base.Icon.Margin = new Thickness(value ? 6 : 0);
            _progressRing.Visibility = value? Visibility.Visible: Visibility.Collapsed;
        }
    }

    public int IconSize
    {
        set => Resources["NavigationViewItemOnLeftIconBoxHeight"] = _iconSize = value;
    }

    public string Text
    {
        set => _textBlock.Text = CoreTools.Translate(value);
    }

    private readonly TextBlock _textBlock;
    private readonly ProgressRing _progressRing;

    private PageType _pageType;
    public new PageType Tag
    {
        set => _pageType = value;
        get => _pageType;
    }

    public CustomNavViewItem()
    {
        Height = 60;
        Resources["NavigationViewItemOnLeftIconBoxHeight"] = _iconSize;
        Resources["NavigationViewItemContentPresenterMargin"] = new Thickness(0);

        var grid = new Grid { Height = 50 };

        _progressRing = new ProgressRing
        {
            Margin = new Thickness(-46, 0, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            IsIndeterminate = true,
            Visibility = Visibility.Collapsed,
        };

        _textBlock = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
        };

        grid.Children.Add(_progressRing);
        grid.Children.Add(_textBlock);
        base.Content = grid;
    }
}
