using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Widgets;
using UniGetUI.Pages.SettingsPages.GeneralPages;
using UniGetUI.PackageEngine;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Pages.SettingsPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsHomepage : Page, ISettingsPage
    {
        public bool CanGoBack => false;
        public string ShortTitle => CoreTools.Translate("WingetUI Settings");

        public event EventHandler? RestartRequired;

        public event EventHandler<Type>? NavigationRequested;

        public SettingsHomepage()
        {
            this.InitializeComponent();

            foreach(var manager in PEInterface.Managers)
            {
                var button = new SettingsPageButton()
                {
                    Text = manager.DisplayName,
                    Description = manager.Properties.Description.Replace("<br>", "\n").Replace("<b>", "").Replace("</b>", ""),
                    HeaderIcon = new LocalIcon(manager.Properties.IconId)
                };
                button.Click += (_, _) => NavigationRequested?.Invoke(this, manager.GetType());

                SettingsEntries.Children.Add(button);
                SettingsEntries.Children.Add(new UserControl() { Height = 16 });
            }
        }

        public void Administrator(object s, RoutedEventArgs e) => NavigationRequested?.Invoke(this, typeof(Administrator));
        public void Backup(object s, RoutedEventArgs e) => NavigationRequested?.Invoke(this, typeof(Backup));
        public void Experimental(object s, RoutedEventArgs e) => NavigationRequested?.Invoke(this, typeof(Experimental));
        public void General(object s, RoutedEventArgs e) => NavigationRequested?.Invoke(this, typeof(General));
        public void Interface(object s, RoutedEventArgs e) => NavigationRequested?.Invoke(this, typeof(Interface_P));
        public void Notifications(object s, RoutedEventArgs e) => NavigationRequested?.Invoke(this, typeof(Notifications));
        public void Operations(object s, RoutedEventArgs e) => NavigationRequested?.Invoke(this, typeof(Operations));
        public void Startup(object s, RoutedEventArgs e) => NavigationRequested?.Invoke(this, typeof(Startup));

    }
}
