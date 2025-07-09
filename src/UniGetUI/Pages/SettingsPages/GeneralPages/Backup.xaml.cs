using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UniGetUI.Core.Tools;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Data;
using System.Diagnostics;
using UniGetUI.Pages.DialogPages;
using UniGetUI.Interface.SoftwarePages;
using UniGetUI.Services;
using UniGetUI.Core.Logging;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.Interface;
using UniGetUI.PackageEngine.Enums;

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
            UpdateGitHubLoginStatus();
            UpdateBackupToGitHubButtonStatus();
        }

        private void UpdateGitHubLoginStatus()
        {
            var userName = Settings.GetValue(Settings.K.GitHubUserLogin);
            if (!string.IsNullOrEmpty(userName))
            {
                GitHubUserText.Text = $"Logged in as: {userName}";
                LoginWithGitHubButton.Visibility = Visibility.Collapsed;
                LogoutGitHubButton.Visibility = Visibility.Visible;
            }
            else
            {
                GitHubUserText.Text = "Not logged in";
                LoginWithGitHubButton.Visibility = Visibility.Visible;
                LogoutGitHubButton.Visibility = Visibility.Collapsed;
            }
        }

        private async void LoginWithGitHubButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWithGitHubButton.IsEnabled = false;
            bool success = await _authService.SignInAsync();
            if (success)
            {
                Logger.Info("Successfully logged in with GitHub.");
            }
            else
            {
                Logger.Error("Failed to log in with GitHub.");
                GitHubUserText.Text = "Login failed. See logs for details.";
            }
            UpdateBackupToGitHubButtonStatus();
            LoginWithGitHubButton.IsEnabled = true;
        }

        private async void LogoutGitHubButton_Click(object sender, RoutedEventArgs e)
        {
            LogoutGitHubButton.IsEnabled = false;
            await _authService.SignOutAsync();
            UpdateBackupToGitHubButtonStatus();
            Logger.Info("Successfully logged out from GitHub.");
            LogoutGitHubButton.IsEnabled = true;
        }

        private async void UpdateBackupToGitHubButtonStatus()
        {
            UpdateGitHubLoginStatus();
            bool isAuthenticated = await _authService.IsAuthenticatedAsync();
            BackupToGitHubButton.IsEnabled = isAuthenticated;
            RestorePackagesFromGitHubButton.IsEnabled = isAuthenticated;
        }

        private async void RestorePackagesFromGitHubButton_Click(object sender, EventArgs e)
        {
            RestorePackagesFromGitHubButton.IsEnabled = false;
            try
            {
                DialogHelper.ShowLoadingDialog(CoreTools.Translate("Fetching available backups..."));
                var availableBackups = await _backupService.GetAvailableBackups();
                DialogHelper.HideLoadingDialog();

                var selectedBackup = await DialogHelper.AskForBackupSelection(availableBackups);
                if (selectedBackup is null)
                {
                    RestorePackagesFromGitHubButton.IsEnabled = true;
                    return;
                }
                selectedBackup = selectedBackup.Split(' ')[0];

                DialogHelper.ShowLoadingDialog(CoreTools.Translate("Downloading backup..."));
                var backupContents = await _backupService.GetBackupContents(selectedBackup);
                DialogHelper.HideLoadingDialog();
                await Task.Delay(500); // Prevent race conditions with dialogs

                if (backupContents is null)
                    throw new Exception($"The backupContents for backup {selectedBackup} returned null");

                Logger.Info("Successfully loaded package bundle from GitHub Gist.");
                DialogHelper.ShowDismissableBalloon(
                    CoreTools.Translate("Done!"),
                    CoreTools.Translate("The cloud backup has been loaded successfully."));

                MainApp.Instance.MainWindow.NavigationPage.LoadBundleFromString(
                    backupContents, BundleFormatType.UBUNDLE, $"GitHub Gist {selectedBackup}");
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred while loading a backup:");
                Logger.Error(ex);

                DialogHelper.HideLoadingDialog();
                var errorDialog = DialogHelper.DialogFactory.Create();
                errorDialog.Title = CoreTools.Translate("An error occurred");
                errorDialog.Content = CoreTools.Translate("An error occurred while loading a backup: ") + ex.Message;
                errorDialog.PrimaryButtonText = CoreTools.Translate("OK");
                errorDialog.DefaultButton = ContentDialogButton.Primary;
                await DialogHelper.Window.ShowDialogAsync(errorDialog);
            }
            UpdateBackupToGitHubButtonStatus();
        }

        private async void BackupToGitHubButton_Click(object sender, EventArgs e)
        {
            BackupToGitHubButton.IsEnabled = false;
            DialogHelper.ShowLoadingDialog(CoreTools.Translate("Backing up packages to GitHub Gist..."));

            var packagesContent = await InstalledPackagesPage.GenerateBackupContents();

            try
            {
                await _backupService.UploadPackageBundle(packagesContent);
                DialogHelper.HideLoadingDialog();
                Logger.Info("Successfully backed up settings and packages to GitHub Gist.");
                DialogHelper.ShowDismissableBalloon(
                    CoreTools.Translate("Backup Successful"),
                    CoreTools.Translate("Your settings and packages have been successfully backed up to GitHub Gist."));
            }
            catch (Exception ex)
            {
                DialogHelper.HideLoadingDialog();

                Logger.Error("An error occurred while uploading the backup:");
                Logger.Error(ex);

                var dialog = DialogHelper.DialogFactory.Create();
                dialog.Title = CoreTools.Translate("Backup Failed");
                dialog.Content = CoreTools.Translate("Could not back up packages to GitHub Gist: ") + ex.Message;
                dialog.PrimaryButtonText = CoreTools.Translate("OK");
                dialog.DefaultButton = ContentDialogButton.Primary;
                await DialogHelper.Window.ShowDialogAsync(dialog);
            }
            UpdateBackupToGitHubButtonStatus();
        }

        public bool CanGoBack => true;

        public string ShortTitle => CoreTools.Translate("Backup and Restore");

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
