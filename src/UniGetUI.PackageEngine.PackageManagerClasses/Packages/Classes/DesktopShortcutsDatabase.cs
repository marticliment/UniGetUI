using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;

namespace UniGetUI.PackageEngine.Classes.Packages.Classes;

public static class DesktopShortcutsDatabase
{
    public enum Status
    {
        Maintain, // The user has explicitly requested this shortcut not be deleted
        Unknown, // The user has not said whether they want this shortcut to be deleted
        Delete, // The user has allowed the shortcut to be deleted
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
    /// <param name="shortcutStatus">The status to set</param>
    public static void AddToDatabase(string shortcutPath, Status shortcutStatus)
    {
        if (shortcutStatus is Status.Unknown)
            Settings.RemoveDictionaryKey<string, bool>("DeletableDesktopShortcuts", shortcutPath);
        else
            Settings.SetDictionaryItem<string, bool>("DeletableDesktopShortcuts", shortcutPath, shortcutStatus is Status.Delete);
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

        // Do nothing if the entry was not there
        Logger.Warn($"Attempted to remove from deletable desktop shortcuts a shortcut {{shortcutPath={shortcutPath}}} that was not found there");
        return false;
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
    /// <returns>The status of a shortcut</returns>
    public static Status GetStatus(string shortcutPath)
    {
        // Check if the package is ignored
        if (Settings.DictionaryContainsKey<string, bool>("DeletableDesktopShortcuts", shortcutPath))
        {
            bool canDelete = Settings.GetDictionaryItem<string, bool>("DeletableDesktopShortcuts", shortcutPath);
            return canDelete ? Status.Delete : Status.Maintain;
        }

        return Status.Unknown;
    }

    /// <summary>
    /// Get a list of shortcuts (.lnk files only) currently on the user's desktop
    /// </summary>
    /// <returns>A list of desktop shortcut paths</returns>
    public static List<string> GetShortcutsOnDisk()
    {
        List<string> shortcuts = [];
        string UserDesktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string CommonDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
        shortcuts.AddRange(Directory.EnumerateFiles(UserDesktop, "*.lnk"));
        shortcuts.AddRange(Directory.EnumerateFiles(CommonDesktop, "*.lnk"));
        return shortcuts;
    }

    /// <summary>
    /// Gets all the shortcuts exist on disk or/and on the database
    /// </summary>
    /// <returns></returns>
    public static List<string> GetAllShortcuts()
    {
        var shortcuts = GetShortcutsOnDisk();

        foreach (var item in Settings.GetDictionary<string, bool>("DeletableDesktopShortcuts"))
        {
            if (!shortcuts.Contains(item.Key))
                shortcuts.Add(item.Key);
        }

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
    /// Will handle the removal, if applicable, of any shortcut that is not present on the given PreviousShortCutList.
    /// </summary>
    /// <param name="PreviousShortCutList">The shortcuts that already existed</param>
    public static void HandleNewShortcuts(IReadOnlyList<string> PreviousShortCutList)
    {
        bool DeleteUnknownShortcuts = Settings.Get("RemoveAllDesktopShortcuts");
        HashSet<string> PreviousShortcuts = [.. PreviousShortCutList];
        List<string> CurrentShortcuts = GetShortcutsOnDisk();

        foreach (string shortcut in CurrentShortcuts)
        {
            var status = GetStatus(shortcut);
            if (status is Status.Maintain)
            {
                // Don't delete this shortcut, it has been set to be kept
            }
            else if (status is Status.Delete)
            {
                // If a shortcut is set to be deleted, delete it,
                // even when it was not created during an UniGetUI operation
                DeleteFromDisk(shortcut);
            }
            else if (status is Status.Unknown)
            {
                // If a shortcut has not been detected yet, and it
                // existed before an operation started, then do nothing.
                if(PreviousShortcuts.Contains(shortcut))
                    continue;

                if (DeleteUnknownShortcuts)
                {
                    // If the shortcut was created during an operation
                    // and autodelete is enabled, delete that icon
                    Logger.Warn($"New shortcut {shortcut} will be set for deletion (this shortcut was never seen before)");
                    AddToDatabase(shortcut, Status.Delete);
                    DeleteFromDisk(shortcut);
                }
                else
                {
                    // Mark the shortcut as unknown and prompt the user.
                    if (!UnknownShortcuts.Contains(shortcut))
                    {
                        Logger.Info($"Marking the shortcut {shortcut} to be asked to be deleted");
                        UnknownShortcuts.Add(shortcut);
                    }
                }
            }
        }
    }
}
