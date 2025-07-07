using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Data;
using System.Diagnostics;
using UniGetUI.Pages.DialogPages;
using UniGetUI.Interface.SoftwarePages;
using UniGetUI.Services; // Required for GitHubAuthService and GitHubBackupService
using UniGetUI.Core.Logging; // Required for Logger

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UniGetUI.Pages.SettingsPages.GeneralPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Backup : Page, ISettingsPage
    {
        private readonly GitHubAuthService _authService;
        private readonly GitHubBackupService _backupService;

        public Backup()
        {
            this.InitializeComponent();

            _authService = new GitHubAuthService();
            _backupService = new GitHubBackupService(_authService);

            EnablePackageBackupUI(Settings.Get(Settings.K.EnablePackageBackup));
            ResetBackupDirectory.Content = CoreTools.Translate("Reset");
            OpenBackupDirectory.Content = CoreTools.Translate("Open");
            UpdateBackupToGitHubButtonStatus();
        }

        private void UpdateBackupToGitHubButtonStatus()
        {
            // Check if user is logged in to GitHub
            // GitHubAuthService.IsAuthenticated() is synchronous for now, if it becomes async, this needs adjustment.
            if (_authService.IsAuthenticated())
            {
                BackupToGitHubButton.IsEnabled = true;
                BackupToGitHubButton.Description = CoreTools.Translate("Backup your settings to the linked GitHub Gist.");
                RestoreFromGitHubButton.IsEnabled = true;
                RestoreFromGitHubButton.Description = CoreTools.Translate("Restore your settings from the linked GitHub Gist. This will overwrite local settings.");
            }
            else
            {
                BackupToGitHubButton.IsEnabled = false;
                BackupToGitHubButton.Description = CoreTools.Translate("Login with GitHub (on the Internet settings page) to enable cloud backup.");
                RestoreFromGitHubButton.IsEnabled = false;
                RestoreFromGitHubButton.Description = CoreTools.Translate("Login with GitHub (on the Internet settings page) to enable cloud restore.");
            }
        }

        private async void RestoreFromGitHubButton_Click(object sender, RoutedEventArgs e)
        {
            RestoreFromGitHubButton.IsEnabled = false;
            var confirmDialog = DialogHelper.DialogFactory.Create();
            confirmDialog.Title = CoreTools.Translate("Confirm Restore");
            confirmDialog.Content = CoreTools.Translate("Restoring settings from GitHub Gist will overwrite your current local settings. Are you sure you want to continue?");
            confirmDialog.PrimaryButtonText = CoreTools.Translate("Yes, Restore");
            confirmDialog.CloseButtonText = CoreTools.Translate("Cancel");
            confirmDialog.DefaultButton = ContentDialogButton.Close;

            var result = await DialogHelper.Window.ShowDialogAsync(confirmDialog);
            if (result != ContentDialogResult.Primary)
            {
                UpdateBackupToGitHubButtonStatus(); // Re-enable button if still authenticated
                return;
            }

            DialogHelper.ShowLoadingDialog(CoreTools.Translate("Restoring settings from GitHub Gist..."));
            bool success = await _backupService.RestoreSettingsAsync();
            DialogHelper.HideLoadingDialog();

            if (success)
            {
                Logger.Info("Successfully restored settings from GitHub Gist.");
                DialogHelper.ShowDismissableBalloon(
                    CoreTools.Translate("Restore Successful"),
                    CoreTools.Translate("Your settings have been successfully restored from GitHub Gist. A restart is recommended to apply all changes."));
                // Optionally, prompt for restart here or let the user do it manually.
                // For now, just a message. The actual import logic (Step 4) will handle if restart is strictly needed.
            }
            else
            {
                Logger.Error("Failed to restore settings from GitHub Gist.");
                var errorDialog = DialogHelper.DialogFactory.Create();
                errorDialog.Title = CoreTools.Translate("Restore Failed");
                errorDialog.Content = CoreTools.Translate("Could not restore settings from GitHub Gist. Please check the logs for more details, or ensure a backup exists.");
                errorDialog.PrimaryButtonText = CoreTools.Translate("OK");
                errorDialog.DefaultButton = ContentDialogButton.Primary;
                _ = DialogHelper.Window.ShowDialogAsync(errorDialog);
            }
            UpdateBackupToGitHubButtonStatus(); // Re-enable button if still authenticated
        }

        private async void BackupToGitHubButton_Click(object sender, RoutedEventArgs e)
        {
            BackupToGitHubButton.IsEnabled = false;
            DialogHelper.ShowLoadingDialog(CoreTools.Translate("Backing up settings to GitHub Gist..."));

            bool success = await _backupService.BackupSettingsAsync();

            DialogHelper.HideLoadingDialog();

            if (success)
            {
                Logger.Info("Successfully backed up settings to GitHub Gist.");
                DialogHelper.ShowDismissableBalloon(
                    CoreTools.Translate("Backup Successful"),
                    CoreTools.Translate("Your settings have been successfully backed up to GitHub Gist."));
            }
            else
            {
                Logger.Error("Failed to backup settings to GitHub Gist.");
                var dialog = DialogHelper.DialogFactory.Create();
                dialog.Title = CoreTools.Translate("Backup Failed");
                dialog.Content = CoreTools.Translate("Could not back up settings to GitHub Gist. Please check the logs for more details.");
                dialog.PrimaryButtonText = CoreTools.Translate("OK");
                dialog.DefaultButton = ContentDialogButton.Primary;
                _ = DialogHelper.Window.ShowDialogAsync(dialog);
            }
            // Re-enable button only if still authenticated, as token issues might cause failure
            UpdateBackupToGitHubButtonStatus();
        }

        public bool CanGoBack => true;

        public string ShortTitle => CoreTools.Translate("Package backup");

        public event EventHandler? RestartRequired;
        public event EventHandler<Type>? NavigationRequested;

        public void ShowRestartBanner(object sender, EventArgs e)
            => RestartRequired?.Invoke(this, e);

        private void ChangeBackupDirectory_Click(object sender, EventArgs e)
        {
            ExternalLibraries.Pickers.FolderPicker openPicker = new(MainApp.Instance.MainWindow.GetWindowHandle());
            string folder = openPicker.Show();
            if (folder != string.Empty)
            {
                Settings.SetValue(Settings.K.ChangeBackupOutputDirectory, folder);
                BackupDirectoryLabel.Text = folder;
                ResetBackupDirectory.IsEnabled = true;
            }
        }

        public void EnablePackageBackupUI(bool enabled)
        {
            EnableBackupTimestampingCheckBox.IsEnabled = enabled;
            ChangeBackupFileNameTextBox.IsEnabled = enabled;
            ChangeBackupDirectory.IsEnabled = enabled;
            BackupNowButton.IsEnabled = enabled;

            if (enabled)
            {
                if (!Settings.Get(Settings.K.ChangeBackupOutputDirectory))
                {
                    BackupDirectoryLabel.Text = CoreData.UniGetUI_DefaultBackupDirectory;
                    ResetBackupDirectory.IsEnabled = false;
                }
                else
                {
                    BackupDirectoryLabel.Text = Settings.GetValue(Settings.K.ChangeBackupOutputDirectory);
                    ResetBackupDirectory.IsEnabled = true;
                }
            }
        }

        private void ResetBackupPath_Click(object sender, RoutedEventArgs e)
        {
            BackupDirectoryLabel.Text = CoreData.UniGetUI_DefaultBackupDirectory;
            Settings.Set(Settings.K.ChangeBackupOutputDirectory, false);
            ResetBackupDirectory.IsEnabled = false;
        }

        private void OpenBackupPath_Click(object sender, RoutedEventArgs e)
        {
            string directory = Settings.GetValue(Settings.K.ChangeBackupOutputDirectory);
            if (directory == "")
            {
                directory = CoreData.UniGetUI_DefaultBackupDirectory;
            }

            directory = directory.Replace("/", "\\");

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Process.Start("explorer.exe", directory);
        }

        private async void DoBackup_Click(object sender, EventArgs e)
        {
            DialogHelper.ShowLoadingDialog(CoreTools.Translate("Performing backup, please wait..."));
            await InstalledPackagesPage.BackupPackages();
            DialogHelper.HideLoadingDialog();
        }
    }
}
