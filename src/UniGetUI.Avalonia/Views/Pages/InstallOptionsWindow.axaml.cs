using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using UniGetUI.Avalonia.Models;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.SettingsEngine.SecureSettings;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Serializable;

namespace UniGetUI.Avalonia.Views.Pages;

public partial class InstallOptionsWindow : Window
{
    private readonly IPackage _package;
    private InstallOptions _options = new();

    public InstallOptionsWindow(IPackage package, PackagePageMode pageMode)
    {
        _package = package;
        InitializeComponent();
        ApplyTranslations();
        PopulateStaticControls();
        _ = LoadOptionsAsync();
    }

    private void ApplyTranslations()
    {
        Title = CoreTools.Translate("{0} — Install options", _package.Name);
        PackageTitleBlock.Text = CoreTools.Translate("{0} installation options", _package.Name);
        PackageIdBlock.Text = _package.Id;
        PackageVersionBlock.Text = string.IsNullOrWhiteSpace(_package.VersionString)
            ? CoreTools.Translate("Unknown")
            : _package.VersionString;
        PackageManagerBlock.Text = _package.Source.AsString_DisplayName;

        GeneralOptionsTitleBlock.Text = CoreTools.Translate("General options");
        RunAsAdminCheckBox.Content = CoreTools.Translate("Run as administrator");
        InteractiveCheckBox.Content = CoreTools.Translate("Interactive installation");
        HashCheckBox.Content = CoreTools.Translate("Skip hash / integrity check");
        AutoUpdateCheckBox.Content = CoreTools.Translate("Auto-update this package");
        SkipMinorUpdatesCheckBox.Content = CoreTools.Translate("Skip minor version updates");
        IgnoreUpdatesCheckBox.Content = CoreTools.Translate("Ignore updates for this package");
        UninstallPreviousCheckBox.Content = CoreTools.Translate("Uninstall previous version on update");

        KillProcessesTitleBlock.Text = CoreTools.Translate("Processes to kill before operation");
        KillProcessesDescBlock.Text = CoreTools.Translate("Enter process names (comma-separated) that should be closed before this package operation runs.");
        KillProcessesTextBox.Watermark = CoreTools.Translate("e.g.  Chrome.exe, Notepad.exe");
        KillProcessesThatWontDieCheckBox.Content = CoreTools.Translate("Try to kill processes that refuse to close");

        VersionSectionTitleBlock.Text = CoreTools.Translate("Version, architecture and scope");
        VersionLabelBlock.Text = CoreTools.Translate("Version");
        ArchitectureLabelBlock.Text = CoreTools.Translate("Architecture");
        ScopeLabelBlock.Text = CoreTools.Translate("Scope");

        LocationTitleBlock.Text = CoreTools.Translate("Custom install location");
        CustomLocationTextBox.Watermark = CoreTools.Translate("Leave empty to use the default location");

        ParametersTitleBlock.Text = CoreTools.Translate("Custom CLI arguments");
        CustomParamsDescBlock.Text = CoreTools.Translate("Additional arguments passed to the installer executable.");
        CustomParams1TextBox.Watermark = CoreTools.Translate("e.g.  /norestart /quiet");
        CustomParams2TextBox.Watermark = CoreTools.Translate("e.g.  /norestart /quiet");
        CustomParams3TextBox.Watermark = CoreTools.Translate("e.g.  /norestart /quiet");
        InstallParamsLabelBlock.Text = CoreTools.Translate("Install arguments");
        UpdateParamsLabelBlock.Text = CoreTools.Translate("Update arguments");
        UninstallParamsLabelBlock.Text = CoreTools.Translate("Uninstall arguments");

        PrePostTitleBlock.Text = CoreTools.Translate("Pre / post operation commands");
        PrePostCommandsDescBlock.Text = CoreTools.Translate("These shell commands run before or after the package operation.");
        PreInstallCommandBox.Watermark = CoreTools.Translate("Command to run before installation");
        PostInstallCommandBox.Watermark = CoreTools.Translate("Command to run after installation");
        PreUpdateCommandBox.Watermark = CoreTools.Translate("Command to run before update");
        PostUpdateCommandBox.Watermark = CoreTools.Translate("Command to run after update");
        PreUninstallCommandBox.Watermark = CoreTools.Translate("Command to run before uninstall");
        PostUninstallCommandBox.Watermark = CoreTools.Translate("Command to run after uninstall");
        ToolTip.SetTip(AbortInstallCheckBox, CoreTools.Translate("Abort install if this command fails"));
        ToolTip.SetTip(AbortUpdateCheckBox, CoreTools.Translate("Abort update if this command fails"));
        ToolTip.SetTip(AbortUninstallCheckBox, CoreTools.Translate("Abort uninstall if this command fails"));
        PreInstallLabelBlock.Text = CoreTools.Translate("Pre-install command");
        PostInstallLabelBlock.Text = CoreTools.Translate("Post-install command");
        PreUpdateLabelBlock.Text = CoreTools.Translate("Pre-update command");
        PostUpdateLabelBlock.Text = CoreTools.Translate("Post-update command");
        PreUninstallLabelBlock.Text = CoreTools.Translate("Pre-uninstall command");
        PostUninstallLabelBlock.Text = CoreTools.Translate("Post-uninstall command");
        AbortInstallCheckBox.Content = CoreTools.Translate("Abort if fails");
        AbortUpdateCheckBox.Content = CoreTools.Translate("Abort if fails");
        AbortUninstallCheckBox.Content = CoreTools.Translate("Abort if fails");

        SaveBtn.Content = CoreTools.Translate("Save");
        CancelBtn.Content = CoreTools.Translate("Cancel");

        SaveBtn.Click += SaveBtn_OnClick;
        CancelBtn.Click += CancelBtn_OnClick;
    }

    private void PopulateStaticControls()
    {
        var caps = _package.Manager.Capabilities;

        // Disable checkboxes based on capability
        RunAsAdminCheckBox.IsEnabled = caps.CanRunAsAdmin;
        InteractiveCheckBox.IsEnabled = caps.CanRunInteractively;
        HashCheckBox.IsEnabled = caps.CanSkipIntegrityChecks;
        UninstallPreviousCheckBox.IsVisible = caps.CanUninstallPreviousVersionsAfterUpdate;

        // Version ComboBox
        VersionComboBox.Items.Add(CoreTools.Translate("Latest"));
        VersionComboBox.SelectedIndex = 0;
        if (caps.SupportsPreRelease)
        {
            VersionComboBox.Items.Add(CoreTools.Translate("PreRelease"));
        }
        VersionComboBox.IsEnabled = caps.SupportsCustomVersions || caps.SupportsPreRelease;

        // Architecture ComboBox
        ArchitectureComboBox.Items.Add(CoreTools.Translate("Default"));
        ArchitectureComboBox.SelectedIndex = 0;
        if (caps.SupportsCustomArchitectures)
        {
            foreach (var arch in caps.SupportedCustomArchitectures)
            {
                ArchitectureComboBox.Items.Add(arch);
            }
        }
        ArchitectureComboBox.IsEnabled = caps.SupportsCustomArchitectures;

        // Scope ComboBox
        ScopeComboBox.Items.Add(CoreTools.Translate("Default"));
        ScopeComboBox.SelectedIndex = 0;
        if (caps.SupportsCustomScopes)
        {
            ScopeComboBox.Items.Add(CoreTools.Translate("User / current user"));
            ScopeComboBox.Items.Add(CoreTools.Translate("Machine / all users"));
        }
        ScopeComboBox.IsEnabled = caps.SupportsCustomScopes;

        // Custom location card
        CustomLocationCard.IsVisible = caps.SupportsCustomLocations;

        // CLI parameters card
        bool cliAllowed = SecureSettings.Get(SecureSettings.K.AllowCLIArguments);
        CustomParametersCard.IsVisible = cliAllowed;

        // Pre/post commands card
        bool prePostAllowed = SecureSettings.Get(SecureSettings.K.AllowPrePostOpCommand);
        PrePostCommandsCard.IsVisible = prePostAllowed;

        // KillProcessesThatWontDie global setting
        KillProcessesThatWontDieCheckBox.IsChecked = Settings.Get(Settings.K.KillProcessesThatRefuseToDie);
    }

    private async Task LoadOptionsAsync()
    {
        _options = await InstallOptionsFactory.LoadForPackageAsync(_package);
        ApplyOptionsToUi(_options);
        IgnoreUpdatesCheckBox.IsChecked = await _package.HasUpdatesIgnoredAsync();

        if (_package.Manager.Capabilities.SupportsCustomVersions)
        {
            VersionLoadingBar.IsVisible = true;
            VersionComboBox.IsEnabled = false;
            var versions = await Task.Run(() => _package.Manager.DetailsHelper.GetVersions(_package));
            foreach (var v in versions)
            {
                VersionComboBox.Items.Add(v);
            }
            VersionLoadingBar.IsVisible = false;
            VersionComboBox.IsEnabled = true;
        }
    }

    private void ApplyOptionsToUi(InstallOptions options)
    {
        RunAsAdminCheckBox.IsChecked = options.RunAsAdministrator;
        InteractiveCheckBox.IsChecked = options.InteractiveInstallation;
        HashCheckBox.IsChecked = options.SkipHashCheck;
        AutoUpdateCheckBox.IsChecked = options.AutoUpdatePackage;
        SkipMinorUpdatesCheckBox.IsChecked = options.SkipMinorUpdates;
        UninstallPreviousCheckBox.IsChecked = options.UninstallPreviousVersionsOnUpdate;
        KillProcessesTextBox.Text = string.Join(", ", options.KillBeforeOperation);

        if (options.PreRelease && _package.Manager.Capabilities.SupportsPreRelease)
        {
            VersionComboBox.SelectedItem = CoreTools.Translate("PreRelease");
        }
        else if (!string.IsNullOrWhiteSpace(options.Version))
        {
            // Try to select an already-added version; otherwise "Latest" stays selected
            var match = VersionComboBox.Items.Cast<object?>()
                .FirstOrDefault(i => i?.ToString() == options.Version);
            if (match is not null)
            {
                VersionComboBox.SelectedItem = match;
            }
        }

        if (!string.IsNullOrWhiteSpace(options.Architecture))
        {
            var match = ArchitectureComboBox.Items.Cast<object?>()
                .FirstOrDefault(i => i?.ToString() == options.Architecture);
            if (match is not null)
            {
                ArchitectureComboBox.SelectedItem = match;
            }
        }

        if (!string.IsNullOrWhiteSpace(options.InstallationScope))
        {
            // Map stored scope string to display string
            string? displayScope = options.InstallationScope == PackageScope.User
                ? CoreTools.Translate("User / current user")
                : options.InstallationScope == PackageScope.Machine
                    ? CoreTools.Translate("Machine / all users")
                    : null;
            if (displayScope is not null)
            {
                var match = ScopeComboBox.Items.Cast<object?>()
                    .FirstOrDefault(i => i?.ToString() == displayScope);
                if (match is not null)
                {
                    ScopeComboBox.SelectedItem = match;
                }
            }
        }

        CustomLocationTextBox.Text = options.CustomInstallLocation;

        CustomParams1TextBox.Text = string.Join(' ', options.CustomParameters_Install);
        CustomParams2TextBox.Text = string.Join(' ', options.CustomParameters_Update);
        CustomParams3TextBox.Text = string.Join(' ', options.CustomParameters_Uninstall);

        PreInstallCommandBox.Text = options.PreInstallCommand;
        PostInstallCommandBox.Text = options.PostInstallCommand;
        AbortInstallCheckBox.IsChecked = options.AbortOnPreInstallFail;

        PreUpdateCommandBox.Text = options.PreUpdateCommand;
        PostUpdateCommandBox.Text = options.PostUpdateCommand;
        AbortUpdateCheckBox.IsChecked = options.AbortOnPreUpdateFail;

        PreUninstallCommandBox.Text = options.PreUninstallCommand;
        PostUninstallCommandBox.Text = options.PostUninstallCommand;
        AbortUninstallCheckBox.IsChecked = options.AbortOnPreUninstallFail;
    }

    private InstallOptions ReadOptionsFromUi()
    {
        var options = new InstallOptions();

        options.RunAsAdministrator = RunAsAdminCheckBox.IsChecked ?? false;
        options.InteractiveInstallation = InteractiveCheckBox.IsChecked ?? false;
        options.SkipHashCheck = HashCheckBox.IsChecked ?? false;
        options.AutoUpdatePackage = AutoUpdateCheckBox.IsChecked ?? false;
        options.UninstallPreviousVersionsOnUpdate = UninstallPreviousCheckBox.IsChecked ?? false;

        // Version
        string versionSelection = VersionComboBox.SelectedItem?.ToString() ?? "";
        if (versionSelection == CoreTools.Translate("PreRelease"))
        {
            options.PreRelease = true;
            options.Version = string.Empty;
        }
        else if (versionSelection == CoreTools.Translate("Latest") || string.IsNullOrWhiteSpace(versionSelection))
        {
            options.PreRelease = false;
            options.Version = string.Empty;
        }
        else
        {
            options.PreRelease = false;
            options.Version = versionSelection;
        }

        // Architecture
        string archSelection = ArchitectureComboBox.SelectedItem?.ToString() ?? "";
        options.Architecture = Architecture.ValidValues.Contains(archSelection) ? archSelection : string.Empty;

        // Scope
        string scopeDisplay = ScopeComboBox.SelectedItem?.ToString() ?? "";
        if (scopeDisplay == CoreTools.Translate("User / current user"))
        {
            options.InstallationScope = PackageScope.User;
        }
        else if (scopeDisplay == CoreTools.Translate("Machine / all users"))
        {
            options.InstallationScope = PackageScope.Machine;
        }
        else
        {
            options.InstallationScope = string.Empty;
        }

        options.CustomInstallLocation = CustomLocationTextBox.Text?.Trim() ?? string.Empty;

        options.CustomParameters_Install = SplitParams(CustomParams1TextBox.Text);
        options.CustomParameters_Update = SplitParams(CustomParams2TextBox.Text);
        options.CustomParameters_Uninstall = SplitParams(CustomParams3TextBox.Text);

        options.PreInstallCommand = PreInstallCommandBox.Text?.Trim() ?? string.Empty;
        options.PostInstallCommand = PostInstallCommandBox.Text?.Trim() ?? string.Empty;
        options.AbortOnPreInstallFail = AbortInstallCheckBox.IsChecked ?? true;

        options.PreUpdateCommand = PreUpdateCommandBox.Text?.Trim() ?? string.Empty;
        options.PostUpdateCommand = PostUpdateCommandBox.Text?.Trim() ?? string.Empty;
        options.AbortOnPreUpdateFail = AbortUpdateCheckBox.IsChecked ?? true;

        options.PreUninstallCommand = PreUninstallCommandBox.Text?.Trim() ?? string.Empty;
        options.PostUninstallCommand = PostUninstallCommandBox.Text?.Trim() ?? string.Empty;
        options.AbortOnPreUninstallFail = AbortUninstallCheckBox.IsChecked ?? true;

        options.SkipMinorUpdates = SkipMinorUpdatesCheckBox.IsChecked ?? false;

        options.KillBeforeOperation.Clear();
        var killNames = (KillProcessesTextBox.Text ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var name in killNames)
        {
            if (!string.IsNullOrEmpty(name))
                options.KillBeforeOperation.Add(name);
        }

        options.OverridesNextLevelOpts = true;
        return options;
    }

    private static List<string> SplitParams(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return [.. text.Split(' ', StringSplitOptions.RemoveEmptyEntries)];
    }

    private async void SaveBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        var options = ReadOptionsFromUi();
        await InstallOptionsFactory.SaveForPackageAsync(options, _package);

        // Persist global kill-processes setting
        Settings.Set(Settings.K.KillProcessesThatRefuseToDie, KillProcessesThatWontDieCheckBox.IsChecked ?? false);

        // Persist ignored-updates status
        if (IgnoreUpdatesCheckBox.IsChecked ?? false)
        {
            await _package.AddToIgnoredUpdatesAsync(version: "*");
        }
        else
        {
            if (await _package.GetIgnoredUpdatesVersionAsync() == "*")
                await _package.RemoveFromIgnoredUpdatesAsync();
        }

        Close();
    }

    private void CancelBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
