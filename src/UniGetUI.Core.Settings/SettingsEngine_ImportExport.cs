using System.Text.Json;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;

namespace UniGetUI.Core.SettingsEngine;

public partial class Settings
{
    public static void ExportToJSON(string path)
    {
        Dictionary<string, string> settings = [];
        foreach (string entry in Directory.EnumerateFiles(CoreData.UniGetUIUserConfigurationDirectory))
        {
            if(new[] {"OperationHistory", "WinGetAlreadyUpgradedPackages.json", "TelemetryClientToken", "CurrentSessionToken"}.Contains(entry.Split("\\")[^1]))
                continue;

            settings.Add(Path.GetFileName(entry), File.ReadAllText(entry));
        }

        File.WriteAllText(path, JsonSerializer.Serialize(settings, SerializationOptions));
    }

    public static async Task<string> ExportSettingsAsStringAsync()
    {
        Dictionary<string, string> settings = [];
        foreach (string entry in Directory.EnumerateFiles(CoreData.UniGetUIUserConfigurationDirectory))
        {
            if (new[] { "OperationHistory", "WinGetAlreadyUpgradedPackages.json", "TelemetryClientToken", "CurrentSessionToken" }.Contains(Path.GetFileName(entry)))
                continue;

            settings.Add(Path.GetFileName(entry), await File.ReadAllTextAsync(entry));
        }
        return JsonSerializer.Serialize(settings, SerializationOptions);
    }

    public static async Task ImportSettingsFromStringAsync(string jsonContent)
    {
        ResetSettings(); // Consider if a full reset is always desired before import.
                        // For cloud sync, usually yes.
        Dictionary<string, string> settings = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent, SerializationOptions) ?? [];
        foreach (KeyValuePair<string, string> entry in settings)
        {
            if (new[] { "OperationHistory", "WinGetAlreadyUpgradedPackages.json", "TelemetryClientToken", "CurrentSessionToken" }.Contains(entry.Key))
                continue;

            await File.WriteAllTextAsync(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, entry.Key), entry.Value);
        }
        Logger.Info("Settings successfully imported from string content.");
        // A restart might be needed for all settings to take effect.
        // This method itself won't trigger it, but the calling UI should inform the user.
    }

    public static void ImportFromJSON(string path)
    {
        if (Path.GetDirectoryName(path) == CoreData.UniGetUIUserConfigurationDirectory)
        {
            var tempLocation  = Directory.CreateTempSubdirectory();
            var newPath = Path.Join(tempLocation.FullName, Path.GetFileName(path));
            File.Copy(path, newPath);
            path = newPath;
        }

        ResetSettings();
        Dictionary<string, string> settings = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path), SerializationOptions) ?? [];
        foreach (KeyValuePair<string, string> entry in settings)
        {
            if(new[] {"OperationHistory", "WinGetAlreadyUpgradedPackages.json", "TelemetryClientToken", "CurrentSessionToken"}.Contains(entry.Key))
                continue;

            File.WriteAllText(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, entry.Key), entry.Value);
        }
    }

    public static void ResetSettings()
    {
        foreach (string entry in Directory.EnumerateFiles(CoreData.UniGetUIUserConfigurationDirectory))
        {
            try
            {
                if(new[] {"TelemetryClientToken"}.Contains(entry.Split("\\")[^1]))
                    continue;

                File.Delete(entry);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex);
            }
        }
    }
}
