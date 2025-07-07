using System;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using UniGetUI.Core.Logging;

namespace UniGetUI.Core.SecureSettings
{
    public static class SecureTokenManager
    {
        private const string GitHubResourceName = "UniGetUI/GitHubAccessToken";

        public static async Task StoreTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                Logger.Warn("Attempted to store a null or empty token. Operation cancelled.");
                return;
            }

            var vault = new PasswordVault();
            var newCredential = new PasswordCredential(GitHubResourceName, "default_user", token);

            try
            {
                // Attempt to remove existing credential first to ensure clean add/update
                try
                {
                    // Retrieve can throw if not found, so FindAllByResource is safer for removal logic
                    var existingCredentials = vault.FindAllByResource(GitHubResourceName);
                    foreach (var cred in existingCredentials)
                    {
                        if (cred.UserName == "default_user") // Ensure we only remove the one we manage
                        {
                            vault.Remove(cred);
                            Logger.Debug("Removed existing GitHub token before storing the new one.");
                            break; // Assuming only one credential per user for this resource
                        }
                    }
                }
                catch (Exception ex)
                {
                    // This might happen if no credential exists, or other PasswordVault issues.
                    // If it's just "not found", that's fine for an add operation.
                    Logger.Debug($"Info during pre-removal attempt (might be okay if adding new): {ex.Message}");
                }

                vault.Add(newCredential);
                Logger.Info("GitHub access token stored/updated securely.");
            }
            catch (Exception ex)
            {
                // This catch is for errors during vault.Add() mainly.
                Logger.Error($"Error storing/updating token in PasswordVault: {ex.Message}");
                Logger.Error(ex);
            }
        }

        public static async Task<string?> GetTokenAsync()
        {
            try
            {
                var vault = new PasswordVault();
                var credential = vault.Retrieve(GitHubResourceName, "default_user");
                credential.RetrievePassword();
                Logger.Debug("GitHub access token retrieved.");
                return credential.Password;
            }
            catch (Exception ex)
            {
                // Typically means the credential was not found
                Logger.Warn($"Could not retrieve token (it may not exist): {ex.Message}");
                return null;
            }
        }

        public static async Task DeleteTokenAsync()
        {
            try
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
            catch (Exception ex)
            {
                Logger.Error($"Error deleting token: {ex.Message}");
                Logger.Error(ex);
            }
        }
    }
}
