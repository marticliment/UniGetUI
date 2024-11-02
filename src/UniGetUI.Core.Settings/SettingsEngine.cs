using System.Collections.Concurrent;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;

namespace UniGetUI.Core.SettingsEngine
{
    public static class Settings
    {
        private static ConcurrentDictionary<string, bool> booleanSettings = new();
        private static ConcurrentDictionary<string, string> valueSettings = new();

        public static bool Get(string setting, bool invert = false)
        {
            if (booleanSettings.TryGetValue(setting, out bool result))
            {   // If the setting was cached
                return result ^ invert;
            }

            // Otherwhise, load the value from disk and cache that setting
            result = File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, setting));
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
                if (value && !File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, setting)))
                {
                    File.WriteAllText(Path.Join(CoreData.UniGetUIDataDirectory, setting), "");
                }
                else if (!value)
                {
                    valueSettings[setting] = "";

                    if (File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, setting)))
                    {
                        File.Delete(Path.Join(CoreData.UniGetUIDataDirectory, setting));
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

            // Otherwhise, load the setting from disk and cache that setting
            value = "";
            if (File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, setting)))
            {
                value = File.ReadAllText(Path.Join(CoreData.UniGetUIDataDirectory, setting));
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
                    File.WriteAllText(Path.Join(CoreData.UniGetUIDataDirectory, setting), value);
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

        /*
         *
         *
         */

        public static bool AreNotificationsDisabled()
        {
            return Get("DisableSystemTray") || Get("DisableNotifications");
        }

        public static bool AreUpdatesNotificationsDisabled()
        {
            return AreNotificationsDisabled() || Get("DisableUpdatesNotifications");
        }

        public static bool AreErrorNotificationsDisabled()
        {
            return AreNotificationsDisabled() || Get("DisableErrorNotifications");
        }

        public static bool AreSuccessNotificationsDisabled()
        {
            return AreNotificationsDisabled() || Get("DisableSuccessNotifications");
        }

        public static bool AreProgressNotificationsDisabled()
        {
            return AreNotificationsDisabled() || Get("DisableProgressNotifications");
        }
    }
}
