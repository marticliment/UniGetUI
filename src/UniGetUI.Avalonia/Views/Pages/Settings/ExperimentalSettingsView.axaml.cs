using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using UniGetUI.Avalonia;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.SettingsEngine.SecureSettings;
using UniGetUI.Core.Tools;

namespace UniGetUI.Avalonia.Views.Pages.SettingsPages;

public partial class ExperimentalSettingsView : UserControl, ISettingsSectionView
{
    private bool _isLoading;
    private readonly DispatcherTimer _iconDbSaveTimer;

    // Checkboxes
    private CheckBox EnableApiCheckBoxControl => GetControl<CheckBox>("EnableApiCheckBox");
    private CheckBox DownloadLangUpdatesCheckBoxControl => GetControl<CheckBox>("DownloadLangUpdatesCheckBox");
    private CheckBox DisableTimeoutCheckBoxControl => GetControl<CheckBox>("DisableTimeoutCheckBox");
    private CheckBox EnableDmwOptimizationsCheckBoxControl => GetControl<CheckBox>("EnableDmwOptimizationsCheckBox");
    private CheckBox PerformIntegrityChecksCheckBoxControl => GetControl<CheckBox>("PerformIntegrityChecksCheckBox");
    private CheckBox ForceUserGSudoCheckBoxControl => GetControl<CheckBox>("ForceUserGSudoCheckBox");
    private CheckBox InstallInstalledPackagesCheckBoxControl => GetControl<CheckBox>("InstallInstalledPackagesCheckBox");

    // Icon DB
    private TextBox IconDbUrlTextBoxControl => GetControl<TextBox>("IconDbUrlTextBox");

    // Restart notice
    private Border RestartNoticeCardControl => GetControl<Border>("RestartNoticeCard");
    private Button RestartAppBtnCtrl => GetControl<Button>("RestartAppButton");

    // GSudo UAC pending
    private Border GsudoPendingBadgeControl => GetControl<Border>("GsudoPendingBadge");
    private TextBlock GsudoPendingTextControl => GetControl<TextBlock>("GsudoPendingText");

    // Label TextBlocks
    private TextBlock LeadTitleText => GetControl<TextBlock>("LeadTitleBlock");
    private TextBlock LeadDescriptionText => GetControl<TextBlock>("LeadDescriptionBlock");
    private TextBlock RestartTitleText => GetControl<TextBlock>("RestartTitleBlock");
    private TextBlock RestartDescriptionText => GetControl<TextBlock>("RestartDescriptionBlock");
    private TextBlock ServicesTitleText => GetControl<TextBlock>("ServicesTitleBlock");
    private TextBlock ServicesDescriptionText => GetControl<TextBlock>("ServicesDescriptionBlock");
    private TextBlock ServicesHintText => GetControl<TextBlock>("ServicesHintBlock");
    private TextBlock LanguageTitleText => GetControl<TextBlock>("LanguageTitleBlock");
    private TextBlock LanguageDescriptionText => GetControl<TextBlock>("LanguageDescriptionBlock");
    private TextBlock IconDbTitleText => GetControl<TextBlock>("IconDbTitleBlock");
    private TextBlock IconDbHintText => GetControl<TextBlock>("IconDbHintBlock");
    private TextBlock PerfTitleText => GetControl<TextBlock>("PerfTitleBlock");
    private TextBlock PerfDescriptionText => GetControl<TextBlock>("PerfDescriptionBlock");
    private TextBlock DisableTimeoutHintText => GetControl<TextBlock>("DisableTimeoutHintBlock");
    private TextBlock DmwOptimizationsHintText => GetControl<TextBlock>("DmwOptimizationsHintBlock");
    private TextBlock IntegrityTitleText => GetControl<TextBlock>("IntegrityTitleBlock");
    private TextBlock IntegrityDescriptionText => GetControl<TextBlock>("IntegrityDescriptionBlock");
    private TextBlock IntegrityHintText => GetControl<TextBlock>("IntegrityHintBlock");
    private TextBlock ElevationTitleText => GetControl<TextBlock>("ElevationTitleBlock");
    private TextBlock ElevationDescriptionText => GetControl<TextBlock>("ElevationDescriptionBlock");
    private TextBlock ElevationHintText => GetControl<TextBlock>("ElevationHintBlock");
    private TextBlock BundleTitleText => GetControl<TextBlock>("BundleTitleBlock");
    private TextBlock BundleDescriptionText => GetControl<TextBlock>("BundleDescriptionBlock");
    private TextBlock BundleHintText => GetControl<TextBlock>("BundleHintBlock");

    public ExperimentalSettingsView()
    {
        _iconDbSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _iconDbSaveTimer.Tick += IconDbSaveTimer_OnTick;

        InitializeComponent();

        EnableApiCheckBoxControl.Click += EnableApiCheckBox_OnClick;
        DownloadLangUpdatesCheckBoxControl.Click += DownloadLangUpdatesCheckBox_OnClick;
        DisableTimeoutCheckBoxControl.Click += DisableTimeoutCheckBox_OnClick;
        EnableDmwOptimizationsCheckBoxControl.Click += EnableDmwOptimizationsCheckBox_OnClick;
        PerformIntegrityChecksCheckBoxControl.Click += PerformIntegrityChecksCheckBox_OnClick;
        ForceUserGSudoCheckBoxControl.Click += ForceUserGSudoCheckBox_OnClick;
        InstallInstalledPackagesCheckBoxControl.Click += InstallInstalledPackagesCheckBox_OnClick;

        SectionTitle = CoreTools.Translate("Experimental preferences");
        SectionSubtitle = CoreTools.Translate("Feature flags, developer options, and advanced tuning settings.");
        SectionStatus = CoreTools.Translate("Live settings");

        ApplyLocalizedText();
        LoadStoredValues();

        // Subscribe TextChanged after loading to avoid spurious timer starts at init
        IconDbUrlTextBoxControl.TextChanged += IconDbUrlTextBox_OnTextChanged;
    }

    public string SectionTitle { get; }
    public string SectionSubtitle { get; }
    public string SectionStatus { get; }

    private void ApplyLocalizedText()
    {
        LeadTitleText.Text = CoreTools.Translate("These settings are experimental or affect advanced behaviors.");
        LeadDescriptionText.Text = CoreTools.Translate("Most of the settings below require a restart to take effect. Some are Windows-specific and will have no effect on other platforms.");

        RestartTitleText.Text = CoreTools.Translate("Restart required");
        RestartDescriptionText.Text = CoreTools.Translate("Changes to one or more settings on this page will take effect after restarting UniGetUI.");
        RestartAppBtnCtrl.Content = CoreTools.Translate("Restart UniGetUI");

        ServicesTitleText.Text = CoreTools.Translate("Background services");
        ServicesDescriptionText.Text = CoreTools.Translate("Configure background API services hosted by UniGetUI.");
        EnableApiCheckBoxControl.Content = CoreTools.Translate("Enable the background package API (UniGetUI Widgets, port 7058)");
        ServicesHintText.Text = CoreTools.Translate("When enabled, UniGetUI exposes a local HTTP API used by Widgets and other integrations. Requires a restart to apply.");

        LanguageTitleText.Text = CoreTools.Translate("Language and resources");
        LanguageDescriptionText.Text = CoreTools.Translate("Control how UniGetUI fetches updated language files and icon data.");
        DownloadLangUpdatesCheckBoxControl.Content = CoreTools.Translate("Download updated language files from GitHub automatically");
        IconDbTitleText.Text = CoreTools.Translate("Custom icon and screenshot database URL");
        IconDbUrlTextBoxControl.Watermark = CoreTools.Translate("Leave empty for default");
        IconDbHintText.Text = CoreTools.Translate("Enter a custom URL for the icon and screenshot database. Leave empty to use the official source. Requires a restart.");

        PerfTitleText.Text = CoreTools.Translate("Performance");
        PerfDescriptionText.Text = CoreTools.Translate("Tuning options that affect operation timing and system thread scheduling.");
        DisableTimeoutCheckBoxControl.Content = CoreTools.Translate("Disable the 1-minute timeout for package-related operations");
        DisableTimeoutHintText.Text = CoreTools.Translate("When checked, package listing operations will not time out after 60 seconds. Useful for very large package sources on slow connections.");
        EnableDmwOptimizationsCheckBoxControl.Content = CoreTools.Translate("Enable background CPU usage optimizations");
        DmwOptimizationsHintText.Text = CoreTools.Translate("Windows only. Applies Desktop Window Manager thread priority settings to reduce background CPU usage. Requires a restart.");

        IntegrityTitleText.Text = CoreTools.Translate("Startup integrity");
        IntegrityDescriptionText.Text = CoreTools.Translate("Control the startup integrity check that verifies package engine consistency on each launch.");
        PerformIntegrityChecksCheckBoxControl.Content = CoreTools.Translate("Perform integrity checks at startup");
        IntegrityHintText.Text = CoreTools.Translate("Disable only if startup is very slow and you understand the implications.");

        ElevationTitleText.Text = CoreTools.Translate("Elevation backend");
        ElevationDescriptionText.Text = CoreTools.Translate("Choose which tool UniGetUI uses to elevate package manager operations.");
        ForceUserGSudoCheckBoxControl.Content = CoreTools.Translate("Use installed GSudo instead of UniGetUI Elevator");
        ElevationHintText.Text = CoreTools.Translate("Requires GSudo to be installed and available on PATH. Changes take effect after restart. Requires administrator confirmation.");

        BundleTitleText.Text = CoreTools.Translate("Package bundle behavior");
        BundleDescriptionText.Text = CoreTools.Translate("Control how UniGetUI handles packages that are already installed when importing a bundle.");
        InstallInstalledPackagesCheckBoxControl.Content = CoreTools.Translate("Also install already-installed packages when batch-installing from a bundle");
        BundleHintText.Text = CoreTools.Translate("When checked, packages that are already at the desired version will still be reinstalled during a bundle restore.");
    }

    private void LoadStoredValues()
    {
        _isLoading = true;

        // Inverted: EnableAPI = !DisableApi
        EnableApiCheckBoxControl.IsChecked = !Settings.Get(Settings.K.DisableApi);
        // Inverted: DownloadLangUpdates = !DisableLangAutoUpdater
        DownloadLangUpdatesCheckBoxControl.IsChecked = !Settings.Get(Settings.K.DisableLangAutoUpdater);
        // Icon DB URL — value setting
        IconDbUrlTextBoxControl.Text = Settings.GetValue(Settings.K.IconDataBaseURL);
        // Not inverted: timeout disabled = setting=true
        DisableTimeoutCheckBoxControl.IsChecked = Settings.Get(Settings.K.DisableTimeoutOnPackageListingTasks);
        // Inverted: OptimizationsEnabled = !DisableDMWThreadOptimizations
        EnableDmwOptimizationsCheckBoxControl.IsChecked = !Settings.Get(Settings.K.DisableDMWThreadOptimizations);
        // Inverted: PerformIntegrity = !DisableIntegrityChecks
        PerformIntegrityChecksCheckBoxControl.IsChecked = !Settings.Get(Settings.K.DisableIntegrityChecks);
        // SecureSetting
        ForceUserGSudoCheckBoxControl.IsChecked = SecureSettings.Get(SecureSettings.K.ForceUserGSudo);
        // Not inverted
        InstallInstalledPackagesCheckBoxControl.IsChecked = Settings.Get(Settings.K.InstallInstalledPackagesBundlesPage);

        RestartNoticeCardControl.IsVisible = false;
        _isLoading = false;
    }

    // ── Click handlers ────────────────────────────────────────────────────

    private void EnableApiCheckBox_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        // Inverted: checkbox checked = API enabled = DisableApi=false
        Settings.Set(Settings.K.DisableApi, EnableApiCheckBoxControl.IsChecked != true);
        ShowRestartNotice();
    }

    private void DownloadLangUpdatesCheckBox_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        Settings.Set(Settings.K.DisableLangAutoUpdater, DownloadLangUpdatesCheckBoxControl.IsChecked != true);
        ShowRestartNotice();
    }

    private void DisableTimeoutCheckBox_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        Settings.Set(Settings.K.DisableTimeoutOnPackageListingTasks, DisableTimeoutCheckBoxControl.IsChecked == true);
    }

    private void EnableDmwOptimizationsCheckBox_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        Settings.Set(Settings.K.DisableDMWThreadOptimizations, EnableDmwOptimizationsCheckBoxControl.IsChecked != true);
        ShowRestartNotice();
    }

    private void PerformIntegrityChecksCheckBox_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        Settings.Set(Settings.K.DisableIntegrityChecks, PerformIntegrityChecksCheckBoxControl.IsChecked != true);
    }

    private async void ForceUserGSudoCheckBox_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        bool desired = ForceUserGSudoCheckBoxControl.IsChecked == true;

        ForceUserGSudoCheckBoxControl.IsEnabled = false;
        GsudoPendingTextControl.Text = desired
            ? CoreTools.Translate("Waiting for administrator confirmation to enable this setting…")
            : CoreTools.Translate("Waiting for administrator confirmation to disable this setting…");
        GsudoPendingBadgeControl.IsVisible = true;

        bool success = await SecureSettings.TrySet(SecureSettings.K.ForceUserGSudo, desired);

        GsudoPendingBadgeControl.IsVisible = false;
        ForceUserGSudoCheckBoxControl.IsEnabled = true;

        if (!success)
        {
            _isLoading = true;
            ForceUserGSudoCheckBoxControl.IsChecked = SecureSettings.Get(SecureSettings.K.ForceUserGSudo);
            _isLoading = false;
        }
        else
        {
            ShowRestartNotice();
        }
    }

    private void InstallInstalledPackagesCheckBox_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        Settings.Set(Settings.K.InstallInstalledPackagesBundlesPage, InstallInstalledPackagesCheckBoxControl.IsChecked == true);
    }

    // ── Icon DB URL debounced save ────────────────────────────────────────

    private void IconDbUrlTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isLoading) return;
        _iconDbSaveTimer.Stop();
        _iconDbSaveTimer.Start();
    }

    private void IconDbSaveTimer_OnTick(object? sender, EventArgs e)
    {
        _iconDbSaveTimer.Stop();
        Settings.SetValue(Settings.K.IconDataBaseURL, IconDbUrlTextBoxControl.Text ?? string.Empty);
        ShowRestartNotice();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void ShowRestartNotice()
    {
        RestartNoticeCardControl.IsVisible = true;
    }

    private void RestartAppButton_OnClick(object? sender, RoutedEventArgs e)
        => MainWindow.KillAndRestart();

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
