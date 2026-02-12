using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.Classes;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;
using Enums = UniGetUI.PackageEngine.Enums;

namespace UniGetUI.PackageEngine.Managers.GitHubCliManager;

public partial class GitHubCli : PackageManager
{
    private const string UnknownVersion = "Unknown";
    private static readonly Uri GitHubUrl = new("https://github.com/");
    private readonly ConcurrentDictionary<string, bool> _hasReleasesCache = new(StringComparer.OrdinalIgnoreCase);

    [GeneratedRegex("^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$")]
    private static partial Regex RepositoryIdRegex();

    public GitHubCli()
    {
        Dependencies =
        [
            new ManagerDependency(
                "GitHub CLI",
                CoreData.PowerShell5,
                "-ExecutionPolicy Bypass -NoLogo -NoProfile -Command \"& {winget install --id GitHub.cli --exact --source winget --accept-source-agreements --accept-package-agreements --force; if($error.count -ne 0){pause}}\"",
                "winget install --id GitHub.cli --exact --source winget",
                async () => (await CoreTools.WhichAsync("gh.exe")).Item1)
        ];

        Capabilities = new ManagerCapabilities
        {
            CanDownloadInstaller = true,
            CanRunAsAdmin = true,
            CanRunInteractively = true,
            SupportsCustomPackageIcons = true,
            SupportsProxy = ProxySupport.No,
            SupportsProxyAuth = false
        };

        var source = new ManagerSource(this, "GitHub", GitHubUrl);
        Properties = new ManagerProperties
        {
            Name = "GitHubCLI",
            DisplayName = "GitHub CLI",
            Description = CoreTools.Translate("GitHub's command-line tool. Search repositories and automatically download the latest release assets."),
            IconId = IconType.Download,
            ColorIconId = "github",
            ExecutableFriendlyName = "gh.exe",
            InstallVerb = "release",
            UninstallVerb = "--version",
            UpdateVerb = "release",
            DefaultSource = source,
            KnownSources = [source]
        };

        DetailsHelper = new GitHubCliPkgDetailsHelper(this);
        OperationHelper = new GitHubCliPkgOperationHelper(this);
    }

    protected override IReadOnlyList<Package> FindPackages_UnSafe(string query)
    {
        var safeQuery = CoreTools.EnsureSafeQueryString(query);
        if (string.IsNullOrWhiteSpace(safeQuery))
            return [];

        JsonNode? node = RunJsonCommand(
            $"search repos \"{safeQuery}\" --limit 50 --json nameWithOwner",
            Enums.LoggableTaskType.FindPackages);

        JsonArray? repos = node as JsonArray;
        if (repos is null)
        {
            repos = SearchRepositoriesViaApi(safeQuery);
            if (repos is null)
                return [];
        }

        List<Package> packages = [];
        foreach (string repositoryId in repos
                     .Select(repoNode => repoNode?["nameWithOwner"]?.ToString()
                                        ?? repoNode?["full_name"]?.ToString())
                     .Where(IsValidRepositoryId)
                     .Select(repositoryId => repositoryId!))
        {
            if (!HasReleases(repositoryId, Enums.LoggableTaskType.FindPackages))
                continue;

            packages.Add(new Package(repositoryId, repositoryId, UnknownVersion, DefaultSource, this));
        }
        return packages;
    }

    private JsonArray? SearchRepositoriesViaApi(string safeQuery)
    {
        JsonNode? node = RunJsonCommand(
            $"api search/repositories -f q=\"{safeQuery}\" -f per_page=50",
            Enums.LoggableTaskType.FindPackages);

        if (node is not JsonObject root || root["items"] is not JsonArray items)
            return null;

        return items;
    }

    protected override IReadOnlyList<Package> GetAvailableUpdates_UnSafe()
    {
        Dictionary<string, string> trackedRepositories = GetTrackedRepositories();
        HashSet<string> repositoryIds = [.. trackedRepositories.Keys];

        foreach (string watched in GetWatchedRepositories())
            repositoryIds.Add(watched);

        List<Package> updates = [];
        bool trackedRepositoriesChanged = false;

        foreach (string repositoryId in repositoryIds)
        {
            string currentVersion = trackedRepositories.GetValueOrDefault(repositoryId, "");
            string? latestVersion = GetLatestReleaseTag(repositoryId, Enums.LoggableTaskType.ListUpdates);
            if (string.IsNullOrWhiteSpace(latestVersion))
                continue;

            if (string.IsNullOrWhiteSpace(currentVersion) || currentVersion == UnknownVersion)
            {
                trackedRepositories[repositoryId] = latestVersion;
                trackedRepositoriesChanged = true;
                continue;
            }

            if (latestVersion != currentVersion)
            {
                updates.Add(new Package(repositoryId, repositoryId, currentVersion, latestVersion, DefaultSource, this));
            }
        }

        if (trackedRepositoriesChanged)
            SaveTrackedRepositories(trackedRepositories);

        return updates;
    }

    protected override IReadOnlyList<Package> GetInstalledPackages_UnSafe()
    {
        Dictionary<string, string> trackedRepositories = GetTrackedRepositories();
        HashSet<string> repositoryIds = [.. trackedRepositories.Keys];

        foreach (string watched in GetWatchedRepositories())
            repositoryIds.Add(watched);

        bool trackedRepositoriesChanged = false;
        List<Package> installedPackages = [];
        foreach (string repositoryId in repositoryIds)
        {
            string currentVersion = trackedRepositories.GetValueOrDefault(repositoryId, "");
            if (string.IsNullOrWhiteSpace(currentVersion))
            {
                currentVersion = GetLatestReleaseTag(repositoryId, Enums.LoggableTaskType.ListInstalledPackages) ?? UnknownVersion;
                if (currentVersion != UnknownVersion)
                {
                    trackedRepositories[repositoryId] = currentVersion;
                    trackedRepositoriesChanged = true;
                }
            }
            installedPackages.Add(new Package(repositoryId, repositoryId, currentVersion, DefaultSource, this));
        }

        if (trackedRepositoriesChanged)
            SaveTrackedRepositories(trackedRepositories);

        return installedPackages;
    }

    public override IReadOnlyList<string> FindCandidateExecutableFiles()
    {
        var candidates = CoreTools.WhichMultiple("gh.exe");
        foreach (string candidate in CoreTools.WhichMultiple("gh")
                     .Where(candidate => !candidates.Contains(candidate, StringComparer.OrdinalIgnoreCase)))
            candidates.Add(candidate);

        return candidates;
    }

    protected override void _loadManagerExecutableFile(out bool found, out string path, out string callArguments)
    {
        var (_found, executablePath) = GetExecutableFile();
        found = _found;
        path = executablePath;
        callArguments = "";
    }

    protected override void _loadManagerVersion(out string version)
    {
        using Process process = GetProcess("--version");
        process.Start();
        version = process.StandardOutput.ReadLine()?.Trim() ?? "";
        string error = process.StandardError.ReadToEnd();
        if (!string.IsNullOrWhiteSpace(error))
            Logger.Warn($"gh --version stderr: {error.Trim()}");
        process.WaitForExit();
    }

    internal static bool IsValidRepositoryId(string? repositoryId)
        => !string.IsNullOrWhiteSpace(repositoryId) && RepositoryIdRegex().IsMatch(repositoryId);

    private Process GetProcess(string extraArguments)
    {
        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Status.ExecutablePath,
                Arguments = $"{Status.ExecutableCallArgs} {extraArguments}".Trim(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            }
        };
    }

    internal JsonNode? RunJsonCommand(string extraArguments, Enums.LoggableTaskType taskType)
    {
        using Process process = GetProcess(extraArguments);
        IProcessTaskLogger logger = TaskLogger.CreateNew(taskType, process);
        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        if (!string.IsNullOrWhiteSpace(output))
            logger.AddToStdOut(output);
        if (!string.IsNullOrWhiteSpace(error))
            logger.AddToStdErr(error);

        process.WaitForExit();
        logger.Close(process.ExitCode);

        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            return null;

        try
        {
            return JsonNode.Parse(output);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to parse JSON output for manager {Name} and args \"{extraArguments}\"");
            Logger.Error(ex);
            return null;
        }
    }

    internal JsonObject? GetRepositoryInfo(string repositoryId, Enums.LoggableTaskType taskType)
    {
        if (!IsValidRepositoryId(repositoryId))
            return null;

        return RunJsonCommand($"api repos/{repositoryId}", taskType) as JsonObject;
    }

    internal JsonObject? GetLatestReleaseInfo(string repositoryId, Enums.LoggableTaskType taskType)
    {
        if (!IsValidRepositoryId(repositoryId))
            return null;

        return RunJsonCommand($"api repos/{repositoryId}/releases/latest", taskType) as JsonObject;
    }

    internal string? GetLatestReleaseTag(string repositoryId, Enums.LoggableTaskType taskType)
    {
        JsonObject? release = GetLatestReleaseInfo(repositoryId, taskType);
        return release?["tag_name"]?.ToString();
    }

    internal bool HasReleases(string repositoryId, Enums.LoggableTaskType taskType)
    {
        if (!IsValidRepositoryId(repositoryId))
            return false;

        if (_hasReleasesCache.TryGetValue(repositoryId, out bool hasReleases))
            return hasReleases;

        JsonNode? node = RunJsonCommand($"api repos/{repositoryId}/releases?per_page=1", taskType);
        if (node is not JsonArray releases)
            return false;

        hasReleases = releases.Count > 0;
        _hasReleasesCache[repositoryId] = hasReleases;
        return hasReleases;
    }

    internal static string GetDownloadDirectory(string repositoryId)
    {
        string safeRepositoryId = CoreTools.MakeValidFileName(repositoryId.Replace('/', '_'));
        return Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "UniGetUI",
            "GitHub Releases",
            safeRepositoryId);
    }

    internal static string GetDefaultDownloadDirectory()
    {
        return Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");
    }

    private IReadOnlyList<string> GetWatchedRepositories()
    {
        JsonNode? node = RunJsonCommand(
            "api --paginate --slurp /user/subscriptions",
            Enums.LoggableTaskType.ListInstalledPackages);

        if (node is not JsonArray pages)
            return [];

        HashSet<string> repositories = [];
        foreach (JsonNode? pageNode in pages)
        {
            if (pageNode is not JsonArray repoArray)
                continue;

            repositories.UnionWith(
                repoArray
                    .Select(repoNode => repoNode?["full_name"]?.ToString())
                    .Where(IsValidRepositoryId)
                    .Select(fullName => fullName!));
        }

        return [.. repositories];
    }

    internal static Dictionary<string, string> GetTrackedRepositories()
    {
        Dictionary<string, string> repositories = [];
        IReadOnlyDictionary<string, string?> rawRepositories =
            Settings.GetDictionary<string, string>(Settings.K.GitHubCliTrackedRepositories);

        foreach ((string repositoryId, string? version) in rawRepositories)
        {
            if (!IsValidRepositoryId(repositoryId))
                continue;

            repositories[repositoryId] = version?.Trim() ?? "";
        }

        return repositories;
    }

    internal static void SaveTrackedRepositories(Dictionary<string, string> repositories)
    {
        Settings.SetDictionary(Settings.K.GitHubCliTrackedRepositories, repositories);
    }

    internal static void TrackRepository(string repositoryId, string version)
    {
        if (!IsValidRepositoryId(repositoryId))
            return;

        Dictionary<string, string> repositories = GetTrackedRepositories();
        repositories[repositoryId] = version;
        SaveTrackedRepositories(repositories);
    }

    internal static bool RemoveTrackedRepository(string repositoryId)
    {
        Dictionary<string, string> repositories = GetTrackedRepositories();
        bool removed = repositories.Remove(repositoryId);
        if (removed)
            SaveTrackedRepositories(repositories);
        return removed;
    }
}
