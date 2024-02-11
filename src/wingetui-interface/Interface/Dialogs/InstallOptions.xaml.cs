using CommunityToolkit.WinUI.Animations;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using ModernWindow.Data;
using ModernWindow.PackageEngine;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Audio;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModernWindow.Interface.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class InstallOptionsPage : Page
    {
        AppTools bindings = AppTools.Instance;
        public InstallationOptions Options;
        public Package Package;
        public InstallOptionsPage(Package package, OperationType Operation)
        {
            Package = package;
            this.InitializeComponent();
            Options = new InstallationOptions(package);
            Options.LoadOptionsFromDisk();


            AdminCheckBox.IsChecked = Options.RunAsAdministrator;
            AdminCheckBox.IsEnabled = Package.Manager.Capabilities.CanRunAsAdmin;

            InteractiveCheckBox.IsChecked = Options.InteractiveInstallation;
            InteractiveCheckBox.IsEnabled = Package.Manager.Capabilities.CanRunInteractively;

            HashCheckbox.IsChecked = Options.SkipHashCheck;
            HashCheckbox.IsEnabled = Operation != OperationType.Uninstall && Package.Manager.Capabilities.CanSkipIntegrityChecks;

            ArchitectureComboBox.IsEnabled = Operation != OperationType.Uninstall && Package.Manager.Capabilities.SupportsCustomArchitectures;
            ArchitectureComboBox.Items.Add(bindings.Translate("Default"));
            ArchitectureComboBox.SelectedIndex = 0;


            if (Package.Manager.Capabilities.SupportsCustomArchitectures)
                foreach (var arch in Package.Manager.Capabilities.SupportedCustomArchitectures)
                {
                    ArchitectureComboBox.Items.Add(CommonTranslations.ArchNames[arch]);
                    if (Options.Architecture == arch)
                        ArchitectureComboBox.SelectedValue = CommonTranslations.ArchNames[arch];
                }

            VersionComboBox.IsEnabled = Operation == OperationType.Install && (Package.Manager.Capabilities.SupportsCustomVersions || Package.Manager.Capabilities.SupportsPreRelease);
            VersionComboBox.SelectionChanged += (s, e) =>
              { IgnoreUpdatesCheckbox.IsChecked = !new string[] { bindings.Translate("Latest"), bindings.Translate("PreRelease"), "" }.Contains(VersionComboBox.SelectedValue.ToString()); };
            VersionComboBox.Items.Add(bindings.Translate("Latest"));
            VersionComboBox.SelectedIndex = 0;
            if (package.Manager.Capabilities.SupportsPreRelease)
            {
                VersionComboBox.Items.Add(bindings.Translate("PreRelease"));
                if (Options.PreRelease)
                    VersionComboBox.SelectedValue = bindings.Translate("PreRelease");
            }

            if (Package.Manager.Capabilities.SupportsCustomVersions)
                _ = LoadVersions();
            else
                VersionProgress.Visibility = Visibility.Collapsed;

            ScopeCombo.IsEnabled = Package.Manager.Capabilities.SupportsCustomScopes;
            ScopeCombo.Items.Add(bindings.Translate("Default"));
            ScopeCombo.SelectedIndex = 0;
            if (package.Manager.Capabilities.SupportsCustomScopes)
            {
                ScopeCombo.Items.Add(bindings.Translate(CommonTranslations.ScopeNames[PackageScope.Local]));
                if (Options.InstallationScope == PackageScope.Local)
                    ScopeCombo.SelectedValue = CommonTranslations.ScopeNames[PackageScope.Local];
                ScopeCombo.Items.Add(bindings.Translate(CommonTranslations.ScopeNames[PackageScope.Global]));
                if (Options.InstallationScope == PackageScope.Global)
                    ScopeCombo.SelectedValue = CommonTranslations.ScopeNames[PackageScope.Global];
            }


            ResetDir.IsEnabled = Package.Manager.Capabilities.SupportsCustomLocations;
            SelectDir.IsEnabled = Package.Manager.Capabilities.SupportsCustomLocations;
            CustomInstallLocation.Text = Options.CustomInstallLocation;

            if (Options.CustomParameters != null)
                CustomParameters.Text = String.Join(' ', Options.CustomParameters);

            LoadIgnoredUpdates();
        }

        private async void LoadIgnoredUpdates()
        { 
            IgnoreUpdatesCheckbox.IsChecked = await Package.GetIgnoredUpdatesVersion() == "*";
        }

        private async Task LoadVersions()
        {
            IgnoreUpdatesCheckbox.IsChecked = await Package.HasUpdatesIgnored();

            var versions = await Package.Manager.GetPackageVersions(Package);
            
            foreach(string ver in versions)
            {
                VersionComboBox.Items.Add(ver);
                if (Options.Version == ver)
                    VersionComboBox.SelectedValue = ver;
            }

            VersionProgress.Visibility = Visibility.Collapsed;
        }

        public async void SaveToDisk()
        {
            Options.RunAsAdministrator = AdminCheckBox.IsChecked.Value;
            Options.InteractiveInstallation = InteractiveCheckBox.IsChecked.Value;
            Options.SkipHashCheck = HashCheckbox.IsChecked.Value;

            if(CommonTranslations.InvertedArchNames.ContainsKey(ArchitectureComboBox.SelectedValue.ToString()))
                Options.Architecture = CommonTranslations.InvertedArchNames[ArchitectureComboBox.SelectedValue.ToString()];
            else
                Options.Architecture = null;

            if (CommonTranslations.InvertedScopeNames.ContainsKey(ScopeCombo.SelectedValue.ToString()))
                Options.InstallationScope = CommonTranslations.InvertedScopeNames[ScopeCombo.SelectedValue.ToString()];
            else
                Options.InstallationScope = null;

            Options.CustomInstallLocation = CustomInstallLocation.Text;
            Options.CustomParameters = CustomParameters.Text.Split(' ').ToList();
            Options.PreRelease = VersionComboBox.SelectedValue.ToString() == bindings.Translate("PreRelease");

            if(VersionComboBox.SelectedValue.ToString() != bindings.Translate("PreRelease") && VersionComboBox.SelectedValue.ToString() != bindings.Translate("Latest"))
                Options.Version = VersionComboBox.SelectedValue.ToString();
            else
                Options.Version = "";

            if (IgnoreUpdatesCheckbox.IsChecked.Value)
                await Package.AddToIgnoredUpdates(version: "*");
            else
            {
                if(await Package.GetIgnoredUpdatesVersion()== "*");
                    await Package.RemoveFromIgnoredUpdates();
            }
            Options.SaveOptionsToDisk();
        }

        private async void SelectDir_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker openPicker = new Windows.Storage.Pickers.FolderPicker();

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(bindings.App.mainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);
            openPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            openPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await openPicker.PickSingleFolderAsync();
            if (folder != null)
                CustomInstallLocation.Text = folder.Path;
        }

        private void ResetDir_Click(object sender, RoutedEventArgs e)
        {
            CustomInstallLocation.Text = "";
        }
    }
}
