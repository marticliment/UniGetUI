using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Windows.Security.Credentials;
using UniGetUI.Core.Logging;

namespace UniGetUI.Core.SettingsEngine;

public partial class Settings
{
    /*
     *
     *
     */

    public static bool AreNotificationsDisabled()
    {
        return Get("DisableSystemTray") || Get("DisableNotifications");
    }

    public static bool AreUpdatesNotificationsDisabled()
    {
        return AreNotificationsDisabled() || Get("DisableUpdatesNotifications");
    }

    public static bool AreErrorNotificationsDisabled()
    {
        return AreNotificationsDisabled() || Get("DisableErrorNotifications");
    }

    public static bool AreSuccessNotificationsDisabled()
    {
        return AreNotificationsDisabled() || Get("DisableSuccessNotifications");
    }

    public static bool AreProgressNotificationsDisabled()
    {
        return AreNotificationsDisabled() || Get("DisableProgressNotifications");
    }

    public static Uri? GetProxyUrl()
    {
        if (!Settings.Get("EnableProxy")) return null;

        string plainUrl = Settings.GetValue("ProxyURL");
        Uri.TryCreate(plainUrl, UriKind.RelativeOrAbsolute, out Uri? var);
        if(Settings.Get("EnableProxy") && var is null) Logger.Warn($"Proxy setting {plainUrl} is not valid");
        return var;
    }

    private const string PROXY_RES_ID = "UniGetUI_proxy";

    public static NetworkCredential? GetProxyCredentials()
    {
        try
        {
            var vault = new PasswordVault();
            var credentials = vault.Retrieve(PROXY_RES_ID, Settings.GetValue("ProxyUsername"));

            return new NetworkCredential()
            {
                UserName = credentials.UserName,
                Password = credentials.Password,
            };
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
            var vault = new PasswordVault();
            Settings.SetValue("ProxyUsername", username);
            vault.Add(new PasswordCredential(PROXY_RES_ID, username, password));
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
