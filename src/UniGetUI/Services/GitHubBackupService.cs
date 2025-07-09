using Octokit;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;

namespace UniGetUI.Services
{
    public class GitHubBackupService
    {
        private const string GistDescription_EndingKey = "@[UNIGETUI_BACKUP_V1]";
        private const string PackageBackup_StartingKey = "@[PACKAGES]";

        private const string GistDescription = $"UniGetUI package backups - DO NOT RENAME OR MODIFY {GistDescription_EndingKey}";
        private const string ReadMeContents = "" +
              "This special Gist is used by UniGetUI to store your package backups. \n" +
              "Please DO NOT EDIT the contents or the description of this gist, or unexpected behaviours may occur.\n" +
              "Learn more about UniGetUI at https://github.com/marticliment/UniGetUI\n";

        private readonly GitHubAuthService _authService;

        private readonly string GistFileKey;

        public GitHubBackupService(GitHubAuthService authService)
        {
            _authService = authService;
            string deviceUserUniqueIdentifier = $"{Environment.MachineName}\\{Environment.UserName}".Replace(" ", "");
            GistFileKey = $"{PackageBackup_StartingKey} {deviceUserUniqueIdentifier}";
        }

        /// <summary>
        /// Assuming authentication is set up, upload the given bundleContents to GitHub
        /// </summary>
        public async Task UploadPackageBundle(string bundleContents)
        {
            var GHClient = _authService.CreateGitHubClient();
            if (GHClient is null)
                throw new Exception("The GitHub user is not authenticated");

            User user = await GHClient.User.Current();

            var candidates = await GHClient.Gist.GetAllForUser(user.Login);
            Gist? existingBackup = candidates.FirstOrDefault(g => g.Description.EndsWith(GistDescription_EndingKey));

            if (existingBackup is null)
            {
                Logger.Warn($"No matching gist was found as a valid backup, a new gist will be created...");
                existingBackup = await _createBackupGistAsync(GHClient);
            }

            await _updateBackupGistAsync(GHClient, existingBackup, bundleContents);
            Logger.Info($"Cloud backup completed successfully to gist {user.Login}/{existingBackup.Id}");
        }

        /// <summary>
        /// Upload the given payload to the given gist.
        /// Updates the existing file if GistFileKey exists, creates a new one otherwhise.
        /// </summary>
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
        /// Creates a new Gist, prepared to be detectable by UniGetUI, and with the base readme file
        /// </summary>
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

        /// <summary>
        /// Retrieves a list of available backups to import
        /// </summary>
        public async Task<IEnumerable<string>> GetAvailableBackups()
        {
            var GHClient = _authService.CreateGitHubClient();
            if (GHClient is null)
                throw new Exception("The GitHub user is not authenticated");


            User user = await GHClient.User.Current();

            var candidates = await GHClient.Gist.GetAllForUser(user.Login);
            Gist? existingBackup = candidates.FirstOrDefault(g => g.Description.EndsWith(GistDescription_EndingKey));

            return existingBackup?.Files
                .Where(f => f.Key.StartsWith(PackageBackup_StartingKey))
                .Select(f => $"{f.Key.Split(' ')[^1]} ({CoreTools.FormatAsSize(f.Value.Size)})") ?? [];
        }

        /// <summary>
        /// For the given backupName, retrieve the backup contents
        /// </summary>
        public async Task<string?> GetBackupContents(string backupName)
        {
            var GHClient = _authService.CreateGitHubClient();
            if (GHClient is null)
                throw new Exception("The GitHub user is not authenticated");

            User user = await GHClient.User.Current();

            var candidates = await GHClient.Gist.GetAllForUser(user.Login);
            Gist? existingBackup = candidates.FirstOrDefault(g => g.Description.EndsWith(GistDescription_EndingKey));
            if (existingBackup is null)
                throw new Exception($"The backup {backupName} was not found");

            existingBackup = await GHClient.Gist.Get(existingBackup.Id);
            return existingBackup.Files
                .FirstOrDefault(f => f.Key.StartsWith(PackageBackup_StartingKey) && f.Key.EndsWith(backupName))
                .Value.Content;
        }
    }
}
