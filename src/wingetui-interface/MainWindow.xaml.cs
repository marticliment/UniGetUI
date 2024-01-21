using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using ModernWindow.SettingsTab;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;


namespace ModernWindow
{
    public sealed partial class MainWindow : Window
    {
        MainApp _app = Application.Current as MainApp;
        public SettingsTab.SettingsInterface SettingsTab;
        public Grid ContentRoot;
        public bool BlockLoading = false;
        public MainWindow()
        {
            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(__content_root);
            ContentRoot = __content_root;
            ApplyTheme();
            LoadComponents();

        }

        private async void LoadComponents()
        {
            await Task.Delay(1000);
            SwitchToInterface();
        }


        public void SwitchToInterface()
        {
            SetTitleBar(__app_titlebar);
            ContentRoot = __content_root;


            SettingsTab = new SettingsInterface();
            MainNavigationFrame.Children.Add(SettingsTab);

            ColumnDefinition ContentColumn = __content_root.ColumnDefinitions[1];
            ContentColumn.Width = new GridLength(1, GridUnitType.Star);

            ColumnDefinition SpashScreenColumn = __content_root.ColumnDefinitions[0];
            SpashScreenColumn.Width = new GridLength(0, GridUnitType.Pixel);
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
