using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Language;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Serializable;
using Windows.ApplicationModel;
using Windows.Gaming.XboxLive.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Pages.DialogPages;

public sealed partial class InstallOptions_Manager : UserControl
{
    private readonly IPackageManager Manager;
    private static string DefaultLocationLabel = null!;
    public InstallOptions_Manager(IPackageManager manager)
    {
        Manager = manager;
        InitializeComponent();
        AdminCheckBox.Content = CoreTools.Translate("Run as admin");
        InteractiveCheckBox.Content = CoreTools.Translate("Interactive installation");
        HashCheckBox.Content = CoreTools.Translate("Skip hash check");
        PreReleaseCheckBox.Content = CoreTools.Translate("Allow pre-release versions");
        ArchLabel.Text = CoreTools.Translate("Architecture to install:");
        ScopeLabel.Text = CoreTools.Translate("Installation scope:");
        LocationLabel.Text = CoreTools.Translate("Install location:");
        SelectDir.Content = CoreTools.Translate("Select");
        ResetDir.Content = CoreTools.Translate("Reset");
        CustomCommandsLabel.Text = CoreTools.Translate("Custom arguments:");
        DefaultLocationLabel ??= CoreTools.Translate("Package's default");
        ResetButton.Content = CoreTools.Translate("Reset");
        ApplyButton.Content = CoreTools.Translate("Apply");
        HeaderLabel.Text = CoreTools.Translate("The following options will be applied by default each time a {0} package is installed, upgraded or uninstalled.", Manager.DisplayName);

        DisableAllInput();
        _ = LoadOptions();
    }

    private async Task LoadOptions()
    {
        LoadingIndicator.Visibility = Visibility.Visible;
        var options = await InstallOptionsFactory.LoadForManagerAsync(Manager);

        // This delay allows the spinner to show,
        // and give the user the sensation that things have worked
        await Task.Delay(500);

        // Administrator checkbox
        AdminCheckBox.IsEnabled = true;
        AdminCheckBox.IsChecked = options.RunAsAdministrator;

        // interactive checkbox
        if (Manager.Capabilities.CanRunInteractively)
        {
            InteractiveCheckBox.IsEnabled = true;
            InteractiveCheckBox.IsChecked = options.InteractiveInstallation;
        }

        // skip hash checkbox
        if (Manager.Capabilities.CanSkipIntegrityChecks)
        {
            HashCheckBox.IsEnabled = true;
            HashCheckBox.IsChecked = options.SkipHashCheck;
        }

        // prerelease support
        if (Manager.Capabilities.SupportsPreRelease)
        {
            PreReleaseCheckBox.IsEnabled = true;
            PreReleaseCheckBox.IsChecked = options.PreRelease;
        }

        // Architecture combobox
        ArchitectureCombo.Items.Clear();
        ArchitectureCombo.Items.Add(CoreTools.Translate("Default"));
        ArchitectureCombo.SelectedIndex = 0;
        if (Manager.Capabilities.SupportsCustomArchitectures)
        {
            ArchitectureCombo.IsEnabled = true;
            foreach (string arch in Manager.Capabilities.SupportedCustomArchitectures)
            {
                ArchitectureCombo.Items.Add(CommonTranslations.ArchNames[arch]);
                if (options.Architecture == CommonTranslations.ArchNames[arch])
                {
                    ArchitectureCombo.SelectedValue = CommonTranslations.ArchNames[arch];
                }
            }
        }

        // Scope combobox
        ScopeCombo.Items.Clear();
        ScopeCombo.Items.Add(CoreTools.Translate("Default"));
        ScopeCombo.SelectedIndex = 0;
        if (Manager.Capabilities.SupportsCustomScopes)
        {
            ScopeCombo.IsEnabled = true;
            ScopeCombo.Items.Add(CoreTools.Translate(CommonTranslations.ScopeNames[PackageScope.Local]));
            if (options.InstallationScope == CommonTranslations.ScopeNames_NonLang[PackageScope.Local])
            {
                ScopeCombo.SelectedValue = CommonTranslations.ScopeNames[PackageScope.Local];
            }

            ScopeCombo.Items.Add(CoreTools.Translate(CommonTranslations.ScopeNames[PackageScope.Global]));
            if (options.InstallationScope == CommonTranslations.ScopeNames_NonLang[PackageScope.Global])
            {
                ScopeCombo.SelectedValue = CommonTranslations.ScopeNames[PackageScope.Global];
            }
        }

        // Install location
        if(Manager.Capabilities.SupportsCustomLocations)
        {
            SelectDir.IsEnabled = true;
            if (options.CustomInstallLocation.Any())
            {
                CustomInstallLocation.Text = options.CustomInstallLocation;
                ResetDir.IsEnabled = true;
            }
            else
            {
                CustomInstallLocation.Text = DefaultLocationLabel;
                ResetDir.IsEnabled = false;
            }
        }
        else
        {
            CustomInstallLocation.Text = CoreTools.Translate("Install location can't be changed for {0} packages", Manager.DisplayName);
        }

        CustomParameters.IsEnabled = true;
        CustomParameters.Text = string.Join(' ', options.CustomParameters);

        ResetButton.IsEnabled = true;
        ApplyButton.IsEnabled = true;
        ApplyButton.Style = (Style)Application.Current.Resources["DefaultButtonStyle"];

        LoadingIndicator.Visibility = Visibility.Collapsed;
    }

    private async Task SaveOptions()
    {
        LoadingIndicator.Visibility = Visibility.Visible;
        DisableAllInput();

        SerializableInstallationOptions options = new();
        // Checkboxes
        options.RunAsAdministrator = AdminCheckBox.IsChecked ?? false;
        options.SkipHashCheck = HashCheckBox.IsChecked ?? false;
        options.InteractiveInstallation = InteractiveCheckBox.IsChecked ?? false;
        options.PreRelease = PreReleaseCheckBox.IsChecked ?? false;

        // Administrator
        options.Architecture = "";
        string candidateValue = ArchitectureCombo.SelectedValue.ToString() ?? "";
        if (CommonTranslations.InvertedArchNames.ContainsKey(candidateValue))
        {
            options.Architecture = candidateValue;
        }

        // Scope
        options.InstallationScope = "";
        candidateValue = ScopeCombo.SelectedValue.ToString() ?? "";
        if (CommonTranslations.InvertedScopeNames.TryGetValue(candidateValue, out string? result))
        {
            options.InstallationScope = CommonTranslations.ScopeNames_NonLang[result];
        }

        // Location
        options.CustomInstallLocation = "";
        if(CustomInstallLocation.Text != DefaultLocationLabel && Manager.Capabilities.SupportsCustomLocations)
        {
            options.CustomInstallLocation = CustomInstallLocation.Text;
        }

        // Command-line parameters
        options.CustomParameters = CustomParameters.Text.Split(' ').Where(x => x.Any()).ToList();

        await InstallOptionsFactory.SaveForManagerAsync(options, Manager);
        await LoadOptions();
    }

    private async Task ResetOptions()
    {
        LoadingIndicator.Visibility = Visibility.Visible;
        DisableAllInput();

        await InstallOptionsFactory.SaveForManagerAsync(new(), Manager);
        await LoadOptions();
    }

    private void DisableAllInput()
    {
        PreReleaseCheckBox.IsEnabled = false;
        AdminCheckBox.IsEnabled = false;
        InteractiveCheckBox.IsEnabled = false;
        HashCheckBox.IsEnabled = false;
        ArchitectureCombo.IsEnabled = false;
        ScopeCombo.IsEnabled = false;
        SelectDir.IsEnabled = false;
        ResetDir.IsEnabled = false;
        CustomParameters.IsEnabled = false;
        ResetButton.IsEnabled = false;
        ApplyButton.IsEnabled = false;
    }

    private void CustomParameters_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyButton.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
    }

    private void ResetDir_Click(object sender, RoutedEventArgs e)
    {
        CustomInstallLocation.Text = DefaultLocationLabel;
        ResetDir.IsEnabled = false;
        ApplyButton.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
    }

    private void SelectDir_Click(object sender, RoutedEventArgs e)
    {
        ExternalLibraries.Pickers.FolderPicker openPicker = new(MainApp.Instance.MainWindow.GetWindowHandle());
        string folder = openPicker.Show();
        if (folder != string.Empty)
        {
            CustomInstallLocation.Text = folder.TrimEnd('\\') + "\\%PACKAGE%";
            ResetDir.IsEnabled = true;
            ApplyButton.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
        }
    }

    private void ScopeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyButton.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
    }

    private void HashCheckbox_Click(object sender, RoutedEventArgs e)
    {
        ApplyButton.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
    }

    private void InteractiveCheckBox_Click(object sender, RoutedEventArgs e)
    {
        ApplyButton.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
    }

    private void AdminCheckBox_Click(object sender, RoutedEventArgs e)
    {
        ApplyButton.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
    }

    private void ArchitectureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyButton.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
    }

    private void PreReleaseCheckBox_Click(object sender, RoutedEventArgs e)
    {
        ApplyButton.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _ = ResetOptions();
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        _ = SaveOptions();
    }
}
