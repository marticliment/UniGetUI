using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;

namespace UniGetUI.PackageEngine.Classes.Packages.Classes;

public static class DesktopShortcutsDatabase
{
    public enum Status
    {
        Maintain, // The user has explicitly requested this shortcut not be deleted
        Delete, // The user has allowed the shortcut to be deleted
        Unknown, // The user has not said whether they want this shortcut to be deleted
    }

    private static readonly List<string> UnknownShortcuts = [];

    public static IReadOnlyDictionary<string, bool> GetDatabase()
    {
        return Settings.GetDictionary<string, bool>("DeletableDesktopShortcuts") ?? new Dictionary<string, bool>();
    }

    public static void ResetDatabase()
    {
        Settings.ClearDictionary("DeletableDesktopShortcuts");
    }

    /// <summary>
    /// Adds a desktop shortcut to the deletable desktop shortcuts database
    /// </summary>
    /// <param name="shortcutPath">The path of the shortcut to delete</param>
    /// <param name="deletable">Whether or not to mark this entry as deletable in the databse. Defaults to true</param>
    public static void AddToDatabase(string shortcutPath, bool deletable = true)
    {
        Settings.SetDictionaryItem("DeletableDesktopShortcuts", shortcutPath, deletable);
    }

    /// <summary>
    /// Attempts to remove the given shortcut path from the database
    /// </summary>
    /// <param name="shortcutPath">The path of the shortcut to delete</param>
    /// <returns>True if the shortcut was removed, false if it was not there from the beginning</returns>
    public static bool Remove(string shortcutPath)
    {
        // Remove the entry if present
        if (Settings.DictionaryContainsKey<string, bool>("DeletableDesktopShortcuts", shortcutPath))
        {
            // Remove the entry and propagate changes to disk
            Settings.SetDictionaryItem("DeletableDesktopShortcuts", shortcutPath, false);
            return true;
        }
        else
        {
            // Do nothing if the entry was not there
            Logger.Warn($"Attempted to remove from deletable desktop shortcuts a shortcut {{shortcutPath={shortcutPath}}} that was not found there");
            return false;
        }
    }

    /// <summary>
    /// Attempts to reset the configuration of a given shortcut path from the database.
    /// This will make it so the user is asked about it the next time it is discovered.
    /// Different from `Remove` as Remove simply marks it as non-deletable, whereas this removes the configuration entirely.
    /// </summary>
    /// <param name="shortcutPath">The path of the shortcut to delete</param>
    /// <returns>True if the shortcut was completely removed, false if it was not there from the beginning</returns>
    public static bool ResetShortcut(string shortcutPath)
    {
        // Remove the entry if present
        if (Settings.DictionaryContainsKey<string, bool>("DeletableDesktopShortcuts", shortcutPath))
        {
            // Remove the entry and propagate changes to disk
            Settings.RemoveDictionaryKey<string, bool>("DeletableDesktopShortcuts", shortcutPath);
            return true;
        }
        else
        {
            // Do nothing if the entry was not there
            Logger.Warn($"Attempted to reset a deletable desktop shortcut {{shortcutPath={shortcutPath}}} that was not found there");
            return false;
        }
    }

    /// <summary>
    /// Attempts to delete the given shortcut path off the disk
    /// </summary>
    /// <param name="shortcutPath">The path of the shortcut to delete</param>
    /// <returns>True if the shortcut was deleted, false if it was not (or didn't exist)</returns>
    public static bool DeleteFromDisk(string shortcutPath)
    {
        Logger.Info("Deleting shortcut " + shortcutPath);
        try
        {
            File.Delete(shortcutPath);
            return true;
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to delete shortcut {{shortcutPath={shortcutPath}}}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks whether a given shortcut can be deleted.
    /// If a user has provided on opinion on whether or not the shortcut can be deleted, it will be returned.
    /// Otherwise, `ShortcutDeletableStatus.Unknown` will be returned and the shortcut should not be deleted
    /// until a choice is given to the user and they explicitly request that it be deleted.
    /// </summary>
    /// <param name="shortcutPath">The path of the shortcut to be deleted</param>
    /// <returns>True if the package is ignored, false otherwhise</returns>
    public static Status GetStatus(string shortcutPath)
    {
        // Check if the package is ignored
        if (Settings.DictionaryContainsKey<string, bool>("DeletableDesktopShortcuts", shortcutPath))
        {
            bool canDelete = Settings.GetDictionaryItem<string, bool>("DeletableDesktopShortcuts", shortcutPath);
            return canDelete ? Status.Delete : Status.Maintain;
        }
        else
        {
            return Status.Unknown;
        }
    }

    /// <summary>
    /// Get a list of shortcuts (.lnk files only) currently on the user's desktop
    /// </summary>
    /// <returns>A list of desktop shortcut paths</returns>
    public static List<string> GetShortcuts()
    {
        List<string> shortcuts = new();
        string UserDesktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string CommonDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
        shortcuts.AddRange(Directory.EnumerateFiles(UserDesktop, "*.lnk"));
        shortcuts.AddRange(Directory.EnumerateFiles(CommonDesktop, "*.lnk"));
        return shortcuts;
    }

    /// <summary>
    /// Remove a shortcut from the list of shortcuts whose deletion verdicts are unknown (as in, the user needs to be asked about deleting them when their operations finish)
    /// </summary>
    /// <param name="shortcutPath">The path of the shortcut to be asked about deletion</param>
    /// <returns>True if it was found, false otherwise</returns>
    public static bool RemoveFromUnknownShortcuts(string shortcutPath)
    {
        return UnknownShortcuts.Remove(shortcutPath);
    }

    /// <summary>
    /// Get the list of shortcuts whose deletion verdicts are unknown (as in, the user needs to be asked about deleting them when their operations finish)
    /// </summary>
    /// <returns>The list of shortcuts awaiting verdicts</returns>
    public static List<string> GetUnknownShortcuts()
    {
        return UnknownShortcuts;
    }

    /// <summary>
    /// Will attempt to remove new desktop shortcuts, if applicable.
    /// </summary>
    /// <param name="PreviousShortCutList"></param>
    public static void TryRemoveNewShortcuts(IEnumerable<string> PreviousShortCutList)
    {
        HashSet<string> ShortcutSet = PreviousShortCutList.ToHashSet();
        List<string> CurrentShortcutList = DesktopShortcutsDatabase.GetShortcuts();
        foreach (string shortcut in CurrentShortcutList)
        {
            if (ShortcutSet.Contains(shortcut)) continue;
            switch (DesktopShortcutsDatabase.GetStatus(shortcut))
            {
                case Status.Delete:
                    DesktopShortcutsDatabase.DeleteFromDisk(shortcut);
                    break;
                case Status.Maintain:
                    Logger.Debug("Refraining from deleting new shortcut " + shortcut + ": user disabled its deletion");
                    break;
                case Status.Unknown:
                    if(UnknownShortcuts.Contains(shortcut)) continue;
                    Logger.Info("Marking the shortcut " + shortcut + " to be asked to be deleted");
                    UnknownShortcuts.Add(shortcut);
                    break;
            }
        }
    }
}
