using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UniGetUI.Core.Tools;
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
        public event EventHandler? ReapplyProperties;
        public bool CanGoBack => true;
        public string ShortTitle => Manager is null? "": CoreTools.Translate("{0} settings", Manager.DisplayName);

        public PackageManagerPage()
        {
            this.InitializeComponent();
            EnableManager.StateChanged += (_, _) => SetManagerStatus();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Manager = null;
            if (e.Parameter is not Type Manager_T) throw new InvalidDataException("The passed parameter was not a type");
            // Can't do switch with types
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

            ReapplyProperties?.Invoke(this, new());

            LongVersionTextBlock.Text = Manager.Status.Version + "\n";
            SetManagerStatus(false);

            LocationLabel.Text = Manager.Status.ExecutablePath + Manager.Properties.ExecutableCallArgs;
            if (LocationLabel.Text == "") LocationLabel.Text = CoreTools.Translate("The executable file for {0} was not found", Manager.DisplayName);
            EnableManager.KeyName = Manager.Name;
            EnableManager.Text = CoreTools.Translate("Enable {pm}").Replace("{pm}", Manager.DisplayName);
            InstallOptionsTitle.Text = CoreTools.Translate("Default installation options for {0} packages", Manager.DisplayName);

            SettingsTitle.Text = CoreTools.Translate("{0} settings", Manager.DisplayName);
            StatusTitle.Text = CoreTools.Translate("{0} status", Manager.DisplayName);

            var DisableNotifsCard = new CheckboxCard_Dict()
            {
                Text = CoreTools.Translate("Ignore packages from {pm} when showing a notification about updates").Replace("{pm}", Manager.DisplayName),
                DictionaryName = Settings.K.DisabledPackageManagerNotifications,
                ForceInversion = true,
                KeyName = Manager.Name
            };

            ManagerLogsLabel.Text = CoreTools.Translate("View {0} logs", Manager.DisplayName);

            InstallOptionsPanel.Description = new InstallOptions_Manager(Manager);

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
                DisableNotifsCard.CornerRadius = new CornerRadius(8, 8, 0, 0);
                DisableNotifsCard.BorderThickness = new Thickness(1, 1, 1, 0);
                ExtraControls.Children.Add(DisableNotifsCard);

                ButtonCard WinGet_ResetWindowsIPackageManager = new()
                {
                    Text = CoreTools.Translate("Reset WinGet") +
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
                    SettingName = Settings.K.ForceLegacyBundledWinGet,
                    CornerRadius = new CornerRadius(0),
                    BorderThickness = new Thickness(1, 0, 1, 0),
                };
                WinGet_UseBundled.StateChanged += (_, _) => RestartRequired?.Invoke(this, new());
                ExtraControls.Children.Add(WinGet_UseBundled);

                CheckboxCard WinGet_EnableTroubleshooter = new()
                {
                    Text = CoreTools.Translate("Enable the automatic WinGet troubleshooter"),
                    SettingName = Settings.K.DisableWinGetMalfunctionDetector,
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
                    SettingName = Settings.K.DisableNewWinGetTroubleshooter,
                    CornerRadius = new CornerRadius(0),
                    BorderThickness = new Thickness(1, 0, 1, 0),
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
                    SettingName = Settings.K.IgnoreUpdatesNotApplicable,
                    CornerRadius = new CornerRadius(0, 0, 8, 8)
                };
                ExtraControls.Children.Add(WinGet_HideNonApplicableUpdates);
            }

            // ---------------------------- SCOOP EXTRA SETTINGS -------------------------

            else if (Manager is Scoop)
            {
                DisableNotifsCard.CornerRadius = new CornerRadius(8,8,0,0);
                DisableNotifsCard.BorderThickness = new Thickness(1, 1, 1, 0);
                ExtraControls.Children.Add(DisableNotifsCard);

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
                    BorderThickness = new Thickness(1, 0, 1, 0)
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
                    CornerRadius = new CornerRadius(0),
                };
                Scoop_ResetAppCache.Click += (_, _) =>
                {
                    CoreTools.LaunchBatchFile(
                        Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "scoop_cleanup.cmd"),
                        CoreTools.Translate("Clearing Scoop cache - WingetUI"), RunAsAdmin: true);
                };
                ExtraControls.Children.Add(Scoop_ResetAppCache);

                CheckboxCard Scoop_CleanupOnStart = new()
                {
                    CornerRadius = new CornerRadius(0, 0, 8, 8),
                    BorderThickness = new Thickness(1, 0, 1, 1),
                    SettingName = Settings.K.EnableScoopCleanup,
                    Text = "Enable Scoop cleanup on launch",
                };
                ExtraControls.Children.Add(Scoop_CleanupOnStart);
            }
            // ----------------------------- CHOCO EXTRA SETTINGS ------------------------------

            else if (Manager is Chocolatey)
            {
                DisableNotifsCard.CornerRadius = new CornerRadius(8,8,0,0);
                DisableNotifsCard.BorderThickness = new Thickness(1, 1, 1, 0);
                ExtraControls.Children.Add(DisableNotifsCard);

                CheckboxCard Chocolatey_SystemChoco = new()
                {
                    Text = CoreTools.AutoTranslated("Use system Chocolatey"),
                    SettingName = Settings.K.UseSystemChocolatey,
                    CornerRadius = new CornerRadius(0, 0, 8, 8)
                };
                Chocolatey_SystemChoco.StateChanged += (_, _) => RestartRequired?.Invoke(this, new());
                ExtraControls.Children.Add(Chocolatey_SystemChoco);
            }

            // -------------------------------- VCPKG EXTRA SETTINGS --------------------------------------

            else if (Manager is Vcpkg)
            {
                DisableNotifsCard.CornerRadius = new CornerRadius(8,8,0,0);
                DisableNotifsCard.BorderThickness = new Thickness(1, 1, 1, 0);
                ExtraControls.Children.Add(DisableNotifsCard);

                Settings.SetValue(Settings.K.DefaultVcpkgTriplet, Vcpkg.GetDefaultTriplet());
                ComboboxCard Vcpkg_DefaultTriplet = new()
                {
                    Text = CoreTools.Translate("Default vcpkg triplet"),
                    SettingName = Settings.K.DefaultVcpkgTriplet,
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
                    CornerRadius = new CornerRadius(0, 0, 8, 8),
                    BorderThickness = new Thickness(1, 0, 1, 1)
                };
                StackPanel p = new() { Orientation = Orientation.Horizontal, Spacing = 5, };
                var VcPkgRootLabel = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
                var ResetVcPkgRootLabel = new HyperlinkButton { Content = CoreTools.Translate("Reset") };
                var OpenVcPkgRootLabel = new HyperlinkButton { Content = CoreTools.Translate("Open") };

                VcPkgRootLabel.Text = Settings.Get(Settings.K.CustomVcpkgRoot)
                    ? Settings.GetValue(Settings.K.CustomVcpkgRoot)
                    : "%VCPKG_ROOT%";
                OpenVcPkgRootLabel.IsEnabled = Settings.Get(Settings.K.CustomVcpkgRoot);
                ResetVcPkgRootLabel.IsEnabled = Settings.Get(Settings.K.CustomVcpkgRoot);

                ResetVcPkgRootLabel.Click += (_, _) =>
                {
                    VcPkgRootLabel.Text = "%VCPKG_ROOT%";
                    Settings.Set(Settings.K.CustomVcpkgRoot, false);
                    ResetVcPkgRootLabel.IsEnabled = false;
                    OpenVcPkgRootLabel.IsEnabled = false;
                };

                OpenVcPkgRootLabel.Click += (_, _) =>
                {
                    string directory = Settings.GetValue(Settings.K.CustomVcpkgRoot).Replace("/", "\\");
                    if (directory.Any()) Process.Start("explorer.exe", directory);
                };

                Vcpkg_CustomVcpkgRoot.Click += (_, _) =>
                {
                    ExternalLibraries.Pickers.FolderPicker openPicker =
                        new(MainApp.Instance.MainWindow.GetWindowHandle());
                    string folder = openPicker.Show();
                    if (folder != string.Empty)
                    {
                        Settings.SetValue(Settings.K.CustomVcpkgRoot, folder);
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
                DisableNotifsCard.CornerRadius = new CornerRadius(8);
                DisableNotifsCard.BorderThickness = new Thickness(1, 1, 1, 1);
                ExtraControls.Children.Add(DisableNotifsCard);
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
