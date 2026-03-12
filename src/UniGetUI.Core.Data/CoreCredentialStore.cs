using System.Net;
using System.Security.Cryptography;
using System.Text;
using UniGetUI.Core.Logging;
#if WINDOWS
using Windows.Security.Credentials;
#endif

namespace UniGetUI.Core.Data;

public static class CoreCredentialStore
{
    public static NetworkCredential? GetCredential(string resourceName, string userName)
    {
        string? secret = GetSecret(resourceName, userName);
        return secret is null
            ? null
            : new NetworkCredential { UserName = userName, Password = secret };
    }

    public static void SetCredential(string resourceName, string userName, string password) =>
        SetSecret(resourceName, userName, password);

    public static string? GetSecret(string resourceName, string userName)
    {
        try
        {
#if WINDOWS
            var vault = new PasswordVault();
            var credential = vault.Retrieve(resourceName, userName);
            credential.RetrievePassword();
            return credential.Password;
#else
            string secretPath = GetSecretPath(resourceName, userName);
            return File.Exists(secretPath) ? File.ReadAllText(secretPath) : null;
#endif
        }
        catch (Exception ex)
        {
            Logger.Warn($"Unable to retrieve secret for resource '{resourceName}': {ex.Message}");
            return null;
        }
    }

    public static void SetSecret(string resourceName, string userName, string secret)
    {
        try
        {
#if WINDOWS
            DeleteSecret(resourceName, userName);
            var vault = new PasswordVault();
            vault.Add(new PasswordCredential(resourceName, userName, secret));
#else
            string storageDirectory = GetStorageDirectory();
            Directory.CreateDirectory(storageDirectory);
            File.WriteAllText(GetSecretPath(resourceName, userName), secret);
#endif
        }
        catch (Exception ex)
        {
            Logger.Error($"Unable to persist secret for resource '{resourceName}'");
            Logger.Error(ex);
        }
    }

    public static void DeleteSecret(string resourceName, string userName)
    {
        try
        {
#if WINDOWS
            var vault = new PasswordVault();
            IReadOnlyList<PasswordCredential> credentials =
                vault.FindAllByResource(resourceName) ?? [];
            foreach (
                PasswordCredential credential in credentials.Where(credential =>
                    credential.UserName == userName
                )
            )
            {
                vault.Remove(credential);
            }
#else
            string secretPath = GetSecretPath(resourceName, userName);
            if (File.Exists(secretPath))
            {
                File.Delete(secretPath);
            }
#endif
        }
        catch (Exception ex)
        {
            Logger.Error($"Unable to delete secret for resource '{resourceName}'");
            Logger.Error(ex);
        }
    }

#if !WINDOWS
    private static string GetStorageDirectory() =>
        Path.Join(CoreData.UniGetUIDataDirectory, "SecureStorage");

    private static string GetSecretPath(string resourceName, string userName) =>
        Path.Join(GetStorageDirectory(), GetStableFileName(resourceName, userName));

    private static string GetStableFileName(string resourceName, string userName)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{resourceName}\n{userName}"));
        return Convert.ToHexString(hash) + ".secret";
    }
#endif
}
