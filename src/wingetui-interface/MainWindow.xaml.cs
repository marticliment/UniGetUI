using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        MainApp _app = Application.Current as MainApp;
        public SettingsTab.MainPage SettingsTab;
        public ScrollView ContentRoot;
        public MainWindow()
        {
            this.InitializeComponent();
            SettingsTab = __settings_tab;
            ContentRoot = __content_root;
            ContentRoot.VerticalScrollRailMode = ScrollingRailMode.Enabled;
            ApplyTheme();
        }


        public void ApplyTheme()
        { 
            string preferredTheme = _app.GetSettingsValue("PreferredTheme");
            if (preferredTheme == "dark")
                ContentRoot.RequestedTheme = ElementTheme.Dark;
            else if (preferredTheme == "light")
                ContentRoot.RequestedTheme = ElementTheme.Light;
            else
                ContentRoot.RequestedTheme = ElementTheme.Default;

        }
    }
}
