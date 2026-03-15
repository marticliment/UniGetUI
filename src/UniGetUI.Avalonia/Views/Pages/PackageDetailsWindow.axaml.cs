using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
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
