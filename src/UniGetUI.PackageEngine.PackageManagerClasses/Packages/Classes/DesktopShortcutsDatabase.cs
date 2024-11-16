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

    /// <summary>
    /// Adds a desktop shortcut to the deletable desktop shortcuts database
    /// </summary>
    /// <param name="shortcutPath">The path of the shortcut to delete</param>
    public static void Add(string shortcutPath)
    {
        // Update the database if it is null
        DeletableDesktopShortcuts ??= ReadDatabase();

        // Add/update the new entry
        DeletableDesktopShortcuts[shortcutPath] = true;

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
}
