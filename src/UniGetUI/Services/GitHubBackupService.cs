using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;

namespace UniGetUI.Services
{
    public class GitHubBackupService
    {
        private readonly GitHubAuthService _authService;
        private const string GistDescription = "UniGetUI Backup";

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

        public async Task<bool> BackupAsync(Dictionary<string, string> filesToBackup)
        {
            var client = await GetAuthenticatedClientAsync();
            if (client == null) return false;

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
                        Settings.SetValue(Settings.K.GitHubGistId, ""); 
                        gistId = null;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error fetching Gist ID {gistId}: {ex.Message}");
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
                    foreach(var file in filesToBackup)
                    {
                        update.Files[file.Key] = new GistFileUpdate { Content = file.Value };
                    }
                    await client.Gist.Edit(gistId, update);
                    Logger.Info($"Successfully updated Gist ID: {gistId}");
                }
                else
                {
                    var newGist = new NewGist
                    {
                        Description = GistDescription,
                        Public = false
                    };
                    foreach (var file in filesToBackup)
                    {
                        newGist.Files.Add(file.Key, file.Value);
                    }

                    var createdGist = await client.Gist.Create(newGist);
                    Settings.SetValue(Settings.K.GitHubGistId, createdGist.Id);
                    Logger.Info($"Successfully created new Gist ID: {createdGist.Id}");
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to backup to GitHub Gist:");
                Logger.Error(ex);
                return false;
            }
        }

        public async Task<string?> RetrieveFileAsync(string fileName)
        {
            var client = await GetAuthenticatedClientAsync();
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