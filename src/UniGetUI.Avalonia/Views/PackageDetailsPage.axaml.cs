using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Serializable;
using UniGetUI.PackageOperations;

namespace UniGetUI.Avalonia.Views;

public partial class PackageDetailsPage : UserControl
{
    private readonly IPackage _package;
    private readonly OperationType _role;
    private InstallOptions _options = new();

    public Action<AbstractOperation>? OnOperationCreated { get; set; }
    public event EventHandler? CloseRequested;

    public PackageDetailsPage(IPackage package, OperationType role)
    {
        InitializeComponent();
        _package = package;
        _role = role;

        SetupHeader();
        SetupActionButtons();
        SetupInstallOptions();

        _ = LoadDetailsAsync();
        _ = LoadIconAsync();
        _ = LoadScreenshotsAsync();

        SizeChanged += OnSizeChanged;
    }

    private void SetupHeader()
    {
        PackageName.Text = _package.Name;
        PackageId.Text = _package.Id;
        PackageSource.Text = _package.Source.AsString_DisplayName;
        ManagerText.Text = _package.Manager.DisplayName;
        PackageIdDetail.Text = _package.Id;

        // Version rows
        if (_package.IsUpgradable)
        {
            InstalledVerRow.IsVisible = true;
            InstalledVerText.Text = _package.VersionString;
            AvailableVerRow.IsVisible = true;
            AvailableVerText.Text = _package.NewVersionString;
        }
        else if (!string.IsNullOrEmpty(_package.VersionString))
        {
            InstalledVerRow.IsVisible = true;
            InstalledVerText.Text = _package.VersionString;
        }
    }

    private void SetupActionButtons()
    {
        switch (_role)
        {
            case OperationType.Install:
                ActionButtonLabel.Text = "Install";
                ActionAdminLabel.Text = "Install as Admin";
                ActionIcon.Symbol = FluentAvalonia.UI.Controls.Symbol.Download;
                break;
            case OperationType.Update:
                ActionButtonLabel.Text = "Update";
                ActionAdminLabel.Text = "Update as Admin";
                ActionIcon.Symbol = FluentAvalonia.UI.Controls.Symbol.Up;
                break;
            case OperationType.Uninstall:
                ActionButtonLabel.Text = "Uninstall";
                ActionAdminLabel.Text = "Uninstall as Admin";
                ActionIcon.Symbol = FluentAvalonia.UI.Controls.Symbol.Delete;
                break;
            default:
                ActionButtonLabel.Text = "Install";
                ActionAdminLabel.Text = "Install as Admin";
                ActionIcon.Symbol = FluentAvalonia.UI.Controls.Symbol.Download;
                break;
        }

        var caps = _package.Manager.Capabilities;
        ActionAdminButton.IsVisible = caps.CanRunAsAdmin;
    }

    private void SetupInstallOptions()
    {
        var caps = _package.Manager.Capabilities;

        // Version selector
        if (caps.SupportsCustomVersions || caps.SupportsPreRelease)
        {
            VersionRow.IsVisible = true;
            _ = LoadVersionsAsync();
        }

        // Architecture
        if (caps.SupportsCustomArchitectures)
        {
            ArchRow.IsVisible = true;
            ArchCombo.ItemsSource = new[] { "Default" }.Concat(caps.SupportedCustomArchitectures).ToList();
            ArchCombo.SelectedIndex = 0;
        }

        // Scope
        if (caps.SupportsCustomScopes)
        {
            ScopeRow.IsVisible = true;
            ScopeCombo.ItemsSource = new[] { "Default", "User", "Machine" };
            ScopeCombo.SelectedIndex = 0;
        }

        // Skip hash check
        SkipHashRow.IsVisible = caps.CanSkipIntegrityChecks && _role != OperationType.Uninstall;

        // Interactive
        InteractiveRow.IsVisible = caps.CanRunInteractively;

        // Run as admin
        AdminRow.IsVisible = caps.CanRunAsAdmin;
    }

    private async Task LoadVersionsAsync()
    {
        try
        {
            var versions = await Task.Run(() =>
                _package.Manager.DetailsHelper.GetVersions(_package));

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var items = new List<string> { "Latest" };
                items.AddRange(versions);
                VersionCombo.ItemsSource = items;
                VersionCombo.SelectedIndex = 0;
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load versions for {_package.Id}");
            Logger.Error(ex);
        }
    }

    private async Task LoadDetailsAsync()
    {
        try
        {
            var details = _package.Details;
            if (!details.IsPopulated)
                await details.Load();

            // Also load saved options
            _options = await InstallOptionsFactory.LoadForPackageAsync(_package);

            await Dispatcher.UIThread.InvokeAsync(() => PopulateDetails(details));
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load details for {_package.Id}");
            Logger.Error(ex);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                DescriptionText.Text = "Failed to load package details.";
                LoadingOverlay.IsVisible = false;
            });
        }
    }

    private void PopulateDetails(IPackageDetails details)
    {
        // Description
        DescriptionText.Text = !string.IsNullOrEmpty(details.Description)
            ? details.Description
            : "No description available.";

        // Homepage
        if (details.HomepageUrl is not null)
        {
            HomepageRow.IsVisible = true;
            HomepageText.Text = details.HomepageUrl.ToString();
            HomepageText.Tag = details.HomepageUrl;
        }

        // Publisher
        if (!string.IsNullOrEmpty(details.Publisher))
        {
            PublisherRow.IsVisible = true;
            PublisherText.Text = details.Publisher;
        }

        // Author
        if (!string.IsNullOrEmpty(details.Author))
        {
            AuthorRow.IsVisible = true;
            AuthorText.Text = details.Author;
        }

        // License
        if (!string.IsNullOrEmpty(details.License) || details.LicenseUrl is not null)
        {
            LicenseRow.IsVisible = true;
            LicenseText.Text = details.License ?? "";
            if (details.LicenseUrl is not null)
            {
                LicenseUrlText.IsVisible = true;
                LicenseUrlText.Text = "View license";
                LicenseUrlText.Tag = details.LicenseUrl;
            }
        }

        // Update date
        if (!string.IsNullOrEmpty(details.UpdateDate))
        {
            UpdateDateRow.IsVisible = true;
            UpdateDateText.Text = details.UpdateDate;
        }

        // Manifest URL
        if (details.ManifestUrl is not null)
        {
            ManifestRow.IsVisible = true;
            ManifestText.Text = details.ManifestUrl.ToString();
            ManifestText.Tag = details.ManifestUrl;
        }

        // Installer type
        if (!string.IsNullOrEmpty(details.InstallerType))
        {
            InstallerTypeRow.IsVisible = true;
            InstallerTypeText.Text = details.InstallerType;
        }

        // Installer URL
        if (details.InstallerUrl is not null)
        {
            InstallerUrlRow.IsVisible = true;
            InstallerUrlText.Text = details.InstallerUrl.ToString();
            InstallerUrlText.Tag = details.InstallerUrl;
        }

        // Installer hash
        if (!string.IsNullOrEmpty(details.InstallerHash))
        {
            InstallerHashRow.IsVisible = true;
            InstallerHashText.Text = details.InstallerHash;
        }

        // Installer size
        if (details.InstallerSize > 0)
        {
            InstallerSizeRow.IsVisible = true;
            InstallerSizeText.Text = FormatSize(details.InstallerSize);
        }

        // Release notes
        if (!string.IsNullOrEmpty(details.ReleaseNotes))
        {
            ReleaseNotesRow.IsVisible = true;
            ReleaseNotesText.Text = details.ReleaseNotes;
        }

        // Release notes URL
        if (details.ReleaseNotesUrl is not null)
        {
            ReleaseNotesUrlRow.IsVisible = true;
            ReleaseNotesUrlText.Text = details.ReleaseNotesUrl.ToString();
            ReleaseNotesUrlText.Tag = details.ReleaseNotesUrl;
        }

        // Tags
        if (details.Tags is { Length: > 0 })
        {
            TagsList.IsVisible = true;
            var tagItems = new List<Border>();
            foreach (var tag in details.Tags)
            {
                var border = new Border
                {
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 3),
                    Margin = new Thickness(0, 0, 6, 6),
                    Background = global::Avalonia.Media.Brush.Parse("#20808080"),
                    Child = new TextBlock { Text = tag, FontSize = 11 }
                };
                tagItems.Add(border);
            }
            TagsList.ItemsSource = tagItems;
        }

        // Dependencies
        if (details.Dependencies is { Count: > 0 })
        {
            DependenciesPanel.IsVisible = true;
            var deps = details.Dependencies.Select(d => new
            {
                Icon = d.Mandatory ? "●" : "○",
                d.Name,
                d.Version
            }).ToList();
            DependenciesList.ItemsSource = deps;
        }

        // Populate saved options into UI
        PopulateSavedOptions();

        LoadingOverlay.IsVisible = false;
    }

    private void PopulateSavedOptions()
    {
        if (SkipHashToggle is not null)
            SkipHashToggle.IsChecked = _options.SkipHashCheck;
        if (InteractiveToggle is not null)
            InteractiveToggle.IsChecked = _options.InteractiveInstallation;
        if (AdminToggle is not null)
            AdminToggle.IsChecked = _options.RunAsAdministrator;

        if (!string.IsNullOrEmpty(_options.Version) && VersionCombo.ItemsSource is IList<string> items)
        {
            var idx = items.IndexOf(_options.Version);
            if (idx >= 0) VersionCombo.SelectedIndex = idx;
        }

        if (!string.IsNullOrEmpty(_options.Architecture) && ArchCombo.ItemsSource is IList<string> archItems)
        {
            var idx = archItems.IndexOf(_options.Architecture);
            if (idx >= 0) ArchCombo.SelectedIndex = idx;
        }

        if (!string.IsNullOrEmpty(_options.InstallationScope) && ScopeCombo.ItemsSource is IList<string> scopeItems)
        {
            var scope = _options.InstallationScope;
            var idx = scopeItems.IndexOf(scope);
            if (idx >= 0) ScopeCombo.SelectedIndex = idx;
        }

        // Custom params for the current role
        var paramsList = _role switch
        {
            OperationType.Update => _options.CustomParameters_Update,
            OperationType.Uninstall => _options.CustomParameters_Uninstall,
            _ => _options.CustomParameters_Install,
        };
        if (paramsList.Count > 0)
            CustomParamsBox.Text = string.Join(" ", paramsList);
    }

    private InstallOptions GatherOptionsFromUI()
    {
        var opts = _options.Copy();

        opts.SkipHashCheck = SkipHashToggle.IsChecked == true;
        opts.InteractiveInstallation = InteractiveToggle.IsChecked == true;
        opts.RunAsAdministrator = AdminToggle.IsChecked == true;

        // Version
        if (VersionCombo.SelectedItem is string ver && ver != "Latest")
            opts.Version = ver;
        else
            opts.Version = "";

        // Architecture
        if (ArchCombo.SelectedItem is string arch && arch != "Default")
            opts.Architecture = arch;
        else
            opts.Architecture = "";

        // Scope
        if (ScopeCombo.SelectedItem is string scope && scope != "Default")
            opts.InstallationScope = scope;
        else
            opts.InstallationScope = "";

        // Custom params
        var paramsText = CustomParamsBox.Text ?? "";
        var paramsList = paramsText.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        switch (_role)
        {
            case OperationType.Update:
                opts.CustomParameters_Update = paramsList;
                break;
            case OperationType.Uninstall:
                opts.CustomParameters_Uninstall = paramsList;
                break;
            default:
                opts.CustomParameters_Install = paramsList;
                break;
        }

        opts.OverridesNextLevelOpts = opts.DiffersFromDefault();
        return opts;
    }

    private async Task LoadIconAsync()
    {
        try
        {
            var iconUri = await Task.Run(() => _package.GetIconUrl());
            if (iconUri.Scheme == "file" && System.IO.File.Exists(iconUri.LocalPath))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    PackageIcon.Source = new Bitmap(iconUri.LocalPath);
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load icon for {_package.Id}: {ex.Message}");
        }
    }

    private async Task LoadScreenshotsAsync()
    {
        try
        {
            var screenshots = await Task.Run(() => _package.GetScreenshots());
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (screenshots.Count == 0) return;

                foreach (var uri in screenshots)
                {
                    var image = new Image
                    {
                        Height = 200,
                        Stretch = global::Avalonia.Media.Stretch.Uniform,
                    };

                    // Load async - use uri directly if http/https
                    _ = LoadScreenshotImageAsync(image, uri);

                    ScreenshotPanelInline.Children.Add(image);
                    // Clone for wide panel
                    var imageWide = new Image
                    {
                        Height = 250,
                        Stretch = global::Avalonia.Media.Stretch.Uniform,
                    };
                    _ = LoadScreenshotImageAsync(imageWide, uri);
                    ScreenshotPanelWide.Children.Add(imageWide);
                }

                ScreenshotsInline.IsVisible = true;
                ScreenshotsWide.IsVisible = true;
                NoScreenshotsText.IsVisible = false;
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load screenshots for {_package.Id}: {ex.Message}");
        }
    }

    private static async Task LoadScreenshotImageAsync(Image image, Uri uri)
    {
        try
        {
            if (uri.Scheme == "file" && System.IO.File.Exists(uri.LocalPath))
            {
                image.Source = new Bitmap(uri.LocalPath);
            }
            else
            {
                using var httpClient = new System.Net.Http.HttpClient();
                var data = await httpClient.GetByteArrayAsync(uri);
                using var stream = new System.IO.MemoryStream(data);
                image.Source = new Bitmap(stream);
            }
        }
        catch
        {
            // Silently fail - screenshot not critical
        }
    }

    // ─── Layout: responsive two-column ──────────────────────────────────

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        bool wide = e.NewSize.Width >= 950;
        var rightColDef = LayoutRoot.ColumnDefinitions[1];

        if (wide)
        {
            rightColDef.Width = new GridLength(1, GridUnitType.Star);
            RightPanel.IsVisible = true;
            ExtendedDetailsInline.IsVisible = false;
            ScreenshotsInline.IsVisible = false;
        }
        else
        {
            rightColDef.Width = new GridLength(0);
            RightPanel.IsVisible = false;
            ExtendedDetailsInline.IsVisible = true;
            if (ScreenshotPanelInline.Children.Count > 0)
                ScreenshotsInline.IsVisible = true;
        }
    }

    // ─── Action handlers ────────────────────────────────────────────────

    private void ActionButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        DoAction(elevated: false);
    }

    private void ActionAdminButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        DoAction(elevated: true);
    }

    private void DoAction(bool elevated)
    {
        var options = GatherOptionsFromUI();
        if (elevated) options.RunAsAdministrator = true;

        AbstractOperation operation = _role switch
        {
            OperationType.Install => new InstallPackageOperation(_package, options),
            OperationType.Update => new UpdatePackageOperation(_package, options),
            OperationType.Uninstall => new UninstallPackageOperation(_package, options),
            _ => new InstallPackageOperation(_package, options),
        };

        OnOperationCreated?.Invoke(operation);
    }

    private void OptionsButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        InstallOptionsExpander.IsExpanded = !InstallOptionsExpander.IsExpanded;
    }

    private async void SaveOptionsButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var opts = GatherOptionsFromUI();
        await InstallOptionsFactory.SaveForPackageAsync(opts, _package);
        _options = opts;
    }

    // ─── Link handlers ──────────────────────────────────────────────────

    private void HomepageLink_Click(object? sender, PointerPressedEventArgs e) =>
        OpenUri(HomepageText.Tag as Uri);

    private void LicenseLink_Click(object? sender, PointerPressedEventArgs e) =>
        OpenUri(LicenseUrlText.Tag as Uri);

    private void ManifestLink_Click(object? sender, PointerPressedEventArgs e) =>
        OpenUri(ManifestText.Tag as Uri);

    private void InstallerUrlLink_Click(object? sender, PointerPressedEventArgs e) =>
        OpenUri(InstallerUrlText.Tag as Uri);

    private void ReleaseNotesUrlLink_Click(object? sender, PointerPressedEventArgs e) =>
        OpenUri(ReleaseNotesUrlText.Tag as Uri);

    private static void OpenUri(Uri? uri)
    {
        if (uri is null) return;
        try
        {
            Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to open URI: {uri}");
            Logger.Error(ex);
        }
    }

    // ─── Helpers ────────────────────────────────────────────────────────

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }
}
