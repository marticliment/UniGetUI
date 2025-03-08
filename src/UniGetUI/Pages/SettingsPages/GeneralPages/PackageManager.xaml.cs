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
using Microsoft.VisualBasic;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Interfaces;
using System.Diagnostics;
using UniGetUI.PackageEngine.Managers.WingetManager;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Managers.ChocolateyManager;
using UniGetUI.PackageEngine.Managers.ScoopManager;
using UniGetUI.PackageEngine.Managers.NpmManager;
using UniGetUI.PackageEngine.Managers.PipManager;
using UniGetUI.PackageEngine.Managers.PowerShellManager;
using UniGetUI.PackageEngine.Managers.PowerShell7Manager;
using UniGetUI.PackageEngine.Managers.CargoManager;
using UniGetUI.PackageEngine.Managers.VcpkgManager;
using UniGetUI.PackageEngine.Managers.DotNetManager;
using ExternalLibraries.Clipboard;
using CommunityToolkit.WinUI.Controls;
using System.Reflection;
using UniGetUI.Interface.Widgets;
using UniGetUI.Core.Data;
using UniGetUI.Pages.DialogPages;
using UniGetUI.Core.SettingsEngine;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Pages.SettingsPages.GeneralPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PackageManagerPage : Page, ISettingsPage
    {
        IPackageManager? Manager;
        public event EventHandler? RestartRequired;
        public event EventHandler<Type>? NavigationRequested;
        public bool CanGoBack => true;
        public string ShortTitle => Manager is null? "Ligma": CoreTools.Translate("{0} Settings", Manager.DisplayName);

        public PackageManagerPage()
        {
            this.InitializeComponent();
            EnableManager.StateChanged += (_, _) => SetManagerStatus();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is not Type Manager_T) throw new InvalidDataException("The passed parameter was not a type");
            // Can't do switch with types
            Manager = null;
            if (Manager_T == typeof(WinGet)) Manager = PEInterface.WinGet;
            else if (Manager_T == typeof(Chocolatey)) Manager = PEInterface.Chocolatey;
            else if (Manager_T == typeof(Scoop)) Manager = PEInterface.Scoop;
            else if (Manager_T == typeof(Npm)) Manager = PEInterface.Npm;
            else if (Manager_T == typeof(Pip)) Manager = PEInterface.Pip;
            else if (Manager_T == typeof(PowerShell)) Manager = PEInterface.PowerShell;
            else if (Manager_T == typeof(PowerShell7)) Manager = PEInterface.PowerShell7;
            else if (Manager_T == typeof(Cargo)) Manager = PEInterface.Cargo;
            else if (Manager_T == typeof(Vcpkg)) Manager = PEInterface.Vcpkg;
            else if (Manager_T == typeof(DotNet)) Manager = PEInterface.DotNet;
            else throw new InvalidCastException("The specified type was not a package manager!");

            LongVersionTextBlock.Text = Manager.Status.Version + "\n";
            SetManagerStatus(false);

            LocationLabel.Text = Manager.Status.ExecutablePath;
            if (LocationLabel.Text == "") LocationLabel.Text = CoreTools.Translate("The executable file for {0} was not found", Manager.DisplayName);
            EnableManager.SettingName = Manager.Name;
            EnableManager.Text = CoreTools.Translate("Enable {0}", Manager.DisplayName);


            var AlwaysElevateManagerOP = new CheckboxCard()
            {
                Text = CoreTools.AutoTranslated("Always run {pm} operations with administrator rights").Replace("{pm}", Manager.DisplayName),
                SettingName = "AlwaysElevate" + Manager.Name,
                CornerRadius = new CornerRadius(8)
            };

            ManagerLogsLabel.Text = CoreTools.Translate("View {0} logs", Manager.DisplayName);

            // ----------------------- SOURCES CONTROL -------------------

            ExtraControls.Children.Clear();

            if(Manager.Capabilities.SupportsCustomSources && Manager is not Vcpkg)
            {
                SettingsCard SourceManagerCard = new() {
                    Resources = { ["SettingsCardLeftIndention"] = 10 },
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0,0,0,16)
                };
                var man = new SourceManager(Manager);
                SourceManagerCard.Description = man;
                ExtraControls.Children.Add(SourceManagerCard);
            }

            // ------------------------- WINGET EXTRA SETTINGS -----------------------

            if (Manager is WinGet)
            {
                AlwaysElevateManagerOP.CornerRadius = new CornerRadius(8, 8, 0, 0);
                AlwaysElevateManagerOP.BorderThickness = new Thickness(1, 1, 1, 0);
                ExtraControls.Children.Add(AlwaysElevateManagerOP);

                ButtonCard WinGet_ResetWindowsIPackageManager = new()
                {
                    Text = CoreTools.AutoTranslated("Reset WinGet") +
                           $" ({CoreTools.Translate("This may help if no packages are listed")})",
                    ButtonText = CoreTools.AutoTranslated("Reset"),
                    CornerRadius = new CornerRadius(0)
                };
                WinGet_ResetWindowsIPackageManager.Click += (_, _) => { DialogHelper.HandleBrokenWinGet(); };
                ExtraControls.Children.Add(WinGet_ResetWindowsIPackageManager);

                CheckboxCard WinGet_UseBundled = new()
                {
                    Text =
                        $"{CoreTools.Translate("Use bundled WinGet instead of system WinGet")} ({CoreTools.Translate("This may help if WinGet packages are not shown")})",
                    SettingName = "ForceLegacyBundledWinGet",
                    CornerRadius = new CornerRadius(0),
                    BorderThickness = new Thickness(1, 0, 1, 0),
                };
                WinGet_UseBundled.StateChanged += (_, _) => RestartRequired?.Invoke(this, new());
                ExtraControls.Children.Add(WinGet_UseBundled);

                CheckboxCard WinGet_EnableTroubleshooter = new()
                {
                    Text = CoreTools.Translate("Enable the automatic WinGet troubleshooter"),
                    SettingName = "DisableWinGetMalfunctionDetector",
                    CornerRadius = new CornerRadius(0),
                };
                WinGet_EnableTroubleshooter.StateChanged += (_, _) =>
                {
                    MainApp.Instance.MainWindow.WinGetWarningBanner.IsOpen = false;
                    _ = PEInterface.InstalledPackagesLoader.ReloadPackages();
                };
                ExtraControls.Children.Add(WinGet_EnableTroubleshooter);

                CheckboxCard WinGet_EnableTroubleshooter_v2 = new()
                {
                    Text = CoreTools.Translate("Enable an [experimental] improved WinGet troubleshooter"),
                    SettingName = "EnableNewWinGetTroubleshooter",
                    CornerRadius = new CornerRadius(0),
                    BorderThickness = new Thickness(1,0,1,0),
                };
                WinGet_EnableTroubleshooter_v2.StateChanged += (_, _) =>
                {
                    MainApp.Instance.MainWindow.WinGetWarningBanner.IsOpen = false;
                    _ = PEInterface.InstalledPackagesLoader.ReloadPackages();
                };
                ExtraControls.Children.Add(WinGet_EnableTroubleshooter_v2);

                CheckboxCard WinGet_HideNonApplicableUpdates = new()
                {
                    Text = CoreTools.Translate("Add updates that fail with a 'no applicable update found' to the ignored updates list"),
                    SettingName = "IgnoreUpdatesNotApplicable",
                    CornerRadius = new CornerRadius(0,0,8,8)
                };
                ExtraControls.Children.Add(WinGet_HideNonApplicableUpdates);
            }

            // ---------------------------- SCOOP EXTRA SETTINGS -------------------------

            else if (Manager is Scoop)
            {
                AlwaysElevateManagerOP.CornerRadius = new CornerRadius(8, 8, 0, 0);
                AlwaysElevateManagerOP.BorderThickness = new Thickness(1, 1, 1, 0);
                ExtraControls.Children.Add(AlwaysElevateManagerOP);

                ButtonCard Scoop_Install = new()
                {
                    Text = CoreTools.AutoTranslated("Install Scoop"),
                    ButtonText = CoreTools.AutoTranslated("Install"),
                    CornerRadius = new CornerRadius(0)
                };
                Scoop_Install.Click += (_, _) =>
                {
                    CoreTools.LaunchBatchFile(
                        Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "install_scoop.cmd"),
                        CoreTools.Translate("Scoop Installer - WingetUI"));
                    RestartRequired?.Invoke(this, new());
                };
                ExtraControls.Children.Add(Scoop_Install);

                ButtonCard Scoop_Uninstall = new()
                {
                    Text = CoreTools.AutoTranslated("Uninstall Scoop (and its packages)"),
                    ButtonText = CoreTools.AutoTranslated("Uninstall"),
                    CornerRadius = new CornerRadius(0),
                    BorderThickness = new Thickness(1,0,1,0)
                };
                Scoop_Uninstall.Click += (_, _) =>
                {
                    CoreTools.LaunchBatchFile(
                        Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "uninstall_scoop.cmd"),
                        CoreTools.Translate("Scoop Uninstaller - WingetUI"));
                    RestartRequired?.Invoke(this, new());
                };
                ExtraControls.Children.Add(Scoop_Uninstall);

                ButtonCard Scoop_ResetAppCache = new()
                {
                    Text = CoreTools.AutoTranslated("Run cleanup and clear cache"),
                    ButtonText = CoreTools.AutoTranslated("Run"),
                    CornerRadius = new CornerRadius(0, 0, 8, 8),
                };
                Scoop_ResetAppCache.Click += (_, _) =>
                {
                    CoreTools.LaunchBatchFile(
                        Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "scoop_cleanup.cmd"),
                        CoreTools.Translate("Clearing Scoop cache - WingetUI"), RunAsAdmin: true);
                };
                ExtraControls.Children.Add(Scoop_ResetAppCache);


            }
            // ----------------------------- CHOCO EXTRA SETTINGS ------------------------------

            else if (Manager is Chocolatey)
            {
                AlwaysElevateManagerOP.CornerRadius = new CornerRadius(8, 8, 0, 0);
                AlwaysElevateManagerOP.BorderThickness = new Thickness(1, 1, 1, 0);
                ExtraControls.Children.Add(AlwaysElevateManagerOP);

                CheckboxCard Chocolatey_SystemChoco = new()
                {
                    Text = CoreTools.AutoTranslated("Use system Chocolatey"),
                    SettingName = "UseSystemChocolatey",
                    CornerRadius = new CornerRadius(0,0,8,8)
                };
                Chocolatey_SystemChoco.StateChanged += (_, _) => RestartRequired?.Invoke(this, new());
                ExtraControls.Children.Add(Chocolatey_SystemChoco);
            }

            // -------------------------------- VCPKG EXTRA SETTINGS --------------------------------------

            else if (Manager is Vcpkg)
            {
                AlwaysElevateManagerOP.CornerRadius = new CornerRadius(8, 8, 0, 0);
                AlwaysElevateManagerOP.BorderThickness = new Thickness(1, 1, 1, 0);
                ExtraControls.Children.Add(AlwaysElevateManagerOP);

                Settings.SetValue("DefaultVcpkgTriplet", Vcpkg.GetDefaultTriplet());
                ComboboxCard Vcpkg_DefaultTriplet = new()
                {
                    Text = CoreTools.Translate("Default vcpkg triplet"),
                    SettingName = "DefaultVcpkgTriplet",
                    CornerRadius = new CornerRadius(0)
                };
                foreach (string triplet in Vcpkg.GetSystemTriplets())
                {
                    Vcpkg_DefaultTriplet.AddItem(triplet, triplet);
                }

                Vcpkg_DefaultTriplet.ShowAddedItems();
                ExtraControls.Children.Add(Vcpkg_DefaultTriplet);

                ButtonCard Vcpkg_CustomVcpkgRoot = new()
                {
                    Text = "Change vcpkg root location",
                    ButtonText = "Select",
                    CornerRadius = new CornerRadius(0,0,8,8),
                    BorderThickness = new Thickness(1,0,1,1)
                };
                StackPanel p = new() { Orientation = Orientation.Horizontal, Spacing = 5, };
                var VcPkgRootLabel = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
                var ResetVcPkgRootLabel = new HyperlinkButton { Content = CoreTools.Translate("Reset") };
                var OpenVcPkgRootLabel = new HyperlinkButton { Content = CoreTools.Translate("Open") };

                VcPkgRootLabel.Text = Settings.Get("CustomVcpkgRoot")
                    ? Settings.GetValue("CustomVcpkgRoot")
                    : "%VCPKG_ROOT%";
                OpenVcPkgRootLabel.IsEnabled = Settings.Get("CustomVcpkgRoot");
                ResetVcPkgRootLabel.IsEnabled = Settings.Get("CustomVcpkgRoot");

                ResetVcPkgRootLabel.Click += (_, _) =>
                {
                    VcPkgRootLabel.Text = "%VCPKG_ROOT%";
                    Settings.Set("CustomVcpkgRoot", false);
                    ResetVcPkgRootLabel.IsEnabled = false;
                    OpenVcPkgRootLabel.IsEnabled = false;
                };

                OpenVcPkgRootLabel.Click += (_, _) =>
                {
                    string directory = Settings.GetValue("CustomVcpkgRoot").Replace("/", "\\");
                    if (directory.Any()) Process.Start("explorer.exe", directory);
                };

                Vcpkg_CustomVcpkgRoot.Click += (_, _) =>
                {
                    ExternalLibraries.Pickers.FolderPicker openPicker =
                        new(MainApp.Instance.MainWindow.GetWindowHandle());
                    string folder = openPicker.Show();
                    if (folder != string.Empty)
                    {
                        Settings.SetValue("CustomVcpkgRoot", folder);
                        VcPkgRootLabel.Text = folder;
                        ResetVcPkgRootLabel.IsEnabled = true;
                        OpenVcPkgRootLabel.IsEnabled = true;
                    }
                };

                p.Children.Add(VcPkgRootLabel);
                p.Children.Add(ResetVcPkgRootLabel);
                p.Children.Add(OpenVcPkgRootLabel);
                Vcpkg_CustomVcpkgRoot.Description = p;
                Vcpkg_CustomVcpkgRoot.Click += (_, _) => RestartRequired?.Invoke(this, new());
                ExtraControls.Children.Add(Vcpkg_CustomVcpkgRoot);
            }

            // -------------------------------- DEFAULT EXTRA SETTINGS --------------------------------------
            else
            {
                AlwaysElevateManagerOP.CornerRadius = new CornerRadius(8);
                AlwaysElevateManagerOP.BorderThickness = new Thickness(1);
                ExtraControls.Children.Add(AlwaysElevateManagerOP);
            }

                base.OnNavigatedTo(e);
        }

        private void ShowVersionHyperlink_Click(object sender, RoutedEventArgs e)
            => SetManagerStatus(true);

        void SetManagerStatus(bool ShowVersion = false)
        {
            if (Manager is null) throw new InvalidDataException();

            ShowVersionHyperlink.Visibility = Visibility.Collapsed;
            LongVersionTextBlock.Visibility = Visibility.Collapsed;
            if (Manager.IsEnabled() && Manager.Status.Found)
            {
                ManagerStatusBar.Severity = InfoBarSeverity.Success;
                ManagerStatusBar.Title = CoreTools.Translate("{pm} is enabled and ready to go", new Dictionary<string, object?> { { "pm", Manager.DisplayName } });
                if (!Manager.Status.Version.Contains('\n'))
                {
                    ManagerStatusBar.Message =
                        CoreTools.Translate("{pm} version:", new Dictionary<string, object?> {{ "pm", Manager.DisplayName }}) + $" {Manager.Status.Version}";
                }
                else if (ShowVersion)
                {
                    ManagerStatusBar.Message = CoreTools.Translate("{pm} version:",
                        new Dictionary<string, object?> { { "pm", Manager.DisplayName } });
                    LongVersionTextBlock.Visibility = Visibility.Visible;
                }
                else
                {
                    ManagerStatusBar.Message = "";
                    ShowVersionHyperlink.Visibility = Visibility.Visible;
                }
            }
            else if (Manager.IsEnabled() && !Manager.Status.Found)
            {
                ManagerStatusBar.Severity = InfoBarSeverity.Error;
                ManagerStatusBar.Title = CoreTools.Translate("{pm} was not found!",
                    new Dictionary<string, object?> { { "pm", Manager.DisplayName } });
                ManagerStatusBar.Message = CoreTools.Translate(
                    "You may need to install {pm} in order to use it with WingetUI.",
                    new Dictionary<string, object?> { { "pm", Manager.DisplayName } });
            }
            else if (!Manager.IsEnabled())
            {
                ManagerStatusBar.Severity = InfoBarSeverity.Informational;
                ManagerStatusBar.Title = CoreTools.Translate("{pm} is disabled",
                    new Dictionary<string, object?> { { "pm", Manager.DisplayName } });
                ManagerStatusBar.Message = CoreTools.Translate("Enable it to install packages from {pm}.",
                    new Dictionary<string, object?> { { "pm", Manager.DisplayName } });
            }
        }

        private async void ManagerPath_Click(object sender, RoutedEventArgs e)
        {
            WindowsClipboard.SetText(LocationLabel.Text);
            CopyButtonIcon.Symbol = Symbol.Accept;
            await Task.Delay(1000);
            CopyButtonIcon.Symbol = Symbol.Copy;
        }

        private void ManagerLogs_Click(object sender, RoutedEventArgs e)
        {
            MainApp.Instance.MainWindow.NavigationPage.OpenManagerLogs(Manager as IPackageManager);
        }
    }
}
