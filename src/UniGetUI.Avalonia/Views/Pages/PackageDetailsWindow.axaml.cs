using System.Diagnostics;
using System.Net.Http;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using UniGetUI.Avalonia.Infrastructure;
using UniGetUI.Avalonia.Models;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageOperations;

namespace UniGetUI.Avalonia.Views.Pages;

public partial class PackageDetailsWindow : Window
{
    private readonly IPackage _package;
    private readonly PackagePageMode _pageMode;
    private Uri? _homepageUrl;
    private Uri? _releaseNotesUrl;
    private Uri? _manifestUrl;

    private TextBlock PackageVersionLabelText => GetControl<TextBlock>("PackageVersionLabelBlock");
    private TextBlock PackageManagerLabelText => GetControl<TextBlock>("PackageManagerLabelBlock");
    private TextBlock PackageNameText => GetControl<TextBlock>("PackageNameBlock");
    private TextBlock PackageIdText => GetControl<TextBlock>("PackageIdBlock");
    private TextBlock PackageVersionText => GetControl<TextBlock>("PackageVersionBlock");
    private TextBlock PackageManagerText => GetControl<TextBlock>("PackageManagerBlock");
    private Border LoadingCardControl => GetControl<Border>("LoadingCard");
    private TextBlock LoadingStateText => GetControl<TextBlock>("LoadingStateBlock");
    private Border DescriptionCardControl => GetControl<Border>("DescriptionCard");
    private TextBlock DescriptionTitleText => GetControl<TextBlock>("DescriptionTitleBlock");
    private TextBlock DescriptionText => GetControl<TextBlock>("DescriptionBlock");
    private Border InfoCardControl => GetControl<Border>("InfoCard");
    private TextBlock InfoTitleText => GetControl<TextBlock>("InfoTitleBlock");
    private Grid AuthorRowControl => GetControl<Grid>("AuthorRow");
    private TextBlock AuthorLabelText => GetControl<TextBlock>("AuthorLabelBlock");
    private TextBlock AuthorValueText => GetControl<TextBlock>("AuthorValueBlock");
    private Grid PublisherRowControl => GetControl<Grid>("PublisherRow");
    private TextBlock PublisherLabelText => GetControl<TextBlock>("PublisherLabelBlock");
    private TextBlock PublisherValueText => GetControl<TextBlock>("PublisherValueBlock");
    private Grid LicenseRowControl => GetControl<Grid>("LicenseRow");
    private TextBlock LicenseLabelText => GetControl<TextBlock>("LicenseLabelBlock");
    private TextBlock LicenseValueText => GetControl<TextBlock>("LicenseValueBlock");
    private Grid UpdateDateRowControl => GetControl<Grid>("UpdateDateRow");
    private TextBlock UpdateDateLabelText => GetControl<TextBlock>("UpdateDateLabelBlock");
    private TextBlock UpdateDateValueText => GetControl<TextBlock>("UpdateDateValueBlock");
    private Grid InstallerTypeRowControl => GetControl<Grid>("InstallerTypeRow");
    private TextBlock InstallerTypeLabelText => GetControl<TextBlock>("InstallerTypeLabelBlock");
    private TextBlock InstallerTypeValueText => GetControl<TextBlock>("InstallerTypeValueBlock");
    private Border LinksCardControl => GetControl<Border>("LinksCard");
    private TextBlock LinksTitleText => GetControl<TextBlock>("LinksTitleBlock");
    private Button HomepageButtonControl => GetControl<Button>("HomepageButton");
    private Button ReleaseNotesUrlButtonControl => GetControl<Button>("ReleaseNotesUrlButton");
    private Button ManifestUrlButtonControl => GetControl<Button>("ManifestUrlButton");
    private Border ReleaseNotesCardControl => GetControl<Border>("ReleaseNotesCard");
    private TextBlock ReleaseNotesTitleText => GetControl<TextBlock>("ReleaseNotesTitleBlock");
    private TextBlock ReleaseNotesText => GetControl<TextBlock>("ReleaseNotesBlock");
    private Button CloseButtonControl => GetControl<Button>("CloseButton");
    private Button ActionButtonControl => GetControl<Button>("ActionButton");
    private Button ActionDropdownButtonControl => GetControl<Button>("ActionDropdownButton");
    private Image PackageIconImageControl => GetControl<Image>("PackageIconImage");
    private TextBlock PackageIconGlyphControl => GetControl<TextBlock>("PackageIconGlyphBlock");
    private Border InstallerCardControl => GetControl<Border>("InstallerCard");
    private TextBlock InstallerTitleText => GetControl<TextBlock>("InstallerTitleBlock");
    private Grid InstallerHashRowControl => GetControl<Grid>("InstallerHashRow");
    private TextBlock InstallerHashLabelText => GetControl<TextBlock>("InstallerHashLabelBlock");
    private TextBlock InstallerHashValueText => GetControl<TextBlock>("InstallerHashValueBlock");
    private Grid InstallerUrlRowControl => GetControl<Grid>("InstallerUrlRow");
    private TextBlock InstallerUrlLabelText => GetControl<TextBlock>("InstallerUrlLabelBlock");
    private TextBlock InstallerUrlValueText => GetControl<TextBlock>("InstallerUrlValueBlock");
    private Grid InstallerSizeRowControl => GetControl<Grid>("InstallerSizeRow");
    private TextBlock InstallerSizeLabelText => GetControl<TextBlock>("InstallerSizeLabelBlock");
    private TextBlock InstallerSizeValueText => GetControl<TextBlock>("InstallerSizeValueBlock");
    private Button DownloadInstallerButtonControl => GetControl<Button>("DownloadInstallerButton");
    private Border DependenciesCardControl => GetControl<Border>("DependenciesCard");
    private TextBlock DependenciesTitleText => GetControl<TextBlock>("DependenciesTitleBlock");
    private StackPanel DependenciesPanelControl => GetControl<StackPanel>("DependenciesPanel");

    public PackageDetailsWindow(IPackage package, PackagePageMode pageMode)
    {
        _package = package;
        _pageMode = pageMode;
        InitializeComponent();
        ApplyStaticContent(package);
        _ = LoadDetailsAsync();
    }

    private void ApplyStaticContent(IPackage package)
    {
        Title = package.Name + " — " + CoreTools.Translate("Details");

        PackageNameText.Text = package.Name;
        PackageIdText.Text = package.Id;
        PackageVersionText.Text = string.IsNullOrWhiteSpace(package.VersionString)
            ? CoreTools.Translate("Unknown")
            : package.VersionString;
        PackageManagerText.Text = package.Source.AsString_DisplayName;

        PackageVersionLabelText.Text = CoreTools.Translate("Version:");
        PackageManagerLabelText.Text = CoreTools.Translate("Manager:");

        if (_pageMode != PackagePageMode.None)
        {
            ActionButtonControl.Content = _pageMode switch
            {
                PackagePageMode.Discover => CoreTools.Translate("Install"),
                PackagePageMode.Updates => CoreTools.Translate("Update"),
                PackagePageMode.Installed => CoreTools.Translate("Uninstall"),
                _ => string.Empty,
            };
            ActionButtonControl.IsVisible = true;
            // ActionDropdownButton visibility is set by BuildActionFlyout() after capability check
        }

        LoadingStateText.Text = CoreTools.Translate("Loading package details...");
        CloseButtonControl.Content = CoreTools.Translate("Close");

        DescriptionTitleText.Text = CoreTools.Translate("Description");
        InfoTitleText.Text = CoreTools.Translate("Package Information");
        LinksTitleText.Text = CoreTools.Translate("Links");
        ReleaseNotesTitleText.Text = CoreTools.Translate("Release Notes");

        AuthorLabelText.Text = CoreTools.Translate("Author:");
        PublisherLabelText.Text = CoreTools.Translate("Publisher:");
        LicenseLabelText.Text = CoreTools.Translate("License:");
        UpdateDateLabelText.Text = CoreTools.Translate("Last updated:");
        InstallerTypeLabelText.Text = CoreTools.Translate("Installer type:");

        HomepageButtonControl.Content = CoreTools.Translate("Open homepage");
        ReleaseNotesUrlButtonControl.Content = CoreTools.Translate("Open release notes");
        ManifestUrlButtonControl.Content = CoreTools.Translate("View package manifest");

        // Installer card
        InstallerTitleText.Text = CoreTools.Translate("Installer");
        InstallerHashLabelText.Text = CoreTools.Translate("Hash:");
        InstallerUrlLabelText.Text = CoreTools.Translate("URL:");
        InstallerSizeLabelText.Text = CoreTools.Translate("Size:");
        DownloadInstallerButtonControl.Content = CoreTools.Translate("Download installer");
        if (_package.Manager.Capabilities.CanDownloadInstaller)
            DownloadInstallerButtonControl.IsVisible = true;

        // Dependencies card
        DependenciesTitleText.Text = CoreTools.Translate("Dependencies");

        // Action dropdown flyout
        BuildActionFlyout();
    }

    private async Task LoadDetailsAsync()
    {
        try
        {
            var details = _package.Details;
            if (!details.IsPopulated)
            {
                await details.Load();
            }

            Dispatcher.UIThread.Post(() => PopulateDetails(details));
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            Dispatcher.UIThread.Post(() =>
            {
                LoadingCardControl.IsVisible = true;
                LoadingStateText.Text = CoreTools.Translate("Failed to load details: {0}", ex.Message);
            });
        }

        _ = LoadIconAsync();
    }

    private void PopulateDetails(IPackageDetails details)
    {
        LoadingCardControl.IsVisible = false;

        if (!string.IsNullOrWhiteSpace(details.Description))
        {
            DescriptionText.Text = details.Description;
            DescriptionCardControl.IsVisible = true;
        }

        bool anyInfoRow = false;

        if (!string.IsNullOrWhiteSpace(details.Author))
        {
            AuthorValueText.Text = details.Author;
            AuthorRowControl.IsVisible = true;
            anyInfoRow = true;
        }

        if (!string.IsNullOrWhiteSpace(details.Publisher))
        {
            PublisherValueText.Text = details.Publisher;
            PublisherRowControl.IsVisible = true;
            anyInfoRow = true;
        }

        if (!string.IsNullOrWhiteSpace(details.License))
        {
            LicenseValueText.Text = details.LicenseUrl is not null
                ? $"{details.License} ({details.LicenseUrl})"
                : details.License;
            LicenseRowControl.IsVisible = true;
            anyInfoRow = true;
        }

        if (!string.IsNullOrWhiteSpace(details.UpdateDate))
        {
            UpdateDateValueText.Text = details.UpdateDate;
            UpdateDateRowControl.IsVisible = true;
            anyInfoRow = true;
        }

        if (!string.IsNullOrWhiteSpace(details.InstallerType))
        {
            InstallerTypeValueText.Text = details.InstallerType;
            InstallerTypeRowControl.IsVisible = true;
            anyInfoRow = true;
        }

        if (anyInfoRow)
        {
            InfoCardControl.IsVisible = true;
        }

        bool anyLink = false;

        if (details.HomepageUrl is not null)
        {
            _homepageUrl = details.HomepageUrl;
            HomepageButtonControl.IsVisible = true;
            anyLink = true;
        }

        if (details.ReleaseNotesUrl is not null)
        {
            _releaseNotesUrl = details.ReleaseNotesUrl;
            ReleaseNotesUrlButtonControl.IsVisible = true;
            anyLink = true;
        }

        if (details.ManifestUrl is not null)
        {
            _manifestUrl = details.ManifestUrl;
            ManifestUrlButtonControl.IsVisible = true;
            anyLink = true;
        }

        if (anyLink)
        {
            LinksCardControl.IsVisible = true;
        }

        if (!string.IsNullOrWhiteSpace(details.ReleaseNotes))
        {
            ReleaseNotesText.Text = details.ReleaseNotes;
            ReleaseNotesCardControl.IsVisible = true;
        }

        // Installer card
        bool anyInstallerRow = false;

        if (!string.IsNullOrWhiteSpace(details.InstallerHash))
        {
            InstallerHashValueText.Text = details.InstallerHash;
            InstallerHashRowControl.IsVisible = true;
            anyInstallerRow = true;
        }

        if (details.InstallerUrl is not null)
        {
            InstallerUrlValueText.Text = details.InstallerUrl.ToString();
            InstallerUrlRowControl.IsVisible = true;
            anyInstallerRow = true;
        }

        if (details.InstallerSize > 0)
        {
            InstallerSizeValueText.Text = CoreTools.FormatAsSize(details.InstallerSize);
            InstallerSizeRowControl.IsVisible = true;
            anyInstallerRow = true;
        }

        if (anyInstallerRow || _package.Manager.Capabilities.CanDownloadInstaller)
            InstallerCardControl.IsVisible = true;

        // Dependencies
        if (details.Dependencies.Count > 0)
        {
            foreach (var dep in details.Dependencies)
            {
                string label = dep.Name;
                if (!string.IsNullOrWhiteSpace(dep.Version))
                    label += " " + dep.Version;
                if (!dep.Mandatory)
                    label += " " + CoreTools.Translate("(optional)");

                DependenciesPanelControl.Children.Add(new TextBlock
                {
                    Text = label,
                    Opacity = 0.82,
                    TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                });
            }
            DependenciesCardControl.IsVisible = true;
        }
    }

    private void HomepageButton_OnClick(object? sender, RoutedEventArgs e)
        => OpenUrl(_homepageUrl?.ToString());

    private void ReleaseNotesUrlButton_OnClick(object? sender, RoutedEventArgs e)
        => OpenUrl(_releaseNotesUrl?.ToString());

    private void ManifestUrlButton_OnClick(object? sender, RoutedEventArgs e)
        => OpenUrl(_manifestUrl?.ToString());

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
        => Close();

    private async void ActionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ActionButtonControl.IsEnabled = false;
        try
        {
            var options = await InstallOptionsFactory.LoadApplicableAsync(_package);
            AbstractOperation? operation = _pageMode switch
            {
                PackagePageMode.Discover => new InstallPackageOperation(_package, options),
                PackagePageMode.Updates => new UpdatePackageOperation(_package, options),
                PackagePageMode.Installed => new UninstallPackageOperation(_package, options),
                _ => null,
            };
            if (operation is null) return;
            AvaloniaOperationRegistry.Add(operation);
            _ = operation.MainThread();
            Close();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            ActionButtonControl.IsEnabled = true;
        }
    }

    private async void DownloadInstallerButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!_package.Manager.Capabilities.CanDownloadInstaller) return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = CoreTools.Translate("Download installer"),
            SuggestedFileName = _package.Id,
            DefaultExtension = "exe",
            FileTypeChoices =
            [
                new FilePickerFileType("Executable") { Patterns = ["*.exe"] },
                new FilePickerFileType("MSI") { Patterns = ["*.msi"] },
                new FilePickerFileType("Compressed file") { Patterns = ["*.zip"] },
                new FilePickerFileType("MSIX") { Patterns = ["*.msix"] },
                new FilePickerFileType("NuGet package") { Patterns = ["*.nupkg"] },
            ],
        });
        if (file is null) return;

        var op = new DownloadOperation(_package, file.Path.LocalPath);
        AvaloniaOperationRegistry.Add(op);
        _ = op.MainThread();
        Close();
    }

    private async Task LoadIconAsync()
    {
        try
        {
            var iconUri = _package.GetIconUrlIfAny();
            if (iconUri is null || iconUri.Scheme == "ms-appx") return;

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var bytes = await client.GetByteArrayAsync(iconUri);
            using var ms = new MemoryStream(bytes);
            var bitmap = new Bitmap(ms);

            Dispatcher.UIThread.Post(() =>
            {
                PackageIconImageControl.Source = bitmap;
                PackageIconImageControl.IsVisible = true;
                PackageIconGlyphControl.IsVisible = false;
            });
        }
        catch
        {
            // ignore — glyph fallback stays visible
        }
    }

    private void BuildActionFlyout()
    {
        if (_pageMode == PackagePageMode.None) return;

        var flyout = new MenuFlyout();
        var caps = _package.Manager.Capabilities;

        // Admin variant
        if (caps.CanRunAsAdmin)
        {
            string label = _pageMode switch
            {
                PackagePageMode.Discover => CoreTools.Translate("Install as administrator"),
                PackagePageMode.Updates => CoreTools.Translate("Update as administrator"),
                PackagePageMode.Installed => CoreTools.Translate("Uninstall as administrator"),
                _ => CoreTools.Translate("Run as administrator"),
            };
            var item = new MenuItem { Header = label };
            item.Click += async (_, _) =>
            {
                ActionButtonControl.IsEnabled = false;
                ActionDropdownButtonControl.IsEnabled = false;
                try
                {
                    var options = await InstallOptionsFactory.LoadApplicableAsync(_package);
                    options.RunAsAdministrator = true;
                    AbstractOperation? op = _pageMode switch
                    {
                        PackagePageMode.Discover => new InstallPackageOperation(_package, options),
                        PackagePageMode.Updates => new UpdatePackageOperation(_package, options),
                        PackagePageMode.Installed => new UninstallPackageOperation(_package, options),
                        _ => null,
                    };
                    if (op is null) return;
                    AvaloniaOperationRegistry.Add(op);
                    _ = op.MainThread();
                    Close();
                }
                catch (Exception ex) { Logger.Error(ex); ActionButtonControl.IsEnabled = true; ActionDropdownButtonControl.IsEnabled = true; }
            };
            flyout.Items.Add(item);
        }

        // Interactive variant
        if (caps.CanRunInteractively && _pageMode != PackagePageMode.Installed)
        {
            string label = _pageMode switch
            {
                PackagePageMode.Discover => CoreTools.Translate("Interactive installation"),
                PackagePageMode.Updates => CoreTools.Translate("Interactive update"),
                _ => CoreTools.Translate("Interactive installation"),
            };
            var item = new MenuItem { Header = label };
            item.Click += async (_, _) =>
            {
                ActionButtonControl.IsEnabled = false;
                ActionDropdownButtonControl.IsEnabled = false;
                try
                {
                    var options = await InstallOptionsFactory.LoadApplicableAsync(_package);
                    options.InteractiveInstallation = true;
                    AbstractOperation? op = _pageMode switch
                    {
                        PackagePageMode.Discover => new InstallPackageOperation(_package, options),
                        PackagePageMode.Updates => new UpdatePackageOperation(_package, options),
                        _ => null,
                    };
                    if (op is null) return;
                    AvaloniaOperationRegistry.Add(op);
                    _ = op.MainThread();
                    Close();
                }
                catch (Exception ex) { Logger.Error(ex); ActionButtonControl.IsEnabled = true; ActionDropdownButtonControl.IsEnabled = true; }
            };
            flyout.Items.Add(item);
        }

        // Skip integrity checks (non-Installed pages)
        if (caps.CanSkipIntegrityChecks && _pageMode != PackagePageMode.Installed)
        {
            var item = new MenuItem { Header = CoreTools.Translate("Skip integrity checks") };
            item.Click += async (_, _) =>
            {
                ActionButtonControl.IsEnabled = false;
                ActionDropdownButtonControl.IsEnabled = false;
                try
                {
                    var options = await InstallOptionsFactory.LoadApplicableAsync(_package);
                    options.SkipHashCheck = true;
                    AbstractOperation? op = _pageMode switch
                    {
                        PackagePageMode.Discover => new InstallPackageOperation(_package, options),
                        PackagePageMode.Updates => new UpdatePackageOperation(_package, options),
                        _ => null,
                    };
                    if (op is null) return;
                    AvaloniaOperationRegistry.Add(op);
                    _ = op.MainThread();
                    Close();
                }
                catch (Exception ex) { Logger.Error(ex); ActionButtonControl.IsEnabled = true; ActionDropdownButtonControl.IsEnabled = true; }
            };
            flyout.Items.Add(item);
        }

        // Cross-op actions
        if (_pageMode == PackagePageMode.Discover)
        {
            if (_package.GetInstalledPackages().Count > 0)
            {
                if (flyout.Items.Count > 0) flyout.Items.Add(new Separator());
                var item = new MenuItem { Header = CoreTools.Translate("Uninstall") };
                item.Click += async (_, _) =>
                {
                    ActionButtonControl.IsEnabled = false;
                    try
                    {
                        var installed = _package.GetInstalledPackages()[0];
                        var options = await InstallOptionsFactory.LoadApplicableAsync(installed);
                        var op = new UninstallPackageOperation(installed, options);
                        AvaloniaOperationRegistry.Add(op);
                        _ = op.MainThread();
                        Close();
                    }
                    catch (Exception ex) { Logger.Error(ex); ActionButtonControl.IsEnabled = true; }
                };
                flyout.Items.Add(item);
            }
        }
        else if (_pageMode == PackagePageMode.Updates)
        {
            if (flyout.Items.Count > 0) flyout.Items.Add(new Separator());

            var uninstallItem = new MenuItem { Header = CoreTools.Translate("Uninstall package") };
            uninstallItem.Click += async (_, _) =>
            {
                ActionButtonControl.IsEnabled = false;
                try
                {
                    var options = await InstallOptionsFactory.LoadApplicableAsync(_package);
                    var op = new UninstallPackageOperation(_package, options);
                    AvaloniaOperationRegistry.Add(op);
                    _ = op.MainThread();
                    Close();
                }
                catch (Exception ex) { Logger.Error(ex); ActionButtonControl.IsEnabled = true; }
            };
            flyout.Items.Add(uninstallItem);

            if (!_package.Source.IsVirtualManager)
            {
                var reinstallItem = new MenuItem { Header = CoreTools.Translate("Reinstall package") };
                reinstallItem.Click += async (_, _) =>
                {
                    ActionButtonControl.IsEnabled = false;
                    try
                    {
                        var options = await InstallOptionsFactory.LoadApplicableAsync(_package);
                        var op = new InstallPackageOperation(_package, options);
                        AvaloniaOperationRegistry.Add(op);
                        _ = op.MainThread();
                        Close();
                    }
                    catch (Exception ex) { Logger.Error(ex); ActionButtonControl.IsEnabled = true; }
                };
                flyout.Items.Add(reinstallItem);
            }
        }
        else if (_pageMode == PackagePageMode.Installed)
        {
            var upgradable = _package.GetUpgradablePackage();
            if (upgradable is not null)
            {
                if (flyout.Items.Count > 0) flyout.Items.Add(new Separator());
                var updateItem = new MenuItem
                {
                    Header = CoreTools.Translate("Update to {0}", upgradable.NewVersionString)
                };
                updateItem.Click += async (_, _) =>
                {
                    ActionButtonControl.IsEnabled = false;
                    try
                    {
                        var options = await InstallOptionsFactory.LoadApplicableAsync(upgradable);
                        var op = new UpdatePackageOperation(upgradable, options);
                        AvaloniaOperationRegistry.Add(op);
                        _ = op.MainThread();
                        Close();
                    }
                    catch (Exception ex) { Logger.Error(ex); ActionButtonControl.IsEnabled = true; }
                };
                flyout.Items.Add(updateItem);
            }

            if (!_package.Source.IsVirtualManager)
            {
                if (flyout.Items.Count > 0 && upgradable is null) flyout.Items.Add(new Separator());
                var reinstallItem = new MenuItem { Header = CoreTools.Translate("Reinstall package") };
                reinstallItem.Click += async (_, _) =>
                {
                    ActionButtonControl.IsEnabled = false;
                    try
                    {
                        var options = await InstallOptionsFactory.LoadApplicableAsync(_package);
                        var op = new InstallPackageOperation(_package, options);
                        AvaloniaOperationRegistry.Add(op);
                        _ = op.MainThread();
                        Close();
                    }
                    catch (Exception ex) { Logger.Error(ex); ActionButtonControl.IsEnabled = true; }
                };
                flyout.Items.Add(reinstallItem);
            }
        }

        if (flyout.Items.Count > 0)
        {
            ActionDropdownButtonControl.Flyout = flyout;
            ActionDropdownButtonControl.IsVisible = true;
        }
    }

    private static void OpenUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch
        {
            // ignore — best effort
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private T GetControl<T>(string name)
        where T : Control
    {
        return this.FindControl<T>(name)
            ?? throw new InvalidOperationException($"Control '{name}' was not found.");
    }
}
