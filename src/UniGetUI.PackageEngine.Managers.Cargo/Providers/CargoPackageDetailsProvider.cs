using System.Text.Json;
using UniGetUI.Core.Data;
using UniGetUI.Core.IconEngine;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Managers.CargoManager;
internal sealed class CargoPackageDetailsProvider(Cargo manager) : BasePackageDetailsProvider<PackageManager>(manager)
{
    private const string ApiUrl = "https://crates.io/api/v1";

    protected override async Task GetPackageDetails_Unsafe(IPackageDetails details)
    {
        details.InstallerType = "Source";

        INativeTaskLogger logger = Manager.TaskLogger.CreateNew(Enums.LoggableTaskType.LoadPackageDetails);

        Uri manifestUrl;
        CargoManifest manifest;
        try
        {
            (manifestUrl, manifest) = await GetManifest(details.Package.Id);
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            logger.Close(1);
            return;
        }

        details.Description = manifest.crate.description;
        details.ManifestUrl = manifestUrl;

        var homepage = manifest.crate.homepage ?? manifest.crate.repository ?? manifest.crate.documentation;
        if (!string.IsNullOrEmpty(homepage))
        {
            details.HomepageUrl = new Uri(homepage);
        }

        var keywords = manifest.crate.keywords == null ? [] : (string[]) manifest.crate.keywords.Clone();
        var categories = manifest.categories?.Select(c => c.category);
        details.Tags = [.. keywords, .. categories];

        var versionData = manifest.versions.Where((v) => v.num == details.Package.Version).First();

        details.Author = versionData.published_by?.name;
        details.License = versionData.license;
        details.InstallerUrl = new Uri(ApiUrl + versionData.dl_path);
        details.InstallerSize = versionData.crate_size ?? 0;
        details.InstallerHash = versionData.checksum;
        details.Publisher = versionData.published_by?.name;
        details.UpdateDate = versionData.updated_at;

        // TODO: most packages are hosted on Github; see if there's a way to use the repository
        // info to extract release notes

        logger.Close(0);
        return;
    }

    protected override Task<CacheableIcon?> GetPackageIcon_Unsafe(IPackage package)
    {
        throw new NotImplementedException();
    }

    protected override Task<Uri[]> GetPackageScreenshots_Unsafe(IPackage package)
    {
        throw new NotImplementedException();
    }

    protected override async Task<string[]> GetPackageVersions_Unsafe(IPackage package)
    {
        INativeTaskLogger logger = Manager.TaskLogger.CreateNew(Enums.LoggableTaskType.LoadPackageVersions);
        try
        {
            var (_, manifest) = await GetManifest(package.Id);
            var versions = manifest.versions.Select((v) => v.num).ToArray();
            logger.Close(0);
            return versions;
        }
        catch (Exception ex)
        {
            logger.Error(ex);
            logger.Close(1);
            throw;
        }
    }

    private async Task<Tuple<Uri, CargoManifest>> GetManifest(string packageId)
    {
        var manifestUrl = new Uri($"{ApiUrl}/crates/{packageId}");

        HttpClient client = new(CoreData.GenericHttpClientParameters);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(CoreData.UserAgentString);

        var manifestStr = await client.GetStringAsync(manifestUrl);

        var manifest = JsonSerializer.Deserialize<CargoManifest>(manifestStr);
        if (manifest == null || manifest.crate == null)
        {
            throw new NullResponseException($"Null response for package {packageId} in manager {Manager.Name}");
        }
        return Tuple.Create(manifestUrl, manifest);
    }

    private record CargoManifest
    {
        public CargoManifestCategory[]? categories { get; init; }
        public required CargoManifestCrate crate { get; init; }
        public required CargoManifestVersion[] versions { get; init; }
    }

    private record CargoManifestCategory
    {
        public required string category { get; init; }
        public required string description { get; init; }
        public required string id { get; init; }
    }

    private record CargoManifestCrate
    {
        public string[]? categories { get; init; }
        public string? description { get; init; }
        public string? documentation { get; init; }
        public double? downloads { get; init; }
        public string? homepage { get; init; }
        public string[]? keywords { get; init; }
        public required string max_stable_version { get; init; }
        public required string max_version { get; init; }
        public required string name { get; init; }
        public required string newest_version { get; init; }
        public string? repository { get; init; }
        public string? updated_at { get; init; }
    }

    private record CargoManifestVersion
    {
        public required string checksum { get; init; }
        public double? crate_size { get; init; }
        public string? created_at { get; init; }
        public required string dl_path { get; init; }
        public string? license { get; init; }
        public required string num { get; init; }
        public CargoManifestPublisher? published_by { get; init; }
        public string? updated_at { get; init; }
        public bool yanked { get; init; }
    }

    private class CargoManifestPublisher
    {
        public string? avatar { get; init; }
        public required string name { get; init; }
        public string? url { get; init; }
    }
}

public class NullResponseException(string message) : Exception(message);
