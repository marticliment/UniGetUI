using System.Collections.Concurrent;
using System.Data;
using System.Text.Json;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Classes.Packages.Classes;

public static class IgnoredUpdatesDatabase
{
    private static ConcurrentDictionary<string, string>? IgnoredPackages;

    private static ConcurrentDictionary<string, string> ReadDatabase()
    {
        Logger.Info("Ignored updates database was never loaded, so it is going to be loaded now");
        try
        {
            var rawContents = File.ReadAllText(CoreData.IgnoredUpdatesDatabaseFile);
            return JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(rawContents, options: CoreData.SerializingOptions)
                   ?? throw new InvalidExpressionException("Deserialization of Ignored Updates file returned a null object");
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            Logger.Warn("The ignored packages database was corrupt, so it has been reset.");
            Logger.Debug("NOTE: Changes will only be reflected on-disk if the database is saved");
            return new();
        }
    }

    public static ConcurrentDictionary<string, string> GetDatabase()
    {
        IgnoredPackages ??= ReadDatabase();
        return IgnoredPackages;
    }

    private static void SaveDatabase()
    {
        // Serialize and save to disk
        string rawContents = JsonSerializer.Serialize(IgnoredPackages, options: CoreData.SerializingOptions);
        File.WriteAllText(CoreData.IgnoredUpdatesDatabaseFile, rawContents);
    }

    public static string GetIgnoredIdForPackage(IPackage package)
    {
        return $"{package.Manager.Properties.Name.ToLower()}\\{package.Id}";
    }

    /// <summary>
    /// Adds a package to the ignored packages database
    /// </summary>
    /// <param name="ignoredId">The Ignored Identifier to check</param>
    /// <param name="version">The version to ignore. Use wildcard (`*`) to ignore all versions</param>
    public static void Add(string ignoredId, string version = "*")
    {
        // Update the database if it is null
        IgnoredPackages ??= ReadDatabase();

        // Add/update the new entry
        IgnoredPackages[ignoredId] = version;

        // Propagate changes to disk
        SaveDatabase();
    }

    /// <summary>
    /// Attempts to remove the given package from the database
    /// </summary>
    /// <param name="ignoredId">The Ignored Identifier to check</param>
    /// <returns>True if the package was removed, false if it was not there from the beginning</returns>
    public static bool Remove(string ignoredId)
    {
        // Update the database if it is null
        IgnoredPackages ??= ReadDatabase();

        // Remove the entry if present
        if (IgnoredPackages.ContainsKey(ignoredId))
        {
            // Remove the entry and propagate changes to disk
            IgnoredPackages.TryRemove(ignoredId, out string? _);
            SaveDatabase();
            return true;
        }
        else
        {
            // Do nothing if the entry was not there
            Logger.Warn($"Attempted to remove from ignored updates a package {{ignoredId={ignoredId}}} that was not found there");
            return false;
        }
    }

    /// <summary>
    /// Checks whether a package version is ignored.
    /// A package version is considered ignored when:
    ///     a) All versions for that package are ignored.
    ///     b) That specific version is ignored
    ///
    /// You may use the wildcard (`*`) as the version to check if
    /// all versions are ignored.
    /// </summary>
    /// <param name="ignoredId">The Ignored Identifier to check</param>
    /// <param name="version">The version to check</param>
    /// <returns>True if the package is ignored, false otherwhise</returns>
    public static bool HasUpdatesIgnored(string ignoredId, string version = "*")
    {
        // Update the database if it is null
        IgnoredPackages ??= ReadDatabase();

        // Check if the package is ignored
        if (IgnoredPackages.TryGetValue(ignoredId, out string? ignoredVersion))
        {
            return ignoredVersion == "*" || ignoredVersion == version;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Retrieves the ignored version for a package
    /// </summary>
    /// <param name="ignoredId">The Ignored Identifier to check</param>
    /// <returns>The ignored version, as a string
    /// If **all** versions are ignored, the wildcard (`*`) will be returned.
    /// If **no** versions are ignored, null will be returned.</returns>
    public static string? GetIgnoredVersion(string ignoredId)
    {
        // Update the database if it is null
        IgnoredPackages ??= ReadDatabase();

        if (IgnoredPackages.TryGetValue(ignoredId, out string? ignoredVersion))
            return ignoredVersion;

        return null;
    }
}
