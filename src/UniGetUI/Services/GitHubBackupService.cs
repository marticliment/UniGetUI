using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;

namespace UniGetUI.Services
{
    public class GitHubBackupService
    {
        private const string GistDescriptionEndingKey = "#[UNIGETUI_BUNDLE_BACKUP_V1]";
        private readonly GitHubAuthService _authService;
        private const string GistDescription = $"UniGetUI package backups - DO NOT RENAME OR MODIFY {GistDescriptionEndingKey}";

        private const string ReadMeContents = "" +
              "This special Gist is used by UniGetUI to store your package backups. \n" +
              "Please DO NOT EDIT the contents or the description of this gist, or unexpected behaviours may occur.\n" +
              "Learn more about UniGetUI at https://github.com/marticliment/UniGetUI\n";


        private readonly string DeviceUserUniqueIdentifier;
        private readonly string GistFileKey;

        public GitHubBackupService(GitHubAuthService authService)
        {
            _authService = authService;
            DeviceUserUniqueIdentifier = $"{Environment.MachineName}\\{Environment.UserName}";
            GistFileKey = $"{DeviceUserUniqueIdentifier}";
        }

        private async Task<GitHubClient?> CreateClientAsync()
        {
            var token = await _authService.GetAccessTokenAsync();

            if (string.IsNullOrEmpty(token))
            {
                Logger.Error("GitHub access token is not available. Cannot perform Gist operation.");
                return null;
            }

            return new GitHubClient(new ProductHeaderValue("UniGetUI", CoreData.VersionName))
            {
                Credentials = new Credentials(token)
            };
        }

        /// <summary>
        /// Assuming authentication is set up, upload the given bundleContents to GitHub
        /// </summary>
        /// <param name="bundleContents"></param>
        /// <returns>A boolean representing the success of the operation</returns>
        public async Task<bool> UploadPackageBundle(string bundleContents)
        {
            var GHClient = await CreateClientAsync();
            if (GHClient == null)
            {
                Logger.Error("Upload of backup has been aborted since the user is not authenticated");
                return false;
            }
            User user = await GHClient.User.Current();

            try
            {
                var candidates = await GHClient.Gist.GetAllForUser(user.Login);
                Gist? existingBackup = candidates.FirstOrDefault(g => g.Description.EndsWith(GistDescriptionEndingKey));

                if (existingBackup is null)
                {
                    Logger.Warn($"No matching gist was found as a valid backup, a new gist will be created...");
                    existingBackup = await _createBackupGistAsync(GHClient);
                }

                await _updateBackupGistAsync(GHClient, existingBackup, bundleContents);
                Logger.Info($"Cloud backup completed successfully to gist {user.Login}/{existingBackup.Id}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred while attempting to upload backup to GitHub:");
                Logger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Upload the given payload to the given gist.
        /// Updates the existing file if GistFileKey exists, creates a new one otherwhise
        /// </summary>
        /// <param name="client"></param>
        /// <param name="gist"></param>
        /// <param name="payload"></param>
        private async Task _updateBackupGistAsync(GitHubClient client, Gist gist, string payload)
        {
            var update = new GistUpdate { Description = GistDescription };
            if (update.Files.ContainsKey(GistFileKey))
            {
                update.Files[GistFileKey] = new GistFileUpdate { Content = payload };
            }
            else
            {
                update.Files.Add(GistFileKey, new GistFileUpdate { Content = payload });
            }
            await client.Gist.Edit(gist.Id, update);
            Logger.Info($"Successfully updated Gist ID: {gist.Id}");
        }

        /// <summary>
        /// Creates a new Gist, prepared to be detectable by UniGetUI
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private static Task<Gist> _createBackupGistAsync(GitHubClient client)
        {
            var newGist = new NewGist
            {
                Description = GistDescription,
                Public = false,
            };
            newGist.Files.Add("- UniGetUI Package Backups", ReadMeContents);
            return client.Gist.Create(newGist);
        }

        public async Task<string?> RetrieveFileAsync(string fileName)
        {
            var client = await CreateClientAsync();
            if (client == null) return null;

            string fileContent = null;
            string gistId = Settings.GetValue(Settings.K.GitHubGistId);

            try
            {
                if (!string.IsNullOrEmpty(gistId))
                {
                    try
                    {
                        var gist = await client.Gist.Get(gistId);
                        if (gist.Files.TryGetValue(fileName, out var file) && file != null)
                        {
                            fileContent = file.Content;
                            Logger.Info($"Successfully retrieved file '{fileName}' content from Gist ID: {gistId}");
                        }
                        else
                        {
                            Logger.Error($"Gist ID {gistId} does not contain the expected file: {fileName}");
                        }
                    }
                    catch (NotFoundException)
                    {
                        Logger.Warn($"Stored Gist ID {gistId} not found. Will try to find by description.");
                        Settings.SetValue(Settings.K.GitHubGistId, "");
                        gistId = null;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error fetching Gist ID {gistId}: {ex.Message}. Will try to find by description.");
                        Settings.SetValue(Settings.K.GitHubGistId, "");
                        gistId = null;
                    }
                }

                if (fileContent == null)
                {
                    Logger.Info("Attempting to find settings Gist by description...");
                    var gists = await client.Gist.GetAll();
                    var settingsGist = gists.FirstOrDefault(g => g.Description == GistDescription && g.Files.ContainsKey(fileName));

                    if (settingsGist != null)
                    {
                        var fullGist = await client.Gist.Get(settingsGist.Id);
                        if (fullGist.Files.TryGetValue(fileName, out var file) && file != null)
                        {
                            fileContent = file.Content;
                            Settings.SetValue(Settings.K.GitHubGistId, fullGist.Id);
                            Logger.Info($"Found settings Gist by description. ID: {fullGist.Id}");
                        }
                        else
                        {
                             Logger.Error($"Found Gist by description (ID: {settingsGist.Id}) but it's missing file {fileName}.");
                        }
                    }
                    else
                    {
                        Logger.Warn($"No UniGetUI settings Gist found for the user with file {fileName}.");
                        return null;
                    }
                }

                if (string.IsNullOrEmpty(fileContent))
                {
                    Logger.Error($"File content for '{fileName}' is empty or could not be retrieved from Gist.");
                    return null;
                }

                return fileContent;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to restore file '{fileName}' from GitHub Gist:");
                Logger.Error(ex);
                return null;
            }
        }
    }
}
