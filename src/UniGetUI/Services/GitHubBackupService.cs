using System;
using System.Linq;
using System.Threading.Tasks;
using Octokit;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine; // For Settings.K.GitHubGistId and potentially settings export
// It's likely SettingsEngine_ImportExport.cs will be needed, but its path is still uncertain.
// Assuming it's in UniGetUI.Core.Settings for now.

namespace UniGetUI.Services
{
    public class GitHubBackupService
    {
        private readonly GitHubAuthService _authService;
        private const string GistDescription = "UniGetUI Settings Backup";
        private const string GistFileName = "unigetui.settings.json"; // Or .yaml if YAML is preferred

        public GitHubBackupService(GitHubAuthService authService)
        {
            _authService = authService;
        }

        private async Task<GitHubClient?> GetAuthenticatedClientAsync()
        {
            var token = await _authService.GetAccessTokenAsync();

            if (string.IsNullOrEmpty(token))
            {
                Logger.Error("GitHub access token is not available. Cannot perform Gist operation.");
                return null;
            }
            return new GitHubClient(new ProductHeaderValue("UniGetUI"))
            {
                Credentials = new Credentials(token)
            };
        }

        public async Task<bool> BackupSettingsAsync()
        {
            var client = await GetAuthenticatedClientAsync();
            if (client == null) return false;

            string settingsContent;
            try
            {
                settingsContent = await Settings.ExportSettingsAsStringAsync();
                Logger.Info("Successfully exported settings for GitHub Gist backup.");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to export settings for backup:");
                Logger.Error(ex);
                return false;
            }

            try
            {
                var gistId = Settings.GetValue(Settings.K.GitHubGistId);
                Gist gistToUpdate = null;

                if (!string.IsNullOrEmpty(gistId))
                {
                    try
                    {
                        gistToUpdate = await client.Gist.Get(gistId);
                        Logger.Info($"Found existing Gist with ID: {gistId} for update.");
                    }
                    catch (NotFoundException)
                    {
                        Logger.Warn($"Previously stored Gist ID {gistId} not found. Will create a new Gist.");
                        Settings.SetValue(Settings.K.GitHubGistId, ""); // Clear invalid Gist ID
                        gistId = null;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error fetching Gist ID {gistId}: {ex.Message}");
                        // Decide if we should proceed to create a new one or fail.
                        // For now, let's try creating a new one if fetching fails for reasons other than NotFound.
                        Settings.SetValue(Settings.K.GitHubGistId, "");
                        gistId = null;
                    }
                }

                if (gistToUpdate != null)
                {
                    var update = new GistUpdate
                    {
                        Description = GistDescription
                    };
                    update.Files[GistFileName] = new GistFileUpdate { Content = settingsContent };
                    await client.Gist.Edit(gistId, update);
                    Logger.Info($"Successfully updated Gist ID: {gistId}");
                }
                else
                {
                    var newGist = new NewGist
                    {
                        Description = GistDescription,
                        Public = false // Private Gist
                    };
                    newGist.Files.Add(GistFileName, settingsContent);

                    var createdGist = await client.Gist.Create(newGist);
                    Settings.SetValue(Settings.K.GitHubGistId, createdGist.Id);
                    Logger.Info($"Successfully created new Gist ID: {createdGist.Id}");
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to backup settings to GitHub Gist:");
                Logger.Error(ex);
                return false;
            }
        }

        public async Task<bool> RestoreSettingsAsync()
        {
            var client = await GetAuthenticatedClientAsync();
            if (client == null) return false;

            string settingsContent = null;
            string gistId = Settings.GetValue(Settings.K.GitHubGistId);

            try
            {
                if (!string.IsNullOrEmpty(gistId))
                {
                    try
                    {
                        var gist = await client.Gist.Get(gistId);
                        if (gist.Files.TryGetValue(GistFileName, out var file) && file != null)
                        {
                            settingsContent = file.Content;
                            Logger.Info($"Successfully retrieved settings content from Gist ID: {gistId}");
                        }
                        else
                        {
                            Logger.Error($"Gist ID {gistId} does not contain the expected file: {GistFileName}");
                            // Attempt to find by description as a fallback
                        }
                    }
                    catch (NotFoundException)
                    {
                        Logger.Warn($"Stored Gist ID {gistId} not found. Will try to find by description.");
                        Settings.SetValue(Settings.K.GitHubGistId, ""); // Clear invalid Gist ID
                        gistId = null; // Force search by description
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error fetching Gist ID {gistId}: {ex.Message}. Will try to find by description.");
                        Settings.SetValue(Settings.K.GitHubGistId, "");
                        gistId = null; // Force search by description
                    }
                }

                if (settingsContent == null) // If not found by ID or ID was invalid/missing
                {
                    Logger.Info("Attempting to find settings Gist by description...");
                    var gists = await client.Gist.GetAll(); // Gets gists for the authenticated user
                    var settingsGist = gists.FirstOrDefault(g => g.Description == GistDescription && g.Files.ContainsKey(GistFileName));

                    if (settingsGist != null)
                    {
                        // Fetch the full Gist to get content, as GetAllForUser might not include it
                        var fullGist = await client.Gist.Get(settingsGist.Id);
                        if (fullGist.Files.TryGetValue(GistFileName, out var file) && file != null)
                        {
                            settingsContent = file.Content;
                            Settings.SetValue(Settings.K.GitHubGistId, fullGist.Id); // Store the found Gist ID
                            Logger.Info($"Found settings Gist by description. ID: {fullGist.Id}");
                        }
                        else
                        {
                             Logger.Error($"Found Gist by description (ID: {settingsGist.Id}) but it's missing file {GistFileName}.");
                        }
                    }
                    else
                    {
                        Logger.Warn("No UniGetUI settings Gist found for the user by description.");
                        return false; // No Gist found to restore from
                    }
                }

                if (string.IsNullOrEmpty(settingsContent))
                {
                    Logger.Error("Settings content is empty or could not be retrieved from Gist.");
                    return false;
                }

                // TODO: Replace with actual settings import call
                // Example: await Core.Settings.SettingsEngine_ImportExport.ImportSettingsFromStringAsync(settingsContent);
                // This will be a key part of step 4 (Refine Settings Engine).
                await Settings.ImportSettingsFromStringAsync(settingsContent);
                Logger.Info("Successfully imported settings from Gist content.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to restore settings from GitHub Gist:");
                Logger.Error(ex);
                return false;
            }
        }
    }
}
