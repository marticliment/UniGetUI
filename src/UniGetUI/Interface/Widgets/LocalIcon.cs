using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace UniGetUI.Interface.Widgets
{
    public class LocalIcon : ImageIcon
    {
        private bool __registered_theme_event = false;
        public DependencyProperty IconNameProperty;

        private static readonly Dictionary<string, BitmapImage> bitmap_cache = [];

        public string IconName
        {
            get => (string)GetValue(IconNameProperty);
            set => SetValue(IconNameProperty, value);
        }

        public LocalIcon()
        {
            IconNameProperty = DependencyProperty.Register(
            nameof(IconName),
            typeof(string),
            typeof(ButtonCard),
            new PropertyMetadata(default(string), new PropertyChangedCallback((d, e) =>
            {
                IconName = (string)e.NewValue;
                __apply_icon();
                if (!__registered_theme_event)
                {
                    ActualThemeChanged += (s, e) => { __apply_icon(); };
                    __registered_theme_event = true;
                }
            })));
        }

        public LocalIcon(string iconName) : this()
        {
            IconName = iconName;
            __apply_icon();

            if (!__registered_theme_event)
            {
                ActualThemeChanged += (s, e) => { __apply_icon(); };
                __registered_theme_event = true;
            }
        }

        public void __apply_icon()
        {
            string theme = "white";
            if (ActualTheme == ElementTheme.Light)
            {
                theme = "black";
            }

            string image_file = $"{IconName}_{theme}.png";
            if (bitmap_cache.TryGetValue(image_file, out BitmapImage? recycled_image) && recycled_image != null)
            {
                Source = recycled_image;
            }
            else
            {
                BitmapImage image = new()
                {
                    UriSource = new Uri($"ms-appx:///Assets/Images/{image_file}")
                };
                bitmap_cache.Add(image_file, image);
                Source = image;
            }
        }
    }
}
