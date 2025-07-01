using UniGetUI.Core.Data;
using UniGetUI.Core.Language;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.Interface.Telemetry;

public enum TEL_InstallReferral
{
    DIRECT_SEARCH,
    FROM_BUNDLE,
    FROM_WEB_SHARE,
    ALREADY_INSTALLED,
}

public enum TEL_OP_RESULT
{
    SUCCESS,
    FAILED,
    CANCELED
}

public static class TelemetryHandler
{
#if DEBUG
    private const string HOST = "http://localhost:3000";
#else
    private const string HOST = "https://marticliment.com/unigetui/statistics";
#endif

    private static readonly Settings.K[] SettingsToSend =
    [
        Settings.K.DisableAutoUpdateWingetUI,
        Settings.K.EnableUniGetUIBeta,
        Settings.K.DisableSystemTray,
        Settings.K.DisableNotifications,
        Settings.K.DisableAutoCheckforUpdates,
        Settings.K.AutomaticallyUpdatePackages,
        Settings.K.AskToDeleteNewDesktopShortcuts,
        Settings.K.EnablePackageBackup,
        Settings.K.DoCacheAdminRights,
        Settings.K.DoCacheAdminRightsForBatches,
        Settings.K.ForceLegacyBundledWinGet,
        Settings.K.UseSystemChocolatey
    ];
    // -------------------------------------------------------------------------

    public static async void Initialize()
    {
        try
        {
            if (Settings.Get(Settings.K.DisableTelemetry)) return;
            await CoreTools.WaitForInternetConnection();
            string ID = GetRandomizedId();

            int mask = 0x1;
            int ManagerMagicValue = 0;

            foreach (var manager in PEInterface.Managers)
            {
                if (manager.IsEnabled()) ManagerMagicValue |= mask;
                mask = mask << 1;
                if (manager.IsEnabled() && manager.Status.Found) ManagerMagicValue |= mask;
                mask = mask << 1;

                if (mask == 0x1)
                    throw new OverflowException();
            }

            int SettingsMagicValue = 0;
            mask = 0x1;
            foreach (var setting in SettingsToSend)
            {
                bool enabled = Settings.Get(
                    key: setting,
                    invert: Settings.ResolveKey(setting).StartsWith("Disable")
                );

                if (enabled) SettingsMagicValue |= mask;
                mask = mask << 1;

                if (mask == 0x1)
                    throw new OverflowException();
            }
            foreach (var setting in new []{"SP1", "SP2"})
            {
                bool enabled;
                if (setting == "SP1") enabled = File.Exists("ForceUniGetUIPortable");
                else if (setting == "SP2") enabled = CoreData.WasDaemon;
                else throw new NotImplementedException();

                if (enabled) SettingsMagicValue |= mask;
                mask = mask << 1;

                if (mask == 0x1)
                    throw new OverflowException();
            }

            var request = new HttpRequestMessage(HttpMethod.Post, $"{HOST}/activity");

            request.Headers.Add("clientId", ID);
            request.Headers.Add("clientVersion", CoreData.VersionName);
            request.Headers.Add("activeManagers", ManagerMagicValue.ToString());
            request.Headers.Add("activeSettings", SettingsMagicValue.ToString());
            request.Headers.Add("language", LanguageEngine.SelectedLocale);

            HttpClient _httpClient = new(CoreTools.GenericHttpClientParameters);
            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Logger.Debug("[Telemetry] Call to /activity succeeded");
            }
            else
            {
                Logger.Warn($"[Telemetry] Call to /activity failed with error code {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error("[Telemetry] Hard crash when calling /activity");
            Logger.Error(ex);
        }
    }

    // -------------------------------------------------------------------------

    public static void InstallPackage(IPackage package, TEL_OP_RESULT status, TEL_InstallReferral source)
        => PackageEndpoint(package, "install", status, source.ToString());

    public static void UpdatePackage(IPackage package, TEL_OP_RESULT status)
        => PackageEndpoint(package, "update", status);

    public static void DownloadPackage(IPackage package, TEL_OP_RESULT status, TEL_InstallReferral source)
        => PackageEndpoint(package, "download", status, source.ToString());

    public static void UninstallPackage(IPackage package, TEL_OP_RESULT status)
        => PackageEndpoint(package, "uninstall", status);

    public static void PackageDetails(IPackage package, string eventSource)
        => PackageEndpoint(package, "details", eventSource: eventSource);

    public static void SharedPackage(IPackage package, string eventSource)
        => PackageEndpoint(package, "share", eventSource: eventSource);

    private static async void PackageEndpoint(IPackage package, string endpoint, TEL_OP_RESULT? result = null, string? eventSource = null)
    {
        if (result is null && eventSource is null) throw new NullReferenceException("result and eventSource cannot be both null!");

        try
        {
            if (Settings.Get(Settings.K.DisableTelemetry)) return;
            await CoreTools.WaitForInternetConnection();
            string ID = GetRandomizedId();

            var request = new HttpRequestMessage(HttpMethod.Post, $"{HOST}/package/{endpoint}");

            request.Headers.Add("clientId", ID);
            request.Headers.Add("packageId", package.Id);
            request.Headers.Add("managerName", package.Manager.Name);
            request.Headers.Add("sourceName", package.Source.Name);
            if(result is not null) request.Headers.Add("operationResult", result.ToString());
            if(eventSource is not null) request.Headers.Add("eventSource", eventSource);

            HttpClient _httpClient = new(CoreTools.GenericHttpClientParameters);
            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Logger.Debug($"[Telemetry] Call to /package/{endpoint} succeeded");
            }
            else
            {
                Logger.Warn($"[Telemetry] Call to /package/{endpoint} failed with error code {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[Telemetry] Hard crash when calling /package/{endpoint}");
            Logger.Error(ex);
        }
    }


    // -------------------------------------------------------------------------

    public static void ImportBundle(BundleFormatType type)
        => BundlesEndpoint("import", type);

    public static void ExportBundle(BundleFormatType type)
        => BundlesEndpoint("export", type);

    private static async void BundlesEndpoint(string endpoint, BundleFormatType type)
    {
        try
        {
            if (Settings.Get(Settings.K.DisableTelemetry)) return;
            await CoreTools.WaitForInternetConnection();
            string ID = GetRandomizedId();

            var request = new HttpRequestMessage(HttpMethod.Post, $"{HOST}/bundles/{endpoint}");

            request.Headers.Add("clientId", ID);
            request.Headers.Add("bundleType", type.ToString());

            HttpClient _httpClient = new(CoreTools.GenericHttpClientParameters);
            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Logger.Debug($"[Telemetry] Call to /bundles/{endpoint} succeeded");
            }
            else
            {
                Logger.Warn($"[Telemetry] Call to /bundles/{endpoint} failed with error code {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"[Telemetry] Hard crash when calling /bundles/{endpoint}");
            Logger.Error(ex);
        }
    }

    private static string GetRandomizedId()
    {
        string ID = Settings.GetValue(Settings.K.TelemetryClientToken);
        if (ID.Length != 64)
        {
            ID = CoreTools.RandomString(64);
            Settings.SetValue(Settings.K.TelemetryClientToken, ID);
        }

        return ID;
    }
}
