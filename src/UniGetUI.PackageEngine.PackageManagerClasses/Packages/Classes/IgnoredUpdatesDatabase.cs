using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Classes.Packages.Classes;

public static class IgnoredUpdatesDatabase
{
    public static IReadOnlyDictionary<string, string> GetDatabase()
    {
        return Settings.GetDictionary<string, string>("IgnoredPackageUpdates") ?? new Dictionary<string, string>();
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
        Settings.SetDictionaryItem("IgnoredPackageUpdates", ignoredId, version);
    }

    /// <summary>
    /// Attempts to remove the given package from the database
    /// </summary>
    /// <param name="ignoredId">The Ignored Identifier to check</param>
    /// <returns>True if the package was removed, false if it was not there from the beginning</returns>
    public static bool Remove(string ignoredId)
    {
        // Remove the entry if present
        if (Settings.DictionaryContainsKey<string, string>("IgnoredPackageUpdates", ignoredId))
        {
            // Remove the entry and propagate changes to disk
            return Settings.RemoveDictionaryKey<string, string>("IgnoredPackageUpdates", ignoredId) != null;
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
        string? ignoredVersion = Settings.GetDictionaryItem<string, string>("IgnoredPackageUpdates", ignoredId);

        // Check if the package is ignored
        return ignoredVersion == "*" || ignoredVersion == version;
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
        return Settings.GetDictionaryItem<string, string>("IgnoredPackageUpdates", ignoredId);
    }
}
