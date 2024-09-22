using System.Text.Json;
using UniGetUI.Core.Data;

namespace UniGetUI.PackageEngine.Managers.CargoManager;

record CargoManifest
{
    public CargoManifestCategory[]? categories { get; init; }
    public required CargoManifestCrate crate { get; init; }
    public required CargoManifestVersion[] versions { get; init; }
}

record CargoManifestCategory
{
    public required string category { get; init; }
    public required string description { get; init; }
    public required string id { get; init; }
}

record CargoManifestCrate
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

record CargoManifestVersion
{
    public string[]? bin_names { get; init; }
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

record CargoManifestVersionWrapper
{
    public required CargoManifestVersion version { get; init; }
}

class CargoManifestPublisher
{
    public string? avatar { get; init; }
    public required string name { get; init; }
    public string? url { get; init; }
}

class CratesIOClient
{
    public const string ApiUrl = "https://crates.io/api/v1";

    public static Tuple<Uri, CargoManifest> GetManifest(string packageId)
    {
        var manifestUrl = new Uri($"{ApiUrl}/crates/{packageId}");
        var manifest = Fetch<CargoManifest>(manifestUrl);
        if (manifest.crate is null)
        {
            throw new NullResponseException($"Null response for package {packageId}");
        }
        return Tuple.Create(manifestUrl, manifest);
    }

    public static CargoManifestVersion GetManifestVersion(string packageId, string version)
    {
        var manifestUrl = new Uri($"{ApiUrl}/crates/{packageId}/{version}");
        var manifest = Fetch<CargoManifestVersionWrapper>(manifestUrl);
        if (manifest.version is null)
        {
            throw new NullResponseException($"Null response for package {packageId}");
        }
        return manifest.version;
    }

    private static T Fetch<T>(Uri url)
    {
        HttpClient client = new(CoreData.GenericHttpClientParameters);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(CoreData.UserAgentString);

        var manifestStr = client.GetStringAsync(url).GetAwaiter().GetResult();

        var manifest = JsonSerializer.Deserialize<T>(manifestStr, options: CoreData.SerializingOptions)
                       ?? throw new NullResponseException($"Null response for request to {url}");
        return manifest;
    }
}

public class NullResponseException(string message) : Exception(message);
