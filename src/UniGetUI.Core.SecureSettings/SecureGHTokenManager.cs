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

            try
            {
                var vault = new PasswordVault();
                var newCredential = new PasswordCredential(GitHubResourceName, UserName, token);
                if (GetToken() is not null)
                    DeleteToken(); // Delete any old token(s)

                vault.Add(newCredential);
                Logger.Info("GitHub access token stored/updated securely.");
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred while attempting to delete the currently stored GitHub Token");
                Logger.Error(ex);
            }
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
            try
            {
                var vault = new PasswordVault();
                var credentials = vault.FindAllByResource(GitHubResourceName) ?? [];
                foreach (var cred in credentials)
                {
                    vault.Remove(cred);
                    Logger.Info("GitHub access token deleted.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred while attempting to delete the currently stored GitHub Token");
                Logger.Error(ex);
            }
        }
    }
}
