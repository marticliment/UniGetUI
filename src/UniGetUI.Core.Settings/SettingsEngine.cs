using System.Collections.Concurrent;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;

namespace UniGetUI.Core.SettingsEngine
{
    public static partial class Settings
    {
        private static readonly ConcurrentDictionary<string, bool> booleanSettings = new();
        private static readonly ConcurrentDictionary<string, string> valueSettings = new();

        public static bool Get(string setting, bool invert = false)
        {
            if (booleanSettings.TryGetValue(setting, out bool result))
            {   // If the setting was cached
                return result ^ invert;
            }

            // Otherwise, load the value from disk and cache that setting
            result = File.Exists(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, setting));
            booleanSettings[setting] = result;
            return result ^ invert;
        }

        public static void Set(string setting, bool value)
        {
            try
            {
                // Cache that setting's new value
                booleanSettings[setting] = value;

                // Update changes on disk if applicable
                if (value && !File.Exists(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, setting)))
                {
                    File.WriteAllText(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, setting), "");
                }
                else if (!value)
                {
                    valueSettings[setting] = "";

                    if (File.Exists(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, setting)))
                    {
                        File.Delete(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, setting));
                    }
                }

            }
            catch (Exception e)
            {
                Logger.Error($"CANNOT SET SETTING FOR setting={setting} enabled={value}");
                Logger.Error(e);
            }
        }

        public static string GetValue(string setting)
        {
            if (valueSettings.TryGetValue(setting, out string? value))
            {   // If the setting was cached
                return value;
            }

            // Otherwise, load the setting from disk and cache that setting
            value = "";
            if (File.Exists(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, setting)))
            {
                value = File.ReadAllText(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, setting));
            }

            valueSettings[setting] = value;
            return value;

        }

        public static void SetValue(string setting, string value)
        {
            try
            {
                if (value == String.Empty)
                {
                    Set(setting, false);
                    booleanSettings[setting] = false;
                    valueSettings[setting] = "";
                }
                else
                {
                    File.WriteAllText(Path.Join(CoreData.UniGetUIUserConfigurationDirectory, setting), value);
                    booleanSettings[setting] = true;
                    valueSettings[setting] = value;
                }
            }
            catch (Exception e)
            {
                Logger.Error($"CANNOT SET SETTING VALUE FOR setting={setting} enabled={value}");
                Logger.Error(e);
            }
        }
    }
}
