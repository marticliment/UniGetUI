using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;

namespace UniGetUI.Core.SettingsEngine;

public partial class Settings
{
    private static readonly string[] SupportedProxySchemes =
    [
        Uri.UriSchemeHttp,
        Uri.UriSchemeHttps,
    ];

    /*
     *
     *
     */

    public static bool AreNotificationsDisabled() =>
        Get(K.DisableSystemTray) || Get(K.DisableNotifications);

    public static bool AreUpdatesNotificationsDisabled() =>
        AreNotificationsDisabled() || Get(K.DisableUpdatesNotifications);

    public static bool AreErrorNotificationsDisabled() =>
        AreNotificationsDisabled() || Get(K.DisableErrorNotifications);

    public static bool AreSuccessNotificationsDisabled() =>
        AreNotificationsDisabled() || Get(K.DisableSuccessNotifications);

    public static bool AreProgressNotificationsDisabled() =>
        AreNotificationsDisabled() || Get(K.DisableProgressNotifications);

    public static Uri? GetProxyUrl()
    {
        if (!Get(K.EnableProxy))
            return null;

        string plainUrl = GetValue(K.ProxyURL).Trim();
        if (plainUrl.Length is 0)
        {
            Logger.Warn("Proxy is enabled, but no proxy URL has been configured");
            return null;
        }

        Uri? proxyUri = TryCreateAbsoluteProxyUri(plainUrl);
        if (proxyUri is null)
            Logger.Warn($"Proxy setting {plainUrl} is not valid");
        return proxyUri;
    }

    private static Uri? TryCreateAbsoluteProxyUri(string plainUrl)
    {
        if (Uri.TryCreate(plainUrl, UriKind.Absolute, out Uri? proxyUri) && IsSupportedProxyUri(proxyUri))
        {
            return proxyUri;
        }

        if (plainUrl.Contains("://", StringComparison.Ordinal))
        {
            return null;
        }

        string normalizedUrl = $"http://{plainUrl}";
        return Uri.TryCreate(normalizedUrl, UriKind.Absolute, out proxyUri)
                && IsSupportedProxyUri(proxyUri)
            ? proxyUri
            : null;
    }

    private static bool IsSupportedProxyUri(Uri proxyUri) =>
        proxyUri.IsAbsoluteUri
        && !string.IsNullOrWhiteSpace(proxyUri.Host)
        && SupportedProxySchemes.Contains(proxyUri.Scheme, StringComparer.OrdinalIgnoreCase);

    private const string PROXY_RES_ID = "UniGetUI_proxy";

    public static NetworkCredential? GetProxyCredentials()
    {
        try
        {
            string username = GetValue(K.ProxyUsername);
            return username.Length is 0
                ? null
                : CoreCredentialStore.GetCredential(PROXY_RES_ID, username);
        }
        catch (Exception ex)
        {
            Logger.Error("Could not retrieve Proxy credentials");
            Logger.Error(ex);
            return null;
        }
    }

    public static void SetProxyCredentials(string username, string password)
    {
        try
        {
            SetValue(K.ProxyUsername, username);
            CoreCredentialStore.SetCredential(PROXY_RES_ID, username, password);
        }
        catch (Exception ex)
        {
            Logger.Error("Cannot save Proxy credentials");
            Logger.Error(ex);
        }
    }

    public static JsonSerializerOptions SerializationOptions = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        AllowTrailingCommas = true,
        WriteIndented = true,
    };
}
