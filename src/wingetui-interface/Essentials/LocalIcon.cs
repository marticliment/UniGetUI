using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace ModernWindow.Interface.Widgets
{
    public class LocalIcon : ImageIcon
    {
        public static MainAppBindings bindings = MainAppBindings.Instance;

        private string __icon_name;

        public string IconName { 
            get { return __icon_name; }
            set { __icon_name = value; __apply_icon(); (Parent as FrameworkElement).ActualThemeChanged += (s, e) => { __apply_icon(); };  }

        
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
            var theme = "white";
            if(ActualTheme == ElementTheme.Light)
            {
                theme = "black";
            }

            if (Source == null)
                Source = new BitmapImage();
            (Source as BitmapImage).UriSource = new Uri("ms-appx:///wingetui/resources/" + IconName + "_" + theme + ".png");
        }
    }
}
