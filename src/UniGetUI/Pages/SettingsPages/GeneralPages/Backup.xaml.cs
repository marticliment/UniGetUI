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
            RestoreSettingsFromGitHubButton.IsEnabled = isAuthenticated;
            RestorePackagesFromGitHubButton.IsEnabled = isAuthenticated;
        }

        private async void RestoreSettingsFromGitHubButton_Click(object sender, EventArgs e)
        {
            RestoreSettingsFromGitHubButton.IsEnabled = false;
            var confirmDialog = DialogHelper.DialogFactory.Create();
            confirmDialog.Title = CoreTools.Translate("Confirm Restore");
            confirmDialog.Content = CoreTools.Translate("Restoring settings from GitHub Gist will overwrite your current local settings. Are you sure you want to continue?");
            confirmDialog.PrimaryButtonText = CoreTools.Translate("Yes, Restore");
            confirmDialog.CloseButtonText = CoreTools.Translate("Cancel");
            confirmDialog.DefaultButton = ContentDialogButton.Close;

            var result = await DialogHelper.Window.ShowDialogAsync(confirmDialog);
            if (result != ContentDialogResult.Primary)
            {
                UpdateBackupToGitHubButtonStatus();
                return;
            }

            DialogHelper.ShowLoadingDialog(CoreTools.Translate("Restoring settings from GitHub Gist..."));
            var settingsContent = await _backupService.RestoreFileAsync("unigetui.settings.json");
            if (settingsContent != null)
            {
                await Task.Run(() => Settings.ImportFromString_JSON(settingsContent));
                DialogHelper.HideLoadingDialog();
                Logger.Info("Successfully restored settings from GitHub Gist.");
                DialogHelper.ShowDismissableBalloon(
                    CoreTools.Translate("Restore Successful"),
                    CoreTools.Translate("Your settings have been successfully restored from GitHub Gist. A restart is recommended to apply all changes."));
            }
            else
            {
                DialogHelper.HideLoadingDialog();
                Logger.Error("Failed to restore settings from GitHub Gist.");
                var errorDialog = DialogHelper.DialogFactory.Create();
                errorDialog.Title = CoreTools.Translate("Restore Failed");
                errorDialog.Content = CoreTools.Translate("Could not restore settings from GitHub Gist. Please check the logs for more details, or ensure a backup exists.");
                errorDialog.PrimaryButtonText = CoreTools.Translate("OK");
                errorDialog.DefaultButton = ContentDialogButton.Primary;
                _ = DialogHelper.Window.ShowDialogAsync(errorDialog);
            }
            UpdateBackupToGitHubButtonStatus();
        }

        private async void RestorePackagesFromGitHubButton_Click(object sender, EventArgs e)
        {
            RestorePackagesFromGitHubButton.IsEnabled = false;
            DialogHelper.ShowLoadingDialog(CoreTools.Translate("Loading package bundle from GitHub Gist..."));
            var packagesContent = await _backupService.RestoreFileAsync("unigetui.packages.ubundle");
            if (packagesContent != null)
            {
                var page = MainApp.Instance.MainWindow.NavigationPage.BundlesPage;
                if (page != null)
                {
                    if (await page.AskForNewBundle() == false)
                    {
                        DialogHelper.HideLoadingDialog();
                        UpdateBackupToGitHubButtonStatus();
                        return;
                    }
                    await page.AddFromBundle(packagesContent, UniGetUI.PackageEngine.Enums.BundleFormatType.UBUNDLE);
                    MainApp.Instance.MainWindow.NavigationPage.NavigateTo(PageType.Bundles);
                }

                DialogHelper.HideLoadingDialog();
                Logger.Info("Successfully loaded package bundle from GitHub Gist.");
                DialogHelper.ShowDismissableBalloon(
                    CoreTools.Translate("Bundle loaded"),
                    CoreTools.Translate("The package bundle has been loaded into the Package Bundles page."));
            }
            else
            {
                DialogHelper.HideLoadingDialog();
                Logger.Error("Failed to restore packages from GitHub Gist.");
                var errorDialog = DialogHelper.DialogFactory.Create();
                errorDialog.Title = CoreTools.Translate("Restore Failed");
                errorDialog.Content = CoreTools.Translate("Could not restore packages from GitHub Gist. Please check the logs for more details, or ensure a backup exists.");
                errorDialog.PrimaryButtonText = CoreTools.Translate("OK");
                errorDialog.DefaultButton = ContentDialogButton.Primary;
                _ = DialogHelper.Window.ShowDialogAsync(errorDialog);
            }
            UpdateBackupToGitHubButtonStatus();
        }

        private async void BackupToGitHubButton_Click(object sender, EventArgs e)
        {
            BackupToGitHubButton.IsEnabled = false;
            DialogHelper.ShowLoadingDialog(CoreTools.Translate("Backing up settings and packages to GitHub Gist..."));

            var settingsContent = await Task.Run(Settings.ExportToString_JSON);
            var packagesContent = await InstalledPackagesPage.GenerateBackupContents();

            var filesToBackup = new Dictionary<string, string>
            {
                { "unigetui.settings.json", settingsContent },
                { "unigetui.packages.ubundle", packagesContent }
            };

            bool success = await _backupService.BackupAsync(filesToBackup);

            DialogHelper.HideLoadingDialog();

            if (success)
            {
                Logger.Info("Successfully backed up settings and packages to GitHub Gist.");
                DialogHelper.ShowDismissableBalloon(
                    CoreTools.Translate("Backup Successful"),
                    CoreTools.Translate("Your settings and packages have been successfully backed up to GitHub Gist."));
            }
            else
            {
                Logger.Error("Failed to backup settings or packages to GitHub Gist.");
                var dialog = DialogHelper.DialogFactory.Create();
                dialog.Title = CoreTools.Translate("Backup Failed");
                dialog.Content = CoreTools.Translate("Could not back up settings and/or packages to GitHub Gist. Please check the logs for more details.");
                dialog.PrimaryButtonText = CoreTools.Translate("OK");
                dialog.DefaultButton = ContentDialogButton.Primary;
                _ = DialogHelper.Window.ShowDialogAsync(dialog);
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
