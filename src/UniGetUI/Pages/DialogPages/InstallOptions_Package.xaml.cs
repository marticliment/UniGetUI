using System.Data;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using UniGetUI.Core.Language;
using UniGetUI.Core.SettingsEngine.SecureSettings;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Serializable;
using UniGetUI.Pages.SettingsPages.GeneralPages;
using Windows.ApplicationModel.Activation;
using Architecture = UniGetUI.PackageEngine.Enums.Architecture;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Interface.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class InstallOptionsPage : Page
    {
        public InstallOptions Options;
        public IPackage Package;
        public event EventHandler? Close;
        private readonly OperationType Operation;
        private readonly string packageInstallLocation;
        private bool _uiLoaded;

        public InstallOptionsPage(IPackage package, InstallOptions options) : this(package, OperationType.None, options) { }
        public InstallOptionsPage(IPackage package, OperationType operation, InstallOptions options)
        {
            Package = package;
            InitializeComponent();
            Operation = operation;
            Options = options;

            ProfileComboBox.Items.Add(CoreTools.Translate("Install"));
            ProfileComboBox.Items.Add(CoreTools.Translate("Update"));
            ProfileComboBox.Items.Add(CoreTools.Translate("Uninstall"));
            ProfileComboBox.SelectedIndex = operation switch { OperationType.Update => 1, OperationType.Uninstall => 2, _ => 0 };
            ProfileComboBox.SelectionChanged += (_, _) =>
            {
                EnableDisableControls(ProfileComboBox.SelectedIndex switch
                {
                    1 => OperationType.Update,
                    2 => OperationType.Uninstall,
                    _ => OperationType.Install,
                });
            };

            FollowGlobalOptionsSwitch.IsOn = !options.OverridesNextLevelOpts;
            FollowGlobalOptionsSwitch.Toggled += (_, _) =>
            {
                EnableDisableControls(ProfileComboBox.SelectedIndex switch
                {
                    1 => OperationType.Update,
                    2 => OperationType.Uninstall,
                    _ => OperationType.Install,
                });
            };

            var iconSource = new BitmapImage()
            {
                UriSource = package.GetIconUrl(),
                DecodePixelHeight = 32,
                DecodePixelWidth = 32,
                DecodePixelType =
                DecodePixelType.Logical
            };

            PackageIcon.Source = iconSource;
            async Task LoadImage()
            {
                iconSource.UriSource = await Task.Run(package.GetIconUrl);
            }
            _ = LoadImage();
            DialogTitle.Text = CoreTools.Translate("{0} installation options", package.Name);
            PlaceholderText.Text = CoreTools.Translate("{0} Install options are currently locked because {0} follows the default install options.", package.Name);

            packageInstallLocation = Package.Manager.DetailsHelper.GetInstallLocation(package) ?? CoreTools.Translate("Unset or unknown");

            AdminCheckBox.IsChecked = Options.RunAsAdministrator;
            InteractiveCheckBox.IsChecked = Options.InteractiveInstallation;
            HashCheckbox.IsChecked = Options.SkipHashCheck;

            ArchitectureComboBox.Items.Add(CoreTools.Translate("Default"));
            ArchitectureComboBox.SelectedIndex = 0;

            if (Package.Manager.Capabilities.SupportsCustomArchitectures)
            {
                foreach (string arch in Package.Manager.Capabilities.SupportedCustomArchitectures)
                {
                    ArchitectureComboBox.Items.Add(arch);
                    if (Options.Architecture == arch)
                    {
                        ArchitectureComboBox.SelectedValue = arch;
                    }
                }
            }

            VersionComboBox.SelectionChanged += (_, _) =>
            {
                IgnoreUpdatesCheckbox.IsChecked =
                    !(new []
                    {
                        CoreTools.Translate("Latest"),
                        CoreTools.Translate("PreRelease"),
                        ""
                    }.Contains(VersionComboBox.SelectedValue.ToString()));
            };

            VersionComboBox.Items.Add(CoreTools.Translate("Latest"));
            VersionComboBox.SelectedIndex = 0;
            if (package.Manager.Capabilities.SupportsPreRelease)
            {
                VersionComboBox.Items.Add(CoreTools.Translate("PreRelease"));
                if (Options.PreRelease)
                {
                    VersionComboBox.SelectedValue = CoreTools.Translate("PreRelease");
                }
            }

            SkipMinorUpdatesCheckbox.IsChecked = Options.SkipMinorUpdates;

            if (Package.Manager.Capabilities.SupportsCustomVersions)
            {
                _ = LoadVersions();
            }
            else
            {
                VersionProgress.Visibility = Visibility.Collapsed;
            }

            ScopeCombo.Items.Add(CoreTools.Translate("Default"));
            ScopeCombo.SelectedIndex = 0;
            if (package.Manager.Capabilities.SupportsCustomScopes)
            {
                ScopeCombo.Items.Add(CoreTools.Translate(CommonTranslations.ScopeNames[PackageScope.Local]));
                if (Options.InstallationScope == PackageScope.Local)
                {
                    ScopeCombo.SelectedValue = CommonTranslations.ScopeNames[PackageScope.Local];
                }

                ScopeCombo.Items.Add(CoreTools.Translate(CommonTranslations.ScopeNames[PackageScope.Global]));
                if (Options.InstallationScope == PackageScope.Global)
                {
                    ScopeCombo.SelectedValue = CommonTranslations.ScopeNames[PackageScope.Global];
                }
            }


            if (Options.CustomInstallLocation == "") CustomInstallLocation.Text = packageInstallLocation;
            else CustomInstallLocation.Text = Options.CustomInstallLocation;

            if (Options.CustomParameters.Count != 0)
            {
                CustomParameters.Text = string.Join(' ', Options.CustomParameters);
            }

            _uiLoaded = true;
            EnableDisableControls(operation);
            LoadIgnoredUpdates();
        }

        private void EnableDisableControls(OperationType operation)
        {
            if(FollowGlobalOptionsSwitch.IsOn)
            {
                OptionsPanel0.Opacity = 0.3;
                SettingsSwitchPresenter.Opacity = 0.3;
                SettingsTabBar.Opacity = 0.3;
                OptionsPanelBase.IsEnabled = false;
                PlaceholderBanner.Visibility = Visibility.Visible;
            }
            else
            {
                OptionsPanel0.Opacity = 1;
                SettingsSwitchPresenter.Opacity = 1;
                SettingsTabBar.Opacity = 1;
                OptionsPanelBase.IsEnabled = true;
                PlaceholderBanner.Visibility = Visibility.Collapsed;

                AdminCheckBox.IsEnabled = Package.Manager.Capabilities.CanRunAsAdmin;
                InteractiveCheckBox.IsEnabled = Package.Manager.Capabilities.CanRunInteractively;
                HashCheckbox.IsEnabled = operation != OperationType.Uninstall && Package.Manager.Capabilities.CanSkipIntegrityChecks;
                ArchitectureComboBox.IsEnabled = operation != OperationType.Uninstall && Package.Manager.Capabilities.SupportsCustomArchitectures;
                VersionComboBox.IsEnabled =
                    (operation == OperationType.Install
                        || operation == OperationType.None)
                    && (Package.Manager.Capabilities.SupportsCustomVersions
                        || Package.Manager.Capabilities.SupportsPreRelease);
                ScopeCombo.IsEnabled = Package.Manager.Capabilities.SupportsCustomScopes;
                ResetDir.IsEnabled = Package.Manager.Capabilities.SupportsCustomLocations;
                SelectDir.IsEnabled = Package.Manager.Capabilities.SupportsCustomLocations;
            }

            bool IsCLIEnabled = SecureSettings.Get(SecureSettings.ALLOW_CLI_ARGUMENTS);
            CustomParameters.IsEnabled = IsCLIEnabled;
            CustomParametersLabel.Opacity = IsCLIEnabled ? 1 : 0.5;
            GoToCLISettings.Visibility = IsCLIEnabled ? Visibility.Collapsed : Visibility.Visible;
            CLIDisabled.Visibility = IsCLIEnabled ? Visibility.Collapsed : Visibility.Visible;
            GenerateCommand();
        }

        private async void LoadIgnoredUpdates()
        {
            IgnoreUpdatesCheckbox.IsChecked = await Package.GetIgnoredUpdatesVersionAsync() == "*";
        }

        private async Task LoadVersions()
        {
            IgnoreUpdatesCheckbox.IsChecked = await Package.HasUpdatesIgnoredAsync();
            VersionComboBox.IsEnabled = false;

            IReadOnlyList<string> versions = await Task.Run(() => Package.Manager.DetailsHelper.GetVersions(Package));
            foreach (string ver in versions)
            {
                VersionComboBox.Items.Add(ver);
                if (Options.Version == ver)
                {
                    VersionComboBox.SelectedValue = ver;
                }
            }

            VersionComboBox.IsEnabled =
                (Operation == OperationType.Install
                 || Operation == OperationType.None)
                && (Package.Manager.Capabilities.SupportsCustomVersions
                    || Package.Manager.Capabilities.SupportsPreRelease);
            VersionProgress.Visibility = Visibility.Collapsed;
        }

        public async Task<InstallOptions> GetUpdatedOptions(bool updateIgnoredUpdates = true)
        {
            Options.RunAsAdministrator = AdminCheckBox?.IsChecked ?? false;
            Options.InteractiveInstallation = InteractiveCheckBox?.IsChecked ?? false;
            Options.SkipHashCheck = HashCheckbox?.IsChecked ?? false;
            Options.OverridesNextLevelOpts = !FollowGlobalOptionsSwitch.IsOn;

            Options.Architecture = "";
            var userSelection = ArchitectureComboBox.SelectedValue?.ToString() ?? "";
            if (Architecture.ValidValues.Contains(userSelection))
            {
                Options.Architecture = userSelection;
            }

            Options.InstallationScope = "";
            userSelection = ScopeCombo.SelectedValue?.ToString() ?? "";
            if (CommonTranslations.InvertedScopeNames.TryGetValue(userSelection, out string? value))
            {
                Options.InstallationScope = value;
            }

            if (CustomInstallLocation.Text == packageInstallLocation) Options.CustomInstallLocation = "";
            else Options.CustomInstallLocation = CustomInstallLocation.Text;

            Options.CustomParameters = CustomParameters.Text.Split(' ').ToList();
            Options.PreRelease = VersionComboBox.SelectedValue.ToString() == CoreTools.Translate("PreRelease");

            if (VersionComboBox.SelectedValue.ToString() != CoreTools.Translate("PreRelease") && VersionComboBox.SelectedValue.ToString() != CoreTools.Translate("Latest"))
            {
                Options.Version = VersionComboBox.SelectedValue.ToString() ?? "";
            }
            else
            {
                Options.Version = "";
            }
            Options.SkipMinorUpdates = SkipMinorUpdatesCheckbox?.IsChecked ?? false;

            if (updateIgnoredUpdates)
            {
                if (IgnoreUpdatesCheckbox?.IsChecked ?? false)
                {
                    await Package.AddToIgnoredUpdatesAsync(version: "*");
                }
                else
                {
                    if (await Package.GetIgnoredUpdatesVersionAsync() == "*")
                    {
                        await Package.RemoveFromIgnoredUpdatesAsync();
                    }
                }
            }
            return Options;
        }

        private void SelectDir_Click(object sender, RoutedEventArgs e)
        {
            ExternalLibraries.Pickers.FolderPicker openPicker = new(MainApp.Instance.MainWindow.GetWindowHandle());
            string folder = openPicker.Show();
            if (folder != string.Empty)
            {
                CustomInstallLocation.Text = folder;
            }
            GenerateCommand();
        }

        private void ResetDir_Click(object sender, RoutedEventArgs e)
        {
            CustomInstallLocation.Text = packageInstallLocation;
            GenerateCommand();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }

        private void CustomParameters_TextChanged(object sender, TextChangedEventArgs e) => GenerateCommand();
        private void ScopeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => GenerateCommand();
        private void VersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => GenerateCommand();
        private void AdminCheckBox_Click(object sender, RoutedEventArgs e) => GenerateCommand();
        private void InteractiveCheckBox_Click(object sender, RoutedEventArgs e) => GenerateCommand();
        private void HashCheckbox_Click(object sender, RoutedEventArgs e) => GenerateCommand();
        private void ArchitectureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => GenerateCommand();

        private async void GenerateCommand()
        {
            if (!_uiLoaded) return;
            InstallOptions options = await GetUpdatedOptions(updateIgnoredUpdates: false);
            options = await InstallOptionsFactory.LoadApplicableAsync(this.Package, overridePackageOptions: options);

            var op = ProfileComboBox.SelectedIndex switch
            {
                1 => OperationType.Update,
                2 => OperationType.Uninstall,
                _ => OperationType.Install,
            };
            var commandline = await Task.Run(() => Package.Manager.OperationHelper.GetParameters(Package, options, op));
            CommandBox.Text = Package.Manager.Properties.ExecutableFriendlyName + " " + string.Join(" ", commandline);
        }

        private void LayoutGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(LayoutGrid.ActualSize.Y > 1 && LayoutGrid.ActualSize.Y < double.PositiveInfinity) MaxHeight = LayoutGrid.ActualSize.Y;
        }

        private void UnlockSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            FollowGlobalOptionsSwitch.IsOn = false;
        }

        private void GoToDefaultOptionsSettings_Click(object sender, RoutedEventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
            MainApp.Instance.MainWindow.NavigationPage.OpenManagerSettings(Package.Manager);
        }

        private void GoToSecureSettings_Click(object sender, RoutedEventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
            MainApp.Instance.MainWindow.NavigationPage.OpenSettingsPage(typeof(Administrator));
        }
    }
}
