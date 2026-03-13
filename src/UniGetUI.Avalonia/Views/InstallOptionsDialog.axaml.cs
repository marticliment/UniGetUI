using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Serializable;

namespace UniGetUI.Avalonia.Views;

public partial class InstallOptionsDialog : UserControl
{
    private IPackage? _package;
    private InstallOptions _options = new();
    private OperationType _currentProfile = OperationType.Install;

    public event EventHandler<InstallOptions>? OptionsSaved;

    public InstallOptionsDialog()
    {
        InitializeComponent();
    }

    public async Task Show(IPackage package, OperationType role)
    {
        _package = package;
        _currentProfile = role;

        DialogTitle.Text = $"Installation options for {package.Name}";
        DialogSubtitle.Text = package.Id;

        // Select profile
        ProfileCombo.SelectedIndex = role switch
        {
            OperationType.Update => 1,
            OperationType.Uninstall => 2,
            _ => 0,
        };

        // Load saved options
        _options = await InstallOptionsFactory.LoadForPackageAsync(package);

        ConfigureCapabilities();
        PopulateFromOptions();
        UpdateCommandPreview();

        Overlay.IsVisible = true;
    }

    public void Close()
    {
        Overlay.IsVisible = false;
    }

    private void ConfigureCapabilities()
    {
        if (_package is null) return;
        ManagerCapabilities caps = _package.Manager.Capabilities;

        OptAdmin.IsVisible = caps.CanRunAsAdmin;
        OptInteractive.IsVisible = caps.CanRunInteractively;
        OptSkipHash.IsVisible = caps.CanSkipIntegrityChecks;
        OptVersion.IsVisible = caps.SupportsCustomVersions || caps.SupportsPreRelease;
        OptPreRelease.IsVisible = caps.SupportsPreRelease;
        OptClearPrev.IsVisible = caps.CanUninstallPreviousVersionsAfterUpdate;
        OptRemoveData.IsVisible = caps.CanRemoveDataOnUninstall;
        OptLocation.IsVisible = caps.SupportsCustomLocations;

        // Architecture
        if (caps.SupportsCustomArchitectures)
        {
            OptArch.IsVisible = true;
            OptArchCombo.ItemsSource = new[] { "Default" }.Concat(caps.SupportedCustomArchitectures).ToList();
            OptArchCombo.SelectedIndex = 0;
        }
        else
        {
            OptArch.IsVisible = false;
        }

        // Scope
        if (caps.SupportsCustomScopes)
        {
            OptScope.IsVisible = true;
            OptScopeCombo.ItemsSource = new[] { "Default", "User", "Machine" };
            OptScopeCombo.SelectedIndex = 0;
        }
        else
        {
            OptScope.IsVisible = false;
        }

        // Load versions
        if (caps.SupportsCustomVersions)
            _ = LoadVersionsAsync();

        // Follow global toggle
        FollowGlobalToggle.IsChecked = !_options.OverridesNextLevelOpts;
        OptionsContainer.Opacity = _options.OverridesNextLevelOpts ? 1.0 : 0.5;
        OptionsContainer.IsEnabled = _options.OverridesNextLevelOpts;
    }

    private void PopulateFromOptions()
    {
        OptAdminToggle.IsChecked = _options.RunAsAdministrator;
        OptInteractiveToggle.IsChecked = _options.InteractiveInstallation;
        OptSkipHashToggle.IsChecked = _options.SkipHashCheck;
        OptPreReleaseToggle.IsChecked = _options.PreRelease;
        OptClearPrevToggle.IsChecked = _options.UninstallPreviousVersionsOnUpdate;
        OptRemoveDataToggle.IsChecked = _options.RemoveDataOnUninstall;
        OptSkipMinorToggle.IsChecked = _options.SkipMinorUpdates;
        OptAutoUpdateToggle.IsChecked = _options.AutoUpdatePackage;

        // Architecture
        if (!string.IsNullOrEmpty(_options.Architecture) && OptArchCombo.ItemsSource is IList<string> archItems)
        {
            var idx = archItems.IndexOf(_options.Architecture);
            if (idx >= 0) OptArchCombo.SelectedIndex = idx;
        }

        // Scope
        if (!string.IsNullOrEmpty(_options.InstallationScope) && OptScopeCombo.ItemsSource is IList<string> scopeItems)
        {
            var idx = scopeItems.IndexOf(_options.InstallationScope);
            if (idx >= 0) OptScopeCombo.SelectedIndex = idx;
        }

        // Location
        if (!string.IsNullOrEmpty(_options.CustomInstallLocation))
            OptLocationText.Text = _options.CustomInstallLocation;

        // Custom params
        OptInstallParams.Text = string.Join(" ", _options.CustomParameters_Install);
        OptUpdateParams.Text = string.Join(" ", _options.CustomParameters_Update);
        OptUninstallParams.Text = string.Join(" ", _options.CustomParameters_Uninstall);

        // Pre/post commands
        OptPreInstCmd.Text = _options.PreInstallCommand;
        OptPostInstCmd.Text = _options.PostInstallCommand;
        OptPreUpdCmd.Text = _options.PreUpdateCommand;
        OptPostUpdCmd.Text = _options.PostUpdateCommand;
        OptPreUninstCmd.Text = _options.PreUninstallCommand;
        OptPostUninstCmd.Text = _options.PostUninstallCommand;
    }

    private InstallOptions GatherFromUI()
    {
        var opts = _options.Copy();

        opts.RunAsAdministrator = OptAdminToggle.IsChecked == true;
        opts.InteractiveInstallation = OptInteractiveToggle.IsChecked == true;
        opts.SkipHashCheck = OptSkipHashToggle.IsChecked == true;
        opts.PreRelease = OptPreReleaseToggle.IsChecked == true;
        opts.UninstallPreviousVersionsOnUpdate = OptClearPrevToggle.IsChecked == true;
        opts.RemoveDataOnUninstall = OptRemoveDataToggle.IsChecked == true;
        opts.SkipMinorUpdates = OptSkipMinorToggle.IsChecked == true;
        opts.AutoUpdatePackage = OptAutoUpdateToggle.IsChecked == true;

        // Version
        if (OptVersionCombo.SelectedItem is string ver && ver != "Latest")
            opts.Version = ver;
        else
            opts.Version = "";

        // Architecture
        if (OptArchCombo.SelectedItem is string arch && arch != "Default")
            opts.Architecture = arch;
        else
            opts.Architecture = "";

        // Scope
        if (OptScopeCombo.SelectedItem is string scope && scope != "Default")
            opts.InstallationScope = scope;
        else
            opts.InstallationScope = "";

        // Location
        string location = OptLocationText.Text ?? "";
        opts.CustomInstallLocation = location == "Default" ? "" : location;

        // Custom params
        opts.CustomParameters_Install = SplitParams(OptInstallParams.Text);
        opts.CustomParameters_Update = SplitParams(OptUpdateParams.Text);
        opts.CustomParameters_Uninstall = SplitParams(OptUninstallParams.Text);

        // Pre/post commands
        opts.PreInstallCommand = OptPreInstCmd.Text ?? "";
        opts.PostInstallCommand = OptPostInstCmd.Text ?? "";
        opts.PreUpdateCommand = OptPreUpdCmd.Text ?? "";
        opts.PostUpdateCommand = OptPostUpdCmd.Text ?? "";
        opts.PreUninstallCommand = OptPreUninstCmd.Text ?? "";
        opts.PostUninstallCommand = OptPostUninstCmd.Text ?? "";

        opts.OverridesNextLevelOpts = FollowGlobalToggle.IsChecked != true;
        return opts;
    }

    private async Task LoadVersionsAsync()
    {
        if (_package is null) return;

        VersionLoadingBar.IsVisible = true;
        try
        {
            var versions = await Task.Run(() =>
                _package.Manager.DetailsHelper.GetVersions(_package));

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var items = new List<string> { "Latest" };
                items.AddRange(versions);
                OptVersionCombo.ItemsSource = items;

                if (!string.IsNullOrEmpty(_options.Version))
                {
                    var idx = items.IndexOf(_options.Version);
                    OptVersionCombo.SelectedIndex = idx >= 0 ? idx : 0;
                }
                else
                {
                    OptVersionCombo.SelectedIndex = 0;
                }
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load versions: {ex.Message}");
            OptVersionCombo.ItemsSource = new[] { "Latest" };
            OptVersionCombo.SelectedIndex = 0;
        }
        finally
        {
            VersionLoadingBar.IsVisible = false;
        }
    }

    private void UpdateCommandPreview()
    {
        if (_package is null) return;

        try
        {
            var opts = GatherFromUI();
            var parameters = _package.Manager.OperationHelper.GetParameters(_package, opts, _currentProfile);
            string exe = _package.Manager.Status.ExecutablePath ?? _package.Manager.Name;
            CommandPreviewText.Text = $"{exe} {string.Join(" ", parameters)}";
        }
        catch
        {
            CommandPreviewText.Text = "(unable to generate preview)";
        }
    }

    // ─── Event handlers ─────────────────────────────────────────────────

    private void ProfileCombo_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (ProfileCombo.SelectedItem is ComboBoxItem item)
        {
            _currentProfile = item.Tag?.ToString() switch
            {
                "Update" => OperationType.Update,
                "Uninstall" => OperationType.Uninstall,
                _ => OperationType.Install,
            };
            UpdateCommandPreview();
        }
    }

    private void FollowGlobalToggle_Changed(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        bool followGlobal = FollowGlobalToggle.IsChecked == true;
        OptionsContainer.Opacity = followGlobal ? 0.5 : 1.0;
        OptionsContainer.IsEnabled = !followGlobal;
    }

    private async void BrowseLocation_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select installation directory",
                AllowMultiple = false,
            });

        if (folders.Count > 0)
        {
            OptLocationText.Text = folders[0].Path.LocalPath;
        }
    }

    private async void Save_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_package is null) return;

        var opts = GatherFromUI();
        await InstallOptionsFactory.SaveForPackageAsync(opts, _package);
        _options = opts;
        OptionsSaved?.Invoke(this, opts);
        Close();
    }

    private void Cancel_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void Reset_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _options = new InstallOptions();
        PopulateFromOptions();
        FollowGlobalToggle.IsChecked = true;
    }

    // ─── Helpers ────────────────────────────────────────────────────────

    private static List<string> SplitParams(string? text) =>
        (text ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
}
