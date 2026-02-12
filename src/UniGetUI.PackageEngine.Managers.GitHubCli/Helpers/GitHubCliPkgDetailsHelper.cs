using System.Text.Json.Nodes;
using System.Runtime.InteropServices;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Managers.GitHubCliManager;

internal sealed class GitHubCliPkgDetailsHelper : BasePkgDetailsHelper
{
    private readonly GitHubCli _manager;
    internal static readonly string[] PreferredExtensions =
    [
        ".exe",
        ".msi",
        ".msixbundle",
        ".msix",
        ".appx",
        ".zip"
    ];
    internal static readonly string[] AutoInstallableExtensions =
    [
        ".exe",
        ".msi",
        ".msixbundle",
        ".msix",
        ".appx"
    ];

    public GitHubCliPkgDetailsHelper(GitHubCli manager) : base(manager)
    {
        _manager = manager;
    }

    protected override void GetDetails_UnSafe(IPackageDetails details)
    {
        string repositoryId = details.Package.Id;
        if (!GitHubCli.IsValidRepositoryId(repositoryId))
            throw new InvalidDataException($"Repository id \"{repositoryId}\" is not valid");

        JsonObject? repository = _manager.GetRepositoryInfo(repositoryId, Enums.LoggableTaskType.LoadPackageDetails);
        JsonObject? release = _manager.GetLatestReleaseInfo(repositoryId, Enums.LoggableTaskType.LoadPackageDetails);

        details.ManifestUrl = new Uri($"https://github.com/{repositoryId}/releases");

        if (repository is not null)
            PopulateRepositoryDetails(repository, details);

        if (release is not null)
            PopulateReleaseDetails(release, details);

        if (details.InstallerUrl is not null)
            details.InstallerSize = CoreTools.GetFileSizeAsLong(details.InstallerUrl);
    }

    private static void PopulateRepositoryDetails(JsonObject repository, IPackageDetails details)
    {
        details.Description = repository["description"]?.ToString();

        string? owner = repository["owner"]?["login"]?.ToString();
        if (!string.IsNullOrWhiteSpace(owner))
        {
            details.Author = owner;
            details.Publisher = owner;
        }

        if (Uri.TryCreate(repository["html_url"]?.ToString(), UriKind.Absolute, out var homepageUrl))
            details.HomepageUrl = homepageUrl;

        details.License = repository["license"]?["spdx_id"]?.ToString();
        if (Uri.TryCreate(repository["license"]?["url"]?.ToString(), UriKind.Absolute, out var licenseUrl))
            details.LicenseUrl = licenseUrl;

        if (repository["topics"] is JsonArray topics)
        {
            details.Tags = topics
                .Where(topic => !string.IsNullOrWhiteSpace(topic?.ToString()))
                .Select(topic => topic!.ToString())
                .ToArray();
        }
    }

    private static void PopulateReleaseDetails(JsonObject release, IPackageDetails details)
    {
        details.UpdateDate = release["published_at"]?.ToString();
        details.ReleaseNotes = release["body"]?.ToString();
        if (Uri.TryCreate(release["html_url"]?.ToString(), UriKind.Absolute, out var releaseUrl))
            details.ReleaseNotesUrl = releaseUrl;

        var installerUrl = SelectInstallerUrl(release, out string? installerType);
        if (installerUrl is not null)
        {
            details.InstallerUrl = installerUrl;
            details.InstallerType = installerType;
        }
        else if (Uri.TryCreate(release["zipball_url"]?.ToString(), UriKind.Absolute, out var zipballUrl))
        {
            details.InstallerUrl = zipballUrl;
            details.InstallerType = "ZIP";
        }
    }

    private static Uri? SelectInstallerUrl(JsonObject release, out string? installerType)
    {
        installerType = null;
        JsonObject? selected = SelectBestAssetFromRelease(release);
        if (selected is null)
            return null;

        string? downloadUrl = selected["browser_download_url"]?.ToString();
        if (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out var installerUrl))
            return null;

        string fileName = selected["name"]?.ToString() ?? "";
        string extension = Path.GetExtension(fileName).Trim('.');
        installerType = string.IsNullOrWhiteSpace(extension)
            ? "GitHub release asset"
            : extension.ToUpperInvariant();

        return installerUrl;
    }

    internal static JsonObject? SelectBestAssetFromRelease(
        JsonObject release,
        bool autoInstallableOnly = false)
    {
        if (release["assets"] is not JsonArray assets || assets.Count == 0)
            return null;

        var candidates = assets.OfType<JsonObject>().ToList();
        if (autoInstallableOnly)
        {
            candidates = candidates.Where(asset =>
            {
                string extension = Path.GetExtension(asset["name"]?.ToString() ?? "");
                return AutoInstallableExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
            }).ToList();
        }

        return SelectBestAsset(
            candidates,
            autoInstallableOnly ? AutoInstallableExtensions : PreferredExtensions);
    }

    private static JsonObject? SelectBestAsset(
        IReadOnlyList<JsonObject> candidates,
        IReadOnlyList<string> preferredExtensions)
    {
        if (!candidates.Any())
            return null;

        var rankedAssets = candidates
            .Select((asset, index) =>
            {
                string name = asset["name"]?.ToString() ?? "";
                int score = ComputeAssetScore(name, preferredExtensions);
                return new
                {
                    Asset = asset,
                    Score = score,
                    Index = index
                };
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Index)
            .ToList();

        return rankedAssets.FirstOrDefault()?.Asset;
    }

    private static int ComputeAssetScore(string assetName, IReadOnlyList<string> preferredExtensions)
    {
        string name = assetName.ToLowerInvariant();
        int score = 0;

        int extensionPriority = GetExtensionPriority(name, preferredExtensions);
        score += extensionPriority >= 0 ? (300 - extensionPriority * 40) : -200;

        bool hasWindowsTag = ContainsAny(name, ["windows", "win32", "win64", "-win-", "_win_", ".win."]);
        bool hasLinuxTag = ContainsAny(name, ["linux", "ubuntu", "debian", "fedora", "rpm", "appimage", "musl"]);
        bool hasMacTag = ContainsAny(name, ["darwin", "macos", "osx"]);

        if (hasWindowsTag)
            score += 120;
        if (hasLinuxTag)
            score -= 220;
        if (hasMacTag)
            score -= 220;

        bool hasX64Tag = ContainsAny(name, ["x64", "amd64", "x86_64", "win64", "64-bit", "64bit"]);
        bool hasArm64Tag = ContainsAny(name, ["arm64", "aarch64"]);
        bool hasX86Tag = !hasX64Tag && ContainsAny(name, ["x86", "i386", "ia32", "win32", "32-bit", "32bit"]);

        score += RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => hasX64Tag ? 220 : hasArm64Tag ? -140 : hasX86Tag ? -30 : 40,
            Architecture.Arm64 => hasArm64Tag ? 260 : hasX64Tag ? -140 : hasX86Tag ? -180 : 30,
            Architecture.X86 => hasX86Tag ? 220 : hasX64Tag || hasArm64Tag ? -180 : 30,
            _ => 0
        };

        return score;
    }

    private static int GetExtensionPriority(string name, IReadOnlyList<string> preferredExtensions)
    {
        for (int i = 0; i < preferredExtensions.Count; i++)
        {
            if (name.EndsWith(preferredExtensions[i], StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static bool ContainsAny(string value, IReadOnlyList<string> patterns)
    {
        return patterns.Any(pattern => value.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    protected override IReadOnlyList<string> GetInstallableVersions_UnSafe(IPackage package)
    {
        return [];
    }

    protected override CacheableIcon? GetIcon_UnSafe(IPackage package)
    {
        JsonObject? repository = _manager.GetRepositoryInfo(package.Id, Enums.LoggableTaskType.LoadPackageDetails);
        string? avatarUrl = repository?["owner"]?["avatar_url"]?.ToString();
        if (!Uri.TryCreate(avatarUrl, UriKind.Absolute, out Uri? iconUrl))
            return null;

        return new CacheableIcon(iconUrl);
    }

    protected override IReadOnlyList<Uri> GetScreenshots_UnSafe(IPackage package)
    {
        return [];
    }

    protected override string? GetInstallLocation_UnSafe(IPackage package)
    {
        JsonObject? release = _manager.GetLatestReleaseInfo(package.Id, Enums.LoggableTaskType.LoadPackageDetails);
        bool canAutoInstall = release is not null &&
                              SelectBestAssetFromRelease(release, autoInstallableOnly: true) is not null;
        string downloadDirectory = canAutoInstall
            ? GitHubCli.GetDownloadDirectory(package.Id)
            : GitHubCli.GetDefaultDownloadDirectory();
        return Directory.Exists(downloadDirectory) ? downloadDirectory : null;
    }
}
