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
        => Get(K.DisableSystemTray) || Get(K.DisableNotifications);

    public static bool AreUpdatesNotificationsDisabled()
        => AreNotificationsDisabled() || Get(K.DisableUpdatesNotifications);

    public static bool AreErrorNotificationsDisabled()
        => AreNotificationsDisabled() || Get(K.DisableErrorNotifications);

    public static bool AreSuccessNotificationsDisabled()
        => AreNotificationsDisabled() || Get(K.DisableSuccessNotifications);

    public static bool AreProgressNotificationsDisabled()
        => AreNotificationsDisabled() || Get(K.DisableProgressNotifications);

    public static Uri? GetProxyUrl()
    {
        if (!Get(K.EnableProxy)) return null;

        string plainUrl = GetValue(K.ProxyURL);
        Uri.TryCreate(plainUrl, UriKind.RelativeOrAbsolute, out Uri? var);
        if(Get(K.EnableProxy) && var is null) Logger.Warn($"Proxy setting {plainUrl} is not valid");
        return var;
    }

    private const string PROXY_RES_ID = "UniGetUI_proxy";

    public static NetworkCredential? GetProxyCredentials()
    {
        try
        {
            var vault = new PasswordVault();
            var credentials = vault.Retrieve(PROXY_RES_ID, GetValue(K.ProxyUsername));

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
            SetValue(K.ProxyUsername, username);
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
