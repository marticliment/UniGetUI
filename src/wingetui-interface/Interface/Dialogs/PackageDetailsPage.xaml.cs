using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using ModernWindow.PackageEngine;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PackageDetailsPage : Page
    {
        public AppTools bindings = AppTools.Instance;
        public Package Package;
        private InstallOptionsPage InstallOptionsPage;
        public event EventHandler Close;

        private enum LayoutMode
        {
            Normal,
            Wide
        }
        
        private LayoutMode? layoutMode;
        public PackageDetailsPage(Package package, string MainButtonText)
        {
            this.InitializeComponent();

            InstallOptionsPage = new InstallOptionsPage(package);
            InstallOptionsPanel.Content = InstallOptionsPage;

            SizeChanged += PackageDetailsPage_SizeChanged;
        }

        public void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            Close?.Invoke(this, new EventArgs());
            //InstallOptionsPanel.Save
            // Install Action 
        }

        public void ShareButton_Click(object sender, RoutedEventArgs e)
        {
             bindings.App.mainWindow.SharePackage(Package);
        }

        public void DownloadInstallerButton_Click(object sender, RoutedEventArgs e)
        {
            //
        }
        public void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close?.Invoke(this, new EventArgs());
        }

        public void PackageDetailsPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width < 800)
            {
                if(layoutMode != LayoutMode.Normal)
                {
                    layoutMode = LayoutMode.Normal;
                 
                    MainGrid.ColumnDefinitions.Clear();
                    MainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                    Grid.SetColumn(TitlePanel, 0);
                    Grid.SetColumn(BasicInfoPanel, 0);
                    Grid.SetColumn(ScreenshotsPanel, 0);
                    Grid.SetColumn(ActionsPanel, 0);
                    Grid.SetColumn(InstallOptionsPanel, 0);
                    Grid.SetColumn(MoreDataStackPanel, 0);
                
                    MainGrid.RowDefinitions.Clear();
                    MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    Grid.SetRow(TitlePanel, 0);
                    Grid.SetRow(DescriptionPanel, 1);
                    Grid.SetRow(BasicInfoPanel, 2);
                    Grid.SetRow(ScreenshotsPanel, 3);
                    Grid.SetRow(ActionsPanel, 4);
                    Grid.SetRow(InstallOptionsPanel, 5);
                    Grid.SetRow(MoreDataStackPanel, 6);

                    LeftPanel.Children.Clear();
                    RightPanel.Children.Clear();
                    MainGrid.Children.Clear();
                    MainGrid.Children.Add(TitlePanel);
                    MainGrid.Children.Add(DescriptionPanel);
                    MainGrid.Children.Add(BasicInfoPanel);
                    MainGrid.Children.Add(ScreenshotsPanel);
                    MainGrid.Children.Add(ActionsPanel);
                    MainGrid.Children.Add(InstallOptionsPanel);
                    MainGrid.Children.Add(MoreDataStackPanel);

                    InstallOptionsPanel.IsExpanded = false;

                }
            }
            else
            {
                if (layoutMode != LayoutMode.Wide)
                {
                    layoutMode = LayoutMode.Wide;

                    MainGrid.ColumnDefinitions.Clear();
                    MainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                    MainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                    Grid.SetColumn(LeftPanel, 0);
                    Grid.SetColumn(RightPanel, 1);
                    Grid.SetColumn(TitlePanel, 0);
                    Grid.SetColumnSpan(TitlePanel, 2);

                    MainGrid.RowDefinitions.Clear();
                    MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    Grid.SetRow(LeftPanel, 1);
                    Grid.SetRow(RightPanel, 1);
                    Grid.SetRow(TitlePanel, 0);

                    LeftPanel.Children.Clear();
                    RightPanel.Children.Clear();
                    MainGrid.Children.Clear();
                    LeftPanel.Children.Add(DescriptionPanel);
                    LeftPanel.Children.Add(BasicInfoPanel);
                    RightPanel.Children.Add(ScreenshotsPanel);
                    LeftPanel.Children.Add(ActionsPanel);
                    LeftPanel.Children.Add(InstallOptionsPanel);
                    RightPanel.Children.Add(MoreDataStackPanel);

                    InstallOptionsPanel.IsExpanded = true;

                    MainGrid.Children.Add(LeftPanel);
                    MainGrid.Children.Add(RightPanel);
                    MainGrid.Children.Add(TitlePanel);

                }
            }
        }
    }
}
