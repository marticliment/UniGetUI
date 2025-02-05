using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Classes.Packages.Classes;

public static class IgnoredUpdatesDatabase
{
    public class PauseTime
    {
        private int _daysTill;
        public int Months { get { return Weeks / 4; } set { Weeks = value * 4; } }
        public int Weeks { get { return Days / 7; } set { Days = value * 7; } }
        public int Days { get { return _daysTill; } set { _daysTill = value; } }

        public string GetDateFromNow()
        {
            DateTime NewTime = DateTime.Now.AddDays(_daysTill);
            return NewTime.ToString("yyyy-MM-dd");
        }

        public void Parse(string Date)
        {
            try
            {
                DateTime ParsedDate = DateTime.ParseExact(Date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                DateTime Now = DateTime.Now;
                if (ParsedDate > Now)
                {
                    _daysTill = (int)(ParsedDate - Now).TotalDays;
                }
                else
                {
                    _daysTill = (int)(Now - ParsedDate).TotalDays;
                }
            }
            catch (FormatException ex)
            {
                Logger.Error($"Couldn't parse date {Date}:");
                Logger.Error(ex);
            }
        }

        public string StringRepresentation()
        {
            if (Months >= 12 && Months % 12 == 0)
            {
                int Years = Months / 12;
                if (Years > 1) return CoreTools.Translate("{0} years", Years);
                return CoreTools.Translate("1 year");
            }

            if (Months >= 1)
            {
                if (Months > 1) return CoreTools.Translate("{0} months", Months);
                return CoreTools.Translate("1 month");
            }

            if (Weeks >= 1)
            {
                if (Weeks > 1) return CoreTools.Translate("{0} weeks", Weeks);
                return CoreTools.Translate("1 week");
            }

            if (Days != 1) return CoreTools.Translate("{0} days", Days);
            return CoreTools.Translate("1 day");
        }
    }

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

        // Do nothing if the entry was not there
        Logger.Warn($"Attempted to remove from ignored updates a package {{ignoredId={ignoredId}}} that was not found there");
        return false;
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
    /// <returns>True if the package is ignored, false otherwise</returns>
    public static bool HasUpdatesIgnored(string ignoredId, string version = "*")
    {
        string? ignoredVersion = Settings.GetDictionaryItem<string, string>("IgnoredPackageUpdates", ignoredId);

        if (ignoredVersion != null && ignoredVersion.StartsWith("<"))
        {
            try
            {
                var ignoreDate = DateTime.ParseExact(ignoredVersion[1..], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                if (ignoreDate > DateTime.Now) return true;
                Remove(ignoredId);
            }
            catch (FormatException ex)
            {
                Logger.Error($"Couldn't parse update ignoration {ignoredVersion}:");
                Logger.Error(ex);
            }
        }

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
