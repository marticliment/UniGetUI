using System.Collections.Concurrent;
using System.Data;
using System.Text.Json;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;

namespace UniGetUI.PackageEngine.Classes.Packages.Classes;

public static class DesktopShortcutsDatabase
{
    public enum ShortcutDeletableStatus
    {
        Undeletable, // The user has explicitly requested this shortcut not be deleted
        Deletable, // The user has allowed the shortcut to be deleted
        Unknown, // The user has not said whether they want this shortcut to be deleted
    }

    private static ConcurrentDictionary<string, bool>? DeletableDesktopShortcuts;
    private static List<string> AwaitingVerdictShortcuts = [];

    private static ConcurrentDictionary<string, bool> ReadDatabase()
    {
        Logger.Info("Deletable desktop shortcuts database was never loaded, so it is going to be loaded now");

        try
        {
            var rawContents = File.ReadAllText(CoreData.DesktopShortcutsDatabaseFile);
            return JsonSerializer.Deserialize<ConcurrentDictionary<string, bool>>(rawContents, options: CoreData.SerializingOptions)
                   ?? throw new InvalidExpressionException("Deserialization of Desktop Shortcuts file returned a null object");
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            Logger.Warn("The deletable desktop shortcuts database was corrupt, so it has been reset.");
            Logger.Debug("NOTE: Changes will only be reflected on-disk if the database is saved");
            return new();
        }
    }

    public static ConcurrentDictionary<string, bool> GetDatabase()
    {
        DeletableDesktopShortcuts ??= ReadDatabase();
        return DeletableDesktopShortcuts;
    }

    private static void SaveDatabase()
    {
        // Serialize and save to disk
        string rawContents = JsonSerializer.Serialize(DeletableDesktopShortcuts, options: CoreData.SerializingOptions);
        File.WriteAllText(CoreData.DesktopShortcutsDatabaseFile, rawContents);
    }

    public static void ResetDatabase()
    {
        DeletableDesktopShortcuts.Clear();
        SaveDatabase();
    }

    /// <summary>
    /// Adds a desktop shortcut to the deletable desktop shortcuts database
    /// </summary>
    /// <param name="shortcutPath">The path of the shortcut to delete</param>
    /// <param name="deletable">Whether or not to mark this entry as deletable in the databse. Defaults to true</param>
    public static void Add(string shortcutPath, bool deletable = true)
    {
        // Update the database if it is null
        DeletableDesktopShortcuts ??= ReadDatabase();

        // Add/update the new entry
        DeletableDesktopShortcuts[shortcutPath] = deletable;

        // Propagate changes to disk
        SaveDatabase();
    }

    /// <summary>
    /// Attempts to remove the given shortcut path from the database
    /// </summary>
    /// <param name="shortcutPath">The path of the shortcut to delete</param>
    /// <returns>True if the shortcut was removed, false if it was not there from the beginning</returns>
    public static bool Remove(string shortcutPath)
    {
        // Update the database if it is null
        DeletableDesktopShortcuts ??= ReadDatabase();

        // Remove the entry if present
        if (DeletableDesktopShortcuts.ContainsKey(shortcutPath))
        {
            // Remove the entry and propagate changes to disk
            DeletableDesktopShortcuts[shortcutPath] = false;
            SaveDatabase();
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
    public static bool Reset(string shortcutPath)
    {
        // Update the database if it is null
        DeletableDesktopShortcuts ??= ReadDatabase();

        // Remove the entry if present
        if (DeletableDesktopShortcuts.ContainsKey(shortcutPath))
        {
            // Remove the entry and propagate changes to disk
            DeletableDesktopShortcuts.Remove(shortcutPath, out _);
            SaveDatabase();
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
    public static bool DeleteShortcut(string shortcutPath)
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
    public static ShortcutDeletableStatus CanShortcutBeDeleted(string shortcutPath)
    {
        // Update the database if it is null
        DeletableDesktopShortcuts ??= ReadDatabase();

        // Check if the package is ignored
        if (DeletableDesktopShortcuts.TryGetValue(shortcutPath, out bool canDelete))
        {
            return canDelete ? ShortcutDeletableStatus.Deletable : ShortcutDeletableStatus.Undeletable;
        }
        else
        {
            return ShortcutDeletableStatus.Unknown;
        }
    }

    /// <summary>
    /// Get a list of shortcuts (.lnk files only) currently on the user's desktop
    /// </summary>
    /// <returns>A list of desktop shortcut paths</returns>
    public static List<string> GetDesktopShortcuts()
    {
        string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        // If you want to search in other places for desktop files, for example the start menu, just add to this list
        IEnumerable<string> Files = Directory.EnumerateFiles(DesktopPath);
        List<string> DesktopShortcuts = [];

        foreach (string file in Files)
        {
            if (Path.GetExtension(file) == ".lnk")
            {
                DesktopShortcuts.Add(file);
            }
        }

        return DesktopShortcuts;
    }

    /// <summary>
    /// Add a shortcut to a list of shortcuts whose deletion verdicts are unknown (as in, the user needs to be asked about deleting them when their operations finish)
    /// </summary>
    /// <param name="shortcutPath">The path of the shortcut to be asked about deletion</param>
    public static void AddToAwaitingVerdicts(string shortcutPath)
    {
        AwaitingVerdictShortcuts.Add(shortcutPath);
    }

    /// <summary>
    /// Remove a shortcut from the list of shortcuts whose deletion verdicts are unknown (as in, the user needs to be asked about deleting them when their operations finish)
    /// </summary>
    /// <param name="shortcutPath">The path of the shortcut to be asked about deletion</param>
    /// <returns>True if it was found, false otherwise</returns>
    public static bool RemoveFromAwaitingVerdicts(string shortcutPath)
    {
        return AwaitingVerdictShortcuts.Remove(shortcutPath);
    }

    /// <summary>
    /// Get the list of shortcuts whose deletion verdicts are unknown (as in, the user needs to be asked about deleting them when their operations finish)
    /// </summary>
    /// <returns>The list of shortcuts awaiting verdicts</returns>
    public static List<string> GetAwaitingVerdicts()
    {
        return AwaitingVerdictShortcuts;
    }
}
