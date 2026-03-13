using System.Text.Json;
using System.Runtime.Versioning;
using Microsoft.Management.Deployment;
using WindowsPackageManager.Interop;

sealed class AnalysisOptions
{
    public string SourceName { get; init; } = "winget";

    public string? PackageId { get; init; }

    public string? OutputPath { get; init; }

    public int Skip { get; init; }

    public int Take { get; init; }
}

sealed class AnalysisOutput
{
    public required string SourceName { get; init; }

    public required int TotalPackages { get; init; }

    public required int ProcessedPackages { get; init; }

    public required PackageIconResult[] Packages { get; init; }

    public required PackageFailure[] Failures { get; init; }
}

sealed class PackageIconResult
{
    public required string PackageId { get; init; }

    public required string PackageVersion { get; init; }

    public required int IconCount { get; init; }

    public required PackageIcon[] Icons { get; init; }
}

sealed class PackageIcon
{
    public required string IconUrl { get; init; }

    public required string IconFileType { get; init; }

    public required string IconResolution { get; init; }

    public required string IconTheme { get; init; }

    public string? IconSha256 { get; init; }
}

sealed class PackageFailure
{
    public required string PackageId { get; init; }

    public required string Error { get; init; }
}

 [SupportedOSPlatform("windows5.0")]
internal static class Program
{
    public static int Main(string[] args)
    {
        var options = ParseArguments(args);
        var output = AnalyzePublishedCatalog(options);
        var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });

        if (!string.IsNullOrWhiteSpace(options.OutputPath))
        {
            var outputDirectory = Path.GetDirectoryName(options.OutputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            File.WriteAllText(options.OutputPath, json);
        }
        else
        {
            Console.WriteLine(json);
        }

        return 0;
    }

    private static AnalysisOptions ParseArguments(string[] args)
    {
        string sourceName = "winget";
        string? packageId = null;
        string? outputPath = null;
        var skip = 0;
        var take = 0;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--source-name":
                    sourceName = GetArgumentValue(args, ++index, "--source-name");
                    break;
                case "--package-id":
                    packageId = GetArgumentValue(args, ++index, "--package-id");
                    break;
                case "--output":
                    outputPath = GetArgumentValue(args, ++index, "--output");
                    break;
                case "--skip":
                    skip = int.Parse(GetArgumentValue(args, ++index, "--skip"));
                    break;
                case "--take":
                    take = int.Parse(GetArgumentValue(args, ++index, "--take"));
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{args[index]}'.");
            }
        }

        return new AnalysisOptions
        {
            SourceName = sourceName,
            PackageId = packageId,
            OutputPath = outputPath,
            Skip = skip,
            Take = take,
        };
    }

    private static string GetArgumentValue(string[] args, int index, string argumentName)
    {
        if (index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
        {
            throw new ArgumentException($"Missing value for '{argumentName}'.");
        }

        return args[index];
    }

    private static AnalysisOutput AnalyzePublishedCatalog(AnalysisOptions options)
    {
        WindowsPackageManagerStandardFactory factory;
        PackageManager packageManager;
        try
        {
            factory = new WindowsPackageManagerStandardFactory();
            packageManager = factory.CreatePackageManager();
        }
        catch
        {
            factory = new WindowsPackageManagerStandardFactory(allowLowerTrustRegistration: true);
            packageManager = factory.CreatePackageManager();
        }

        PackageCatalogReference? catalogReference = null;
        var catalogs = packageManager.GetPackageCatalogs();
        for (var index = 0; index < catalogs.Count; index++)
        {
            var candidate = catalogs[index];
            if (string.Equals(candidate.Info.Name, options.SourceName, StringComparison.OrdinalIgnoreCase))
            {
                catalogReference = candidate;
                break;
            }
        }

        if (catalogReference is null)
        {
            throw new InvalidOperationException($"Could not find a package source named '{options.SourceName}'.");
        }

        catalogReference.AcceptSourceAgreements = true;
        var connectResult = catalogReference.Connect();
        if (connectResult.Status != ConnectResultStatus.Ok)
        {
            throw new InvalidOperationException(
                $"Failed to connect to '{options.SourceName}'. Status: {connectResult.Status}."
            );
        }

        var findPackagesOptions = factory.CreateFindPackagesOptions();
        findPackagesOptions.ResultLimit = 0;

        if (!string.IsNullOrWhiteSpace(options.PackageId))
        {
            var packageFilter = factory.CreatePackageMatchFilter();
            packageFilter.Field = PackageMatchField.Id;
            packageFilter.Option = PackageFieldMatchOption.EqualsCaseInsensitive;
            packageFilter.Value = options.PackageId;
            findPackagesOptions.Filters.Add(packageFilter);
        }

        Console.Error.WriteLine($"Enumerating packages from source '{options.SourceName}'...");
        var matches = connectResult.PackageCatalog.FindPackages(findPackagesOptions).Matches;
        var startIndex = Math.Clamp(options.Skip, 0, matches.Count);
        var endExclusive = options.Take > 0
            ? Math.Min(startIndex + options.Take, matches.Count)
            : matches.Count;
        var scheduledPackageCount = endExclusive - startIndex;
        Console.Error.WriteLine(
            $"Found {matches.Count} package(s). Inspecting metadata for {scheduledPackageCount} package(s)..."
        );

        var packagesWithIcons = new List<PackageIconResult>();
        var failures = new List<PackageFailure>();
        for (var index = startIndex; index < endExclusive; index++)
        {
            var match = matches[index];
            var package = match.CatalogPackage;
            var packageId = package.Id;

            try
            {
                var availableVersions = package.AvailableVersions;
                if (availableVersions is null || availableVersions.Count == 0)
                {
                    continue;
                }

                var latestVersion = availableVersions[0];
                var version = latestVersion.Version;
                var metadata = package.GetPackageVersionInfo(latestVersion).GetCatalogPackageMetadata();
                var metadataIcons = metadata.Icons;
                if (metadataIcons.Count == 0)
                {
                    continue;
                }

                var icons = new List<PackageIcon>(metadataIcons.Count);
                for (var iconIndex = 0; iconIndex < metadataIcons.Count; iconIndex++)
                {
                    var icon = metadataIcons[iconIndex];
                    if (icon?.Url is null)
                    {
                        continue;
                    }

                    icons.Add(
                        new PackageIcon
                        {
                            IconUrl = icon.Url,
                            IconFileType = icon.FileType.ToString(),
                            IconResolution = icon.Resolution.ToString(),
                            IconTheme = icon.Theme.ToString(),
                            IconSha256 = icon.Sha256 is null || icon.Sha256.Length == 0
                                ? null
                                : Convert.ToHexString(icon.Sha256),
                        }
                    );
                }

                if (icons.Count == 0)
                {
                    continue;
                }

                packagesWithIcons.Add(
                    new PackageIconResult
                    {
                        PackageId = packageId,
                        PackageVersion = version,
                        IconCount = icons.Count,
                        Icons = icons.ToArray(),
                    }
                );
            }
            catch (Exception exception)
            {
                failures.Add(
                    new PackageFailure
                    {
                        PackageId = packageId,
                        Error = exception.Message,
                    }
                );
            }

            var processedCount = (index - startIndex) + 1;
            if (processedCount % 100 == 0 || index == endExclusive - 1)
            {
                Console.Error.WriteLine($"Processed {processedCount}/{scheduledPackageCount} package(s)...");
            }
        }

        return new AnalysisOutput
        {
            SourceName = options.SourceName,
            TotalPackages = matches.Count,
            ProcessedPackages = scheduledPackageCount - failures.Count,
            Packages = packagesWithIcons.OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase).ToArray(),
            Failures = failures.OrderBy(failure => failure.PackageId, StringComparer.OrdinalIgnoreCase).ToArray(),
        };
    }
}