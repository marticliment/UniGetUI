using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;

namespace UniGetUI;

public class AutoUpdater
{
    public static Window Window = null!;
    public static InfoBar Banner = null!;
    //------------------------------------------------------------------------------------------------------------------
    private const string STABLE_ENDPOINT = "https://www.marticliment.com/versions/unigetui/stable.ver";
    private const string BETA_ENDPOINT = "https://www.marticliment.com/versions/unigetui/beta.ver";
    private const string STABLE_INSTALLER_URL = "https://github.com/marticliment/UniGetUI/releases/latest/download/UniGetUI.Installer.exe";
    private const string BETA_INSTALLER_URL = "https://github.com/marticliment/UniGetUI/releases/download/$TAG/UniGetUI.Installer.exe";
    //------------------------------------------------------------------------------------------------------------------
    public static bool ReleaseLockForAutoupdate_Notification;
    public static bool ReleaseLockForAutoupdate_Window;
    public static bool ReleaseLockForAutoupdate_UpdateBanner;
    public static bool UpdateReadyToBeInstalled { get; private set; }

    public static async Task UpdateCheckLoop(Window window, InfoBar banner)
    {
        if (Settings.Get(Settings.K.DisableAutoUpdateWingetUI))
        {
            Logger.Warn("User has disabled updates");
            return;
        }

        bool IsFirstLaunch = true;
        Window = window;
        Banner = banner;

        await CoreTools.WaitForInternetConnection();
        while (true)
        {
            // User could have disabled updates on runtime
            if (Settings.Get(Settings.K.DisableAutoUpdateWingetUI))
            {
                Logger.Warn("User has disabled updates");
                return;
            }
            bool updateSucceeded = await CheckAndInstallUpdates(window, banner, false, IsFirstLaunch);
            IsFirstLaunch = false;
            await Task.Delay(TimeSpan.FromMinutes(updateSucceeded ? 60 : 10));
        }
    }

    /// <summary>
    /// Performs the entire update process, and returns true/false whether the process finished successfully;
    /// </summary>
    public static async Task<bool> CheckAndInstallUpdates(Window window, InfoBar banner, bool Verbose, bool AutoLaunch = false, bool ManualCheck = false)
    {
        Window = window;
        Banner = banner;
        bool WasCheckingForUpdates = true;

        try
        {
            if (Verbose) ShowMessage_ThreadSafe(
                CoreTools.Translate("We are checking for updates."),
                CoreTools.Translate("Please wait"),
                InfoBarSeverity.Informational,
                false
            );

            // Check for updates
            string UpdatesEndpoint = Settings.Get(Settings.K.EnableUniGetUIBeta) ? BETA_ENDPOINT : STABLE_ENDPOINT;
            string InstallerDownloadUrl = Settings.Get(Settings.K.EnableUniGetUIBeta) ? BETA_INSTALLER_URL : STABLE_INSTALLER_URL;
            var (IsUpgradable, LatestVersion, InstallerHash) = await CheckForUpdates(UpdatesEndpoint);

            if (IsUpgradable)
            {
                WasCheckingForUpdates = false;
                InstallerDownloadUrl = InstallerDownloadUrl.Replace("$TAG", LatestVersion);

                Logger.Info($"An update to UniGetUI version {LatestVersion} is available");
                string InstallerPath = Path.Join(CoreData.UniGetUIDataDirectory, "UniGetUI Updater.exe");

                if (File.Exists(InstallerPath)
                    && await CheckInstallerHash(InstallerPath, InstallerHash))
                {
                    Logger.Info($"A cached valid installer was found, launching update process...");
                    return await PrepairToLaunchInstaller(InstallerPath, LatestVersion, AutoLaunch, ManualCheck);
                }

                File.Delete(InstallerPath);

                ShowMessage_ThreadSafe(
                    CoreTools.Translate("UniGetUI version {0} is being downloaded.", LatestVersion.ToString(CultureInfo.InvariantCulture)),
                    CoreTools.Translate("This may take a minute or two"),
                    InfoBarSeverity.Informational,
                    false);

                // Download the installer
                await DownloadInstaller(InstallerDownloadUrl, InstallerPath);

                if (await CheckInstallerHash(InstallerPath, InstallerHash))
                {
                    Logger.Info("The downloaded installer is valid, launching update process...");
                    return await PrepairToLaunchInstaller(InstallerPath, LatestVersion, AutoLaunch, ManualCheck);
                }

                ShowMessage_ThreadSafe(
                    CoreTools.Translate("The installer authenticity could not be verified."),
                    CoreTools.Translate("The update process has been aborted."),
                    InfoBarSeverity.Error,
                    true);
                return false;
            }

            if (Verbose) ShowMessage_ThreadSafe(
                CoreTools.Translate("Great! You are on the latest version."),
                CoreTools.Translate("There are no new UniGetUI versions to be installed"),
                InfoBarSeverity.Success,
                true
            );
            return true;

        }
        catch (Exception e)
        {
            Logger.Error("An error occurred while checking for updates: ");
            Logger.Error(e);
            // We don't want an error popping if updates can't
            if (Verbose || !WasCheckingForUpdates) ShowMessage_ThreadSafe(
                CoreTools.Translate("An error occurred when checking for updates: "),
                e.Message,
                InfoBarSeverity.Error,
                true
            );
            return false;
        }
    }

    /// <summary>
    /// Checks whether new updates are available, and returns a tuple containing:
    ///  - A boolean that is set to True if new updates are available
    ///  - The new version name
    ///  - The hash of the installer for the new version, as a string.
    /// </summary>
    private static async Task<(bool, string, string)> CheckForUpdates(string endpoint)
    {
        Logger.Debug($"Begin check for updates on endpoint {endpoint}");
        string[] UpdateResponse;
        using (HttpClient client = new(CoreTools.GenericHttpClientParameters))
        {
            client.Timeout = TimeSpan.FromSeconds(600);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(CoreData.UserAgentString);
            UpdateResponse = (await client.GetStringAsync(endpoint)).Split("////");
        }

        if (UpdateResponse.Length >= 3)
        {
            int LatestVersion = int.Parse(UpdateResponse[0].Replace("\n", "").Replace("\r", "").Trim());
            string InstallerHash = UpdateResponse[1].Replace("\n", "").Replace("\r", "").Trim();
            string VersionName = UpdateResponse[2].Replace("\n", "").Replace("\r", "").Trim();
            Logger.Debug($"Got response from endpoint: ({LatestVersion}, {VersionName}, {InstallerHash})");
            return (LatestVersion > CoreData.BuildNumber, VersionName, InstallerHash);
        }

        Logger.Warn($"Received update string is {UpdateResponse[0]}");
        throw new FormatException("The updates file does not follow the FloatVersion////Sha256Hash////VersionName format");
   }

    /// <summary>
    /// Checks whether the downloaded updater matches the hash.
    /// </summary>
    private static async Task<bool> CheckInstallerHash(string installerLocation, string expectedHash)
    {
        Logger.Debug($"Checking updater hash on location {installerLocation}");
        using (FileStream stream = File.OpenRead(installerLocation))
        {
            string hash = Convert.ToHexString(await SHA256.Create().ComputeHashAsync(stream)).ToLower();
            if (hash == expectedHash.ToLower())
            {
                Logger.Debug($"The hashes match ({hash})");
                return true;
            }
            Logger.Warn($"Hash mismatch.\nExpected: {expectedHash}\nGot:      {hash}");
            return false;
        }
    }

    /// <summary>
    /// Downloads the given installer to the given location
    /// </summary>
    private static async Task DownloadInstaller(string downloadUrl, string installerLocation)
    {
        Logger.Debug($"Downloading installer from {downloadUrl} to {installerLocation}");
        using (HttpClient client = new(CoreTools.GenericHttpClientParameters))
        {
            client.Timeout = TimeSpan.FromSeconds(600);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(CoreData.UserAgentString);
            HttpResponseMessage result = await client.GetAsync(downloadUrl);
            result.EnsureSuccessStatusCode();
            using FileStream fs = new(installerLocation, FileMode.OpenOrCreate);
            await result.Content.CopyToAsync(fs);
        }
        Logger.Debug("The download has finished successfully");
    }

    /// <summary>
    /// Waits for the window to be closed if it is open and launches the updater
    /// </summary>
    private static async Task<bool> PrepairToLaunchInstaller(string installerLocation, string NewVersion, bool AutoLaunch, bool ManualCheck)
    {
        Logger.Debug("Starting the process to launch the installer.");
        UpdateReadyToBeInstalled = true;
        ReleaseLockForAutoupdate_Window = false;
        ReleaseLockForAutoupdate_Notification = false;
        ReleaseLockForAutoupdate_UpdateBanner = false;

        // Check if the user has disabled updates
        if (!ManualCheck && Settings.Get(Settings.K.DisableAutoUpdateWingetUI))
        {
            Banner.IsOpen = false;
            Logger.Warn("User disabled updates!");
            return true;
        }

        Window.DispatcherQueue.TryEnqueue(() =>
        {
            // Set the banner to Restart UniGetUI to update
            var UpdateNowButton = new Button { Content = CoreTools.Translate("Update now") };
            UpdateNowButton.Click += (_, _) => ReleaseLockForAutoupdate_UpdateBanner = true;
            ShowMessage_ThreadSafe(
                CoreTools.Translate("UniGetUI {0} is ready to be installed.", NewVersion),
                CoreTools.Translate("The update process will start after closing UniGetUI"),
                InfoBarSeverity.Success,
                true,
                UpdateNowButton);

            // Show a toast notification
            AppNotificationBuilder builder = new AppNotificationBuilder()
                .SetScenario(AppNotificationScenario.Default)
                .SetTag(CoreData.UniGetUICanBeUpdated.ToString())
                .AddText(CoreTools.Translate("{0} can be updated to version {1}", "UniGetUI", NewVersion))
                .SetAttributionText(CoreTools.Translate("You have currently version {0} installed", CoreData.VersionName))
                .AddArgument("action", NotificationArguments.Show)
                .AddButton(new AppNotificationButton(CoreTools.Translate("Update now"))
                    .AddArgument("action", NotificationArguments.ReleaseSelfUpdateLock)
                );
            AppNotification notification = builder.BuildNotification();
            notification.ExpiresOnReboot = true;
            AppNotificationManager.Default.Show(notification);

        });

        if (AutoLaunch && !Window.Visible)
        {
            Logger.Debug("AutoLaunch is enabled and the Window is hidden, launching installer...");
        }
        else
        {
            Logger.Debug("Waiting for mainWindow to be closed or for user to trigger the update from the notification...");
            while (
                !(ReleaseLockForAutoupdate_Window && !ManualCheck) &&
                !ReleaseLockForAutoupdate_Notification &&
                !ReleaseLockForAutoupdate_UpdateBanner)
            {
                await Task.Delay(100);
            }
            Logger.Debug("Autoupdater lock released, launching installer...");
        }

        if (!ManualCheck && Settings.Get(Settings.K.DisableAutoUpdateWingetUI))
        {
            Logger.Warn("User has disabled updates");
            return true;
        }

        await LaunchInstallerAndQuit(installerLocation);
        return true;
    }

    /// <summary>
    /// Launches the installer located on the installerLocation argument and quits UniGetUI
    /// </summary>
    private static async Task LaunchInstallerAndQuit(string installerLocation)
    {
        Logger.Debug("Launching the updater...");
        using Process p = new()
        {
            StartInfo = new()
            {
                FileName = installerLocation,
                Arguments = "/SILENT /SUPPRESSMSGBOXES /NORESTART /SP- /NoVCRedist /NoEdgeWebView /NoWinGet /NoChocolatey",
                UseShellExecute = true,
                CreateNoWindow = true,
            }
        };
        p.Start();
        ShowMessage_ThreadSafe(
            CoreTools.Translate("UniGetUI is being updated..."),
            CoreTools.Translate("This may take a minute or two"),
            InfoBarSeverity.Informational,
            false
        );
        await p.WaitForExitAsync();
        ShowMessage_ThreadSafe(
            CoreTools.Translate("Something went wrong while launching the updater."),
            CoreTools.Translate("Please try again later"),
            InfoBarSeverity.Error,
            true
        );
    }

    private static void ShowMessage_ThreadSafe(string Title, string Message, InfoBarSeverity MessageSeverity, bool BannerClosable, Button? ActionButton = null)
    {
        try
        {
            if (Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() is null)
            {
                Window.DispatcherQueue.TryEnqueue(() =>
                    ShowMessage_ThreadSafe(Title, Message, MessageSeverity, BannerClosable, ActionButton));
                return;
            }

            Banner.Title = Title;
            Banner.Message = Message;
            Banner.Severity = MessageSeverity;
            Banner.IsClosable = BannerClosable;
            Banner.ActionButton = ActionButton;
            Banner.IsOpen = true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }

    }
}
