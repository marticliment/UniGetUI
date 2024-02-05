using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using ModernWindow.Data;
using ModernWindow.Essentials;
using ModernWindow.Interface;
using ModernWindow.Interface.Widgets;
using ModernWindow.PackageEngine;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.PointOfService.Provider;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI.Core;


namespace ModernWindow
{
    public sealed partial class MainWindow : Window
    {
        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
        [System.Runtime.InteropServices.InterfaceType(
            System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        interface IDataTransferManagerInterop
        {
            IntPtr GetForWindow([System.Runtime.InteropServices.In] IntPtr appWindow,
                [System.Runtime.InteropServices.In] ref Guid riid);
            void ShowShareUIForWindow(IntPtr appWindow);
        }

        TaskbarIcon TrayIcon;
        private bool RecentlyActivated = false;

        static readonly Guid _dtm_iid =
            new Guid(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);

        AppTools bindings = AppTools.Instance;
        public NavigationPage NavigationPage;
        public Grid ContentRoot;
        public bool BlockLoading = false;

        public List<NavButton> NavButtonList = new List<NavButton>();
        public MainWindow()
        {
            this.InitializeComponent();
            LoadTrayMenu();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(__content_root);
            ContentRoot = __content_root;
            ApplyTheme();
        }

        public void HandleClosingEvent(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
        {
            if (!bindings.GetSettings("DisableSystemTray"))
            {
                args.Cancel = true;
                RecentlyActivated = false;
                this.Hide();
            } else
            {
                //if (bindings.OperationQueue.Count > 0)
                    // TODO: Handle confirmation if ongoing operations
            }
        }

        private void LoadTrayMenu()
        {
            var TrayMenu = new MenuFlyout();

            var DiscoverPackages = new XamlUICommand();
            var AvailableUpdates = new XamlUICommand();
            var InstalledPackages = new XamlUICommand();
            var AboutWingetUI = new XamlUICommand();
            var ShowWingetUI = new XamlUICommand();
            var QuitWingetUI = new XamlUICommand();

            var Labels = new Dictionary<XamlUICommand, string>
            {
                { DiscoverPackages, "Discover Packages" },
                { AvailableUpdates, "Available Updates" },
                { InstalledPackages, "Installed Packages" },
                { AboutWingetUI, "WingetUI Version {0}" },
                { ShowWingetUI, "Show WingetUI" },
                { QuitWingetUI, "Quit" },
            };

            foreach (var item in Labels)
            {
                item.Key.Label = bindings.Translate(item.Value);
            }

            var Icons = new Dictionary<XamlUICommand, string>
            {
                { DiscoverPackages,  "\uF6FA"},
                { AvailableUpdates,  "\uE977"},
                { InstalledPackages,  "\uE895"},
                { AboutWingetUI,  "\uE946"},
                { ShowWingetUI,  "\uE8A7"},
                { QuitWingetUI,  "\uE711"},
            };

            foreach (var item in Icons)
            {
                item.Key.IconSource = new FontIconSource { Glyph = item.Value };
            }

            DiscoverPackages.ExecuteRequested += (s, e) => {NavigationPage.DiscoverNavButton.ForceClick(); Activate(); };
            AvailableUpdates.ExecuteRequested += (s, e) => {NavigationPage.UpdatesNavButton.ForceClick(); Activate(); };
            InstalledPackages.ExecuteRequested += (s, e) => {NavigationPage.InstalledNavButton.ForceClick(); Activate(); };
            AboutWingetUI.Label = bindings.Translate("WingetUI Version {0}").Replace("{0}", CoreData.VersionName);
            ShowWingetUI.ExecuteRequested += (s, e) => { Activate(); };
            QuitWingetUI.ExecuteRequested += (s, e) => { bindings.App.DisposeAndQuit(); };

            TrayMenu.Items.Add(new MenuFlyoutItem() { Command = DiscoverPackages });
            TrayMenu.Items.Add(new MenuFlyoutItem() { Command = AvailableUpdates });
            TrayMenu.Items.Add(new MenuFlyoutItem() { Command = InstalledPackages });
            TrayMenu.Items.Add(new MenuFlyoutSeparator());
            var _about = new MenuFlyoutItem() { Command = AboutWingetUI };
            _about.IsEnabled = false;
            TrayMenu.Items.Add(_about);
            TrayMenu.Items.Add(new MenuFlyoutSeparator());
            TrayMenu.Items.Add(new MenuFlyoutItem() { Command = ShowWingetUI });
            TrayMenu.Items.Add(new MenuFlyoutItem() { Command = QuitWingetUI });


            TrayMenu.AreOpenCloseAnimationsEnabled = false;

            TrayIcon = new TaskbarIcon();
            __content_root.Children.Add(TrayIcon);
            TrayIcon.ContextMenuMode = H.NotifyIcon.ContextMenuMode.PopupMenu;

            var ShowHideCommand = new XamlUICommand();
            ShowHideCommand.ExecuteRequested += async (s, e) =>
            {
                if (!RecentlyActivated)
                {
                    Activate();
                    RecentlyActivated = true;
                    await Task.Delay(5000);
                    RecentlyActivated = false;
                }
                else
                {
                    RecentlyActivated = false;
                    this.Hide();
                }
            };

            TrayIcon.LeftClickCommand = ShowHideCommand;
            TrayIcon.DoubleClickCommand = ShowHideCommand;
            TrayIcon.NoLeftClickDelay = true;
            TrayIcon.ContextFlyout = TrayMenu;
            UpdateSystemTrayStatus();
        }

        public void UpdateSystemTrayStatus()
        {
            string modifier = "_empty";
            string tooltip = bindings.Translate("Everything is up to date") + " - WingetUI";

            if(bindings.TooltipStatus.OperationsInProgress > 0)
            {
                modifier = "_blue";
                tooltip = bindings.Translate("Operation in progress") + " - WingetUI";
            }
            else if (bindings.TooltipStatus.ErrorsOccurred > 0)
            {
                modifier = "_orange";
                tooltip = bindings.Translate("Attention required") + " - WingetUI";
            }
            else if (bindings.TooltipStatus.RestartRequired)
            {
                modifier = "_turquoise";
                tooltip = bindings.Translate("Restart required") + " - WingetUI";
            }
            else if (bindings.TooltipStatus.AvailableUpdates > 0)
            {
                modifier = "_green";
                if(bindings.TooltipStatus.AvailableUpdates == 1)
                    tooltip = bindings.Translate("1 update is available") + " - WingetUI";
                else
                    tooltip = bindings.Translate("{0} updates are available").Replace("{0}", bindings.TooltipStatus.AvailableUpdates.ToString()) + " - WingetUI";
            }

            TrayIcon.ToolTipText = tooltip;

            var theme = ApplicationTheme.Light;
            string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            string RegistryValueName = "SystemUsesLightTheme";
            var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            var registryValueObject = key?.GetValue(RegistryValueName);
            if (registryValueObject != null)
            {   
                var registryValue = (int)registryValueObject;
                theme = registryValue > 0 ? ApplicationTheme.Light : ApplicationTheme.Dark;
            }
            if(theme == ApplicationTheme.Light)
                modifier += "_black";
            else
                modifier += "_white";
            

            var FullIconPath = Path.Join(Directory.GetParent(Assembly.GetEntryAssembly().Location).ToString(), "\\wingetui\\resources\\tray" + modifier + ".ico");

            TrayIcon.SetValue(TaskbarIcon.IconSourceProperty, new BitmapImage() { UriSource = new Uri(FullIconPath) });
        }


        public void SwitchToInterface()
        {
            SetTitleBar(__app_titlebar);
            ContentRoot = __content_root;


            NavigationPage = new NavigationPage();
            Grid.SetRow(NavigationPage, 1);
            Grid.SetColumn(NavigationPage, 0);
            MainContentGrid.Children.Add(NavigationPage);

            ColumnDefinition ContentColumn = __content_root.ColumnDefinitions[1];
            ContentColumn.Width = new GridLength(1, GridUnitType.Star);

            ColumnDefinition SpashScreenColumn = __content_root.ColumnDefinitions[0];
            SpashScreenColumn.Width = new GridLength(0, GridUnitType.Pixel);
        }

        public void ApplyTheme()
        { 
            string preferredTheme = bindings.GetSettingsValue("PreferredTheme");
            if (preferredTheme == "dark")
            {
                bindings.ThemeListener.CurrentTheme = ApplicationTheme.Dark;
                ContentRoot.RequestedTheme = ElementTheme.Dark;
            } 
            else if (preferredTheme == "light")
            {
                bindings.ThemeListener.CurrentTheme = ApplicationTheme.Light;
                ContentRoot.RequestedTheme = ElementTheme.Light;
            }
            else
            {
                if (ContentRoot.ActualTheme == ElementTheme.Dark)
                    bindings.ThemeListener.CurrentTheme = ApplicationTheme.Dark;
                else
                    bindings.ThemeListener.CurrentTheme = ApplicationTheme.Light;
                ContentRoot.RequestedTheme = ElementTheme.Default;
            }

        }

        public void SharePackage(Package package)
        {
            if (package == null)
                return;
            
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            IDataTransferManagerInterop interop =
            Windows.ApplicationModel.DataTransfer.DataTransferManager.As
                <IDataTransferManagerInterop>();

            IntPtr result = interop.GetForWindow(hWnd, _dtm_iid);
            var dataTransferManager = WinRT.MarshalInterface
                <Windows.ApplicationModel.DataTransfer.DataTransferManager>.FromAbi(result);

            dataTransferManager.DataRequested += (sender, args) =>
            {
                DataRequest dataPackage = args.Request;
                var ShareUrl = new Uri("https://marticliment.com/wingetui/share?pid=" + System.Web.HttpUtility.UrlEncode(package.Id) + "&pname=" + System.Web.HttpUtility.UrlEncode(package.Name) + "&psource=" + System.Web.HttpUtility.UrlEncode(package.Source.ToString()));
                dataPackage.Data.SetWebLink(ShareUrl);
                dataPackage.Data.Properties.Title = "Sharing " + package.Name;
                dataPackage.Data.Properties.ApplicationName = "WingetUI";
                dataPackage.Data.Properties.ContentSourceWebLink = ShareUrl;
                dataPackage.Data.Properties.Description = "Share " + package.Name + " with your friends";
                dataPackage.Data.Properties.PackageFamilyName = "WingetUI";
            };

            interop.ShowShareUIForWindow(hWnd);

        }

        public IntPtr GetWindowHandle()
        {
            return WinRT.Interop.WindowNative.GetWindowHandle(this);
        }


    }
}
