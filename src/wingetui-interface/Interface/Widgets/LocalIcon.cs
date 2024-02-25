using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using ModernWindow.Structures;
using System;

namespace ModernWindow.Interface.Widgets
{
    public class LocalIcon : ImageIcon
    {
        public static AppTools Tools = AppTools.Instance;

        private string __icon_name;
        public string IconName
        {
            get { return __icon_name; }
            set { __icon_name = value; __apply_icon(); ActualThemeChanged += (s, e) => { __apply_icon(); }; }
        }

        public LocalIcon()
        {
        }

        public LocalIcon(string iconName)
        {
            __icon_name = iconName;
            __apply_icon();
            ActualThemeChanged += (s, e) => { __apply_icon(); };
        }

        public void __apply_icon()
        {
            string theme = "white";
            if (ActualTheme == ElementTheme.Light)
            {
                theme = "black";
            }

            if (Source == null)
                Source = new BitmapImage();
            (Source as BitmapImage).UriSource = new Uri("ms-appx:///Assets/Images/" + IconName + "_" + theme + ".png");
        }
    }
}
