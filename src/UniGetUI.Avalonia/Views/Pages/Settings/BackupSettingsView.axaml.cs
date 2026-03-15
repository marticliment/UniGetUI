using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using UniGetUI.Avalonia.Views.Pages;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.PackageLoader;
using System.Diagnostics;

namespace UniGetUI.Avalonia.Views.Pages.SettingsPages;

public partial class BackupSettingsView : UserControl, ISettingsSectionView
{
    private bool _isLoading;
    private readonly DispatcherTimer _filenameSaveTimer;

    // Checkboxes
    private CheckBox EnableLocalBackupCheckBoxControl => GetControl<CheckBox>("EnableLocalBackupCheckBox");
    private CheckBox EnableCloudBackupCheckBoxControl => GetControl<CheckBox>("EnableCloudBackupCheckBox");
    private CheckBox BackupTimestampingCheckBoxControl => GetControl<CheckBox>("BackupTimestampingCheckBox");

    // Buttons
    private Button ChangeBackupDirButtonControl => GetControl<Button>("ChangeBackupDirButton");
    private Button ResetBackupDirButtonControl => GetControl<Button>("ResetBackupDirButton");
    private Button OpenBackupDirButtonControl => GetControl<Button>("OpenBackupDirButton");
    private Button BackupToCloudButtonControl => GetControl<Button>("BackupToCloudButton");
    private Button RestoreFromCloudButtonControl => GetControl<Button>("RestoreFromCloudButton");
    private Button BackupNowButtonControl => GetControl<Button>("BackupNowButton");

    // TextBox
    private TextBox BackupFilenameTextBoxControl => GetControl<TextBox>("BackupFilenameTextBox");

    // Panels / cards
    private Border RestartNoticeCardControl => GetControl<Border>("RestartNoticeCard");
    private Border DirectoryCardControl => GetControl<Border>("DirectoryCard");
    private Border AdvancedCardControl => GetControl<Border>("AdvancedCard");

    // Labels
    private TextBlock LeadTitleText => GetControl<TextBlock>("LeadTitleBlock");
    private TextBlock BulletPackageListText => GetControl<TextBlock>("BulletPackageListBlock");
    private TextBlock BulletNoBinaryText => GetControl<TextBlock>("BulletNoBinaryBlock");
    private TextBlock BulletSmallSizeText => GetControl<TextBlock>("BulletSmallSizeBlock");
    private TextBlock BulletAutoBackupText => GetControl<TextBlock>("BulletAutoBackupBlock");
    private TextBlock RestartTitleText => GetControl<TextBlock>("RestartTitleBlock");
    private TextBlock RestartDescriptionText => GetControl<TextBlock>("RestartDescriptionBlock");
    private TextBlock CloudSectionTitleText => GetControl<TextBlock>("CloudSectionTitleBlock");
    private TextBlock CloudNotAvailableDescriptionText => GetControl<TextBlock>("CloudNotAvailableDescriptionBlock");
    private TextBlock LocalSectionTitleText => GetControl<TextBlock>("LocalSectionTitleBlock");
    private TextBlock LocalSectionDescriptionText => GetControl<TextBlock>("LocalSectionDescriptionBlock");
    private TextBlock DirectorySectionTitleText => GetControl<TextBlock>("DirectorySectionTitleBlock");
    private TextBlock DirectoryDescriptionText => GetControl<TextBlock>("DirectoryDescriptionBlock");
    private TextBlock BackupDirCurrentLabelText => GetControl<TextBlock>("BackupDirCurrentLabel");
    private TextBlock AdvancedSectionTitleText => GetControl<TextBlock>("AdvancedSectionTitleBlock");
    private TextBlock FilenameDescriptionText => GetControl<TextBlock>("FilenameDescriptionBlock");
    private TextBlock FilenameHintText => GetControl<TextBlock>("FilenameHintBlock");

    public BackupSettingsView()
    {
        _filenameSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _filenameSaveTimer.Tick += FilenameSaveTimer_OnTick;

        InitializeComponent();

        EnableLocalBackupCheckBoxControl.Click += EnableLocalBackupCheckBox_OnClick;
        BackupTimestampingCheckBoxControl.Click += BackupTimestampingCheckBox_OnClick;
        ChangeBackupDirButtonControl.Click += ChangeBackupDir_OnClick;
        ResetBackupDirButtonControl.Click += ResetBackupDir_OnClick;
        OpenBackupDirButtonControl.Click += OpenBackupDir_OnClick;

        SectionTitle = CoreTools.Translate("Backup and Restore");
        SectionSubtitle = CoreTools.Translate("Configure automatic backups of your installed packages list.");
        SectionStatus = CoreTools.Translate("Live settings");

        ApplyLocalizedText();
        LoadStoredValues();

        // Subscribe after loading to avoid spurious saves at init
        BackupFilenameTextBoxControl.TextChanged += BackupFilenameTextBox_OnTextChanged;
    }

    public string SectionTitle { get; }
    public string SectionSubtitle { get; }
    public string SectionStatus { get; }

    private void ApplyLocalizedText()
    {
        LeadTitleText.Text = CoreTools.Translate("Package backup");
        BulletPackageListText.Text = " \u25cf " + CoreTools.Translate("The backup will include the complete list of the installed packages and their installation options. Ignored updates and skipped versions will also be saved.");
        BulletNoBinaryText.Text = " \u25cf " + CoreTools.Translate("The backup will NOT include any binary file nor any program's saved data.");
        BulletSmallSizeText.Text = " \u25cf " + CoreTools.Translate("The size of the backup is estimated to be less than 1MB.");
        BulletAutoBackupText.Text = " \u25cf " + CoreTools.Translate("The backup will be performed after login.");

        RestartTitleText.Text = CoreTools.Translate("Restart required");
        RestartDescriptionText.Text = CoreTools.Translate("Changes to one or more settings on this page will take effect after restarting UniGetUI.");

        CloudSectionTitleText.Text = CoreTools.Translate("Cloud package backup");
        CloudNotAvailableDescriptionText.Text = CoreTools.Translate("Cloud backup via GitHub Gist requires signing in with a GitHub account. This feature is not yet available.");
        EnableCloudBackupCheckBoxControl.Content = CoreTools.Translate("Periodically perform a cloud backup of the installed packages");
        BackupToCloudButtonControl.Content = CoreTools.Translate("Backup");
        RestoreFromCloudButtonControl.Content = CoreTools.Translate("Select backup");

        LocalSectionTitleText.Text = CoreTools.Translate("Local package backup");
        LocalSectionDescriptionText.Text = CoreTools.Translate("Automatically save a backup of your installed packages list to a local folder.");
        EnableLocalBackupCheckBoxControl.Content = CoreTools.Translate("Periodically perform a local backup of the installed packages");
        BackupNowButtonControl.Content = CoreTools.Translate("Backup now");

        DirectorySectionTitleText.Text = CoreTools.Translate("Backup output directory");
        DirectoryDescriptionText.Text = CoreTools.Translate("Choose where backup files will be saved. Click Reset to restore the default location.");
        ChangeBackupDirButtonControl.Content = CoreTools.Translate("Select");
        ResetBackupDirButtonControl.Content = CoreTools.Translate("Reset");
        OpenBackupDirButtonControl.Content = CoreTools.Translate("Open");

        AdvancedSectionTitleText.Text = CoreTools.Translate("Local backup advanced options");
        FilenameDescriptionText.Text = CoreTools.Translate("Set a custom backup file name");
        BackupFilenameTextBoxControl.Watermark = CoreTools.Translate("Leave empty for default");
        FilenameHintText.Text = CoreTools.Translate("Leave blank to use the default name based on your PC name.");
        BackupTimestampingCheckBoxControl.Content = CoreTools.Translate("Add a timestamp to the backup file names");
    }

    private void LoadStoredValues()
    {
        _isLoading = true;

        bool localEnabled = Settings.Get(Settings.K.EnablePackageBackup_LOCAL);
        EnableLocalBackupCheckBoxControl.IsChecked = localEnabled;
        BackupTimestampingCheckBoxControl.IsChecked = Settings.Get(Settings.K.EnableBackupTimestamping);
        BackupFilenameTextBoxControl.Text = Settings.GetValue(Settings.K.ChangeBackupFileName);

        UpdateDirectoryLabel();
        ApplyLocalBackupEnabledState(localEnabled);
        BackupNowButtonControl.IsEnabled = localEnabled;

        RestartNoticeCardControl.IsVisible = false;
        _isLoading = false;
    }

    private void UpdateDirectoryLabel()
    {
        bool hasCustomDir = Settings.Get(Settings.K.ChangeBackupOutputDirectory);
        if (hasCustomDir)
        {
            string customPath = Settings.GetValue(Settings.K.ChangeBackupOutputDirectory);
            BackupDirCurrentLabelText.Text = string.IsNullOrWhiteSpace(customPath)
                ? CoreData.UniGetUI_DefaultBackupDirectory
                : customPath;
            ResetBackupDirButtonControl.IsEnabled = true;
        }
        else
        {
            BackupDirCurrentLabelText.Text = CoreData.UniGetUI_DefaultBackupDirectory;
            ResetBackupDirButtonControl.IsEnabled = false;
        }
    }

    private void ApplyLocalBackupEnabledState(bool enabled)
    {
        DirectoryCardControl.IsEnabled = enabled;
        AdvancedCardControl.IsEnabled = enabled;
        BackupNowButtonControl.IsEnabled = enabled;
    }

    // ── Click handlers ────────────────────────────────────────────────────

    private void EnableLocalBackupCheckBox_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        bool enabled = EnableLocalBackupCheckBoxControl.IsChecked == true;
        Settings.Set(Settings.K.EnablePackageBackup_LOCAL, enabled);
        ApplyLocalBackupEnabledState(enabled);
        ShowRestartNotice();
    }

    private void BackupTimestampingCheckBox_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        Settings.Set(Settings.K.EnableBackupTimestamping, BackupTimestampingCheckBoxControl.IsChecked == true);
    }

    private async void ChangeBackupDir_OnClick(object? sender, RoutedEventArgs e)
    {
        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storageProvider is null) return;

        var result = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = CoreTools.Translate("Select backup output folder"),
            AllowMultiple = false,
        });

        if (result.Count > 0)
        {
            string? path = result[0].TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(path))
            {
                Settings.SetValue(Settings.K.ChangeBackupOutputDirectory, path);
                Settings.Set(Settings.K.ChangeBackupOutputDirectory, true);
                UpdateDirectoryLabel();
            }
        }
    }

    private void ResetBackupDir_OnClick(object? sender, RoutedEventArgs e)
    {
        Settings.Set(Settings.K.ChangeBackupOutputDirectory, false);
        UpdateDirectoryLabel();
    }

    private void OpenBackupDir_OnClick(object? sender, RoutedEventArgs e)
    {
        string directory = Settings.GetValue(Settings.K.ChangeBackupOutputDirectory);
        if (string.IsNullOrWhiteSpace(directory))
            directory = CoreData.UniGetUI_DefaultBackupDirectory;

        directory = directory.Replace("/", "\\");
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        Process.Start(new ProcessStartInfo
        {
            FileName = directory,
            UseShellExecute = true,
        });
    }

    // ── Filename debounced save ───────────────────────────────────────────

    private void BackupFilenameTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isLoading) return;
        _filenameSaveTimer.Stop();
        _filenameSaveTimer.Start();
    }

    private void FilenameSaveTimer_OnTick(object? sender, EventArgs e)
    {
        _filenameSaveTimer.Stop();
        Settings.SetValue(Settings.K.ChangeBackupFileName, BackupFilenameTextBoxControl.Text ?? string.Empty);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void ShowRestartNotice()
    {
        RestartNoticeCardControl.IsVisible = true;
    }

    private async void BackupNowButton_OnClick(object? sender, RoutedEventArgs e)
    {
        BackupNowButtonControl.IsEnabled = false;
        try
        {
            var packages = InstalledPackagesLoader.Instance.Packages;
            string contents = await BundlesPageView.CreateBundleStringAsync(packages);

            string dirName = Settings.GetValue(Settings.K.ChangeBackupOutputDirectory);
            if (string.IsNullOrWhiteSpace(dirName))
                dirName = CoreData.UniGetUI_DefaultBackupDirectory;

            if (!Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);

            string fileName = Settings.GetValue(Settings.K.ChangeBackupFileName);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = CoreTools.Translate("{pcName} installed packages",
                    new Dictionary<string, object?> { { "pcName", Environment.MachineName } });

            if (Settings.Get(Settings.K.EnableBackupTimestamping))
                fileName += " " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");

            fileName += ".ubundle";
            string filePath = Path.Combine(dirName, fileName);
            await File.WriteAllTextAsync(filePath, contents);
            Logger.ImportantInfo("Backup saved to " + filePath);
            CoreTools.Launch(dirName);
        }
        catch (Exception ex)
        {
            Logger.Error("An error occurred during manual backup");
            Logger.Error(ex);
        }
        finally
        {
            BackupNowButtonControl.IsEnabled = Settings.Get(Settings.K.EnablePackageBackup_LOCAL);
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
