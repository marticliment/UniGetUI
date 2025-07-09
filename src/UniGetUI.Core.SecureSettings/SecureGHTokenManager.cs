using Windows.Security.Credentials;
using UniGetUI.Core.Logging;

namespace UniGetUI.Core.SecureSettings
{
    public static class SecureGHTokenManager
    {
        private const string GitHubResourceName = "UniGetUI/GitHubAccessToken";
        private static readonly string UserName = Environment.UserName;

        public static void StoreToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                Logger.Warn("Attempted to store a null or empty token. Operation cancelled.");
                return;
            }

            var vault = new PasswordVault();
            var newCredential = new PasswordCredential(GitHubResourceName, UserName, token);

            try
            {
                if (GetToken() is not null)
                {
                    DeleteToken();
                }
            }
            catch
            {
                // ignore
            }

            vault.Add(newCredential);
            Logger.Info("GitHub access token stored/updated securely.");
        }

        public static string? GetToken()
        {
            try
            {
                var vault = new PasswordVault();
                var credential = vault.Retrieve(GitHubResourceName, UserName);
                credential.RetrievePassword();
                Logger.Debug("GitHub access token retrieved.");
                return credential.Password;
            }
            catch (Exception ex)
            {
                Logger.Warn($"Could not retrieve token (it may not exist): {ex.Message}");
                return null;
            }
        }

        public static void DeleteToken()
        {
            var vault = new PasswordVault();
            var credentials = vault.FindAllByResource(GitHubResourceName);
            if (credentials.Count > 0)
            {
                foreach (var cred in credentials)
                {
                    vault.Remove(cred);
                }
                Logger.Info("GitHub access token deleted.");
            }
            else
            {
                Logger.Info("No GitHub access token found to delete.");
            }
        }
    }
}
