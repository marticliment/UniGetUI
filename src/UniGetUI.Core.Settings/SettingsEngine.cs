using System.Collections.Concurrent;
using System.Text.Json;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;

namespace UniGetUI.Core.SettingsEngine
{
    public static class Settings
    {
        private static ConcurrentDictionary<string, bool> booleanSettings = new();
        private static ConcurrentDictionary<string, string> valueSettings = new();
        private static ConcurrentDictionary<string, List<object>> listSettings = new();

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

        // Complex Settings

        // Returns an empty list if the setting doesn't exist and null if the type is invalid
        private static List<T>? GetListInternal<T>(string setting)
        {
            try
            {
                if (listSettings.TryGetValue(setting, out List<object>? result))
                {
                    // If the setting was cached
                    return result.Cast<T>().ToList();
                }
            }
            catch (InvalidCastException)
            {
                Logger.Error($"Tried to get a list setting as type {typeof(T)}, which is not the type of the list");
                return null;
            }

            // Otherwhise, load the setting from disk and cache that setting
            List<T> value = [];
            if (File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, setting)))
            {
                foreach (var result in File.ReadAllText(Path.Join(CoreData.UniGetUIDataDirectory, setting)).Split("\n"))
                {
                    try
                    {
                        if (result != "")
                        {
                            T? item = JsonSerializer.Deserialize<T>(result);
                            if (item is not null)
                            {
                                value.Add(item);
                            }
                        }
                    }
                    catch (InvalidCastException)
                    {
                        Logger.Error($"Tried to get a list setting as type {typeof(T)}, but the setting on disk {result} cannot be deserialized to {typeof(T)}");
                    }
                }
            }

            listSettings[setting] = value.Cast<object>().ToList();
            return value;
        }

        // Returns an empty list if the setting doesn't exist and null if the type is invalid
        public static IReadOnlyList<T>? GetList<T>(string setting)
        {
            return GetListInternal<T>(setting);
        }

        public static void SetList<T>(string setting, List<T> value)
        {
            listSettings[setting] = value.Cast<object>().ToList();

            try
            {
                if (value.Count > 0)
                {
                    File.WriteAllText(Path.Join(CoreData.UniGetUIDataDirectory, setting), "");
                    foreach (T item in value)
                    {
                        File.AppendAllText(Path.Join(CoreData.UniGetUIDataDirectory, setting), JsonSerializer.Serialize(item) + "\n");
                    }
                }
                else
                {
                    if (File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, setting)))
                    {
                        File.Delete(Path.Join(CoreData.UniGetUIDataDirectory, setting));
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"CANNOT SET SETTING LIST FOR setting={setting} [{string.Join(", ", value)}]");
                Logger.Error(e);
            }
        }

        public static T? GetListItem<T>(string setting, int index)
        {
            List<T>? list = GetListInternal<T>(setting);
            if (list == null) return default;
            if (list.Count >= index)
            {
                Logger.Error($"Index {index} out of range for list setting {setting}");
                return default;
            }

            return list.ElementAt(index);
        }

        public static void AddToList<T>(string setting, T value)
        {
            List<T>? list = GetListInternal<T>(setting);
            if (list == null) return;

            list.Add(value);
            SetList(setting, list);
        }

        public static bool RemoveFromList<T>(string setting, T value)
        {
            List<T>? list = GetListInternal<T>(setting);
            if (list == null) return false;

            bool result = list.Remove(value);
            SetList(setting, list);
            return result;
        }

        public static bool ListContains<T>(string setting, T value)
        {
            List<T>? list = GetListInternal<T>(setting);
            if (list == null) return false;
            return list.Contains(value);
        }

        public static void ClearList(string setting)
        {
            SetList<object>(setting, []);
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
