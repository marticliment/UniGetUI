using System.Collections.Concurrent;
using System.Text.Json;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;

namespace UniGetUI.Core.SettingsEngine
{
    public static partial class Settings
    {
        private static readonly ConcurrentDictionary<string, Dictionary<object, object?>> dictionarySettings = new();

        // Returns an empty dictionary if the setting doesn't exist and null if the types are invalid
        private static Dictionary<K, V>? _getDictionary<K, V>(string setting)
            where K : notnull
        {
            try
            {
                try
                {
                    if (dictionarySettings.TryGetValue(setting, out Dictionary<object, object>? result))
                    {
                        // If the setting was cached
                        return result.ToDictionary(
                            kvp => (K)kvp.Key,
                            kvp => (V)kvp.Value
                        );
                    }
                }
                catch (InvalidCastException)
                {
                    Logger.Error(
                        $"Tried to get a dictionary setting with a key of type {typeof(K)} and a value of type {typeof(V)}, which is not the type of the dictionary");
                    return null;
                }

                // Otherwhise, load the setting from disk and cache that setting
                Dictionary<K, V> value = new();
                if (File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, $"{setting}.json")))
                {
                    string result = File.ReadAllText(Path.Join(CoreData.UniGetUIDataDirectory, $"{setting}.json"));
                    try
                    {
                        if (result != "")
                        {
                            Dictionary<K, V>? item = JsonSerializer.Deserialize<Dictionary<K, V>>(result);
                            if (item is not null)
                            {
                                value = item;
                            }
                        }
                    }
                    catch (InvalidCastException)
                    {
                        Logger.Error(
                            $"Tried to get a dictionary setting with a key of type {typeof(K)} and a value of type {typeof(V)}, but the setting on disk ({result}) cannot be deserialized to that");
                    }
                }

                dictionarySettings[setting] = value.ToDictionary(
                    kvp => (object)kvp.Key,
                    kvp => (object?)kvp.Value
                );
                return value;
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not load dictionary name {setting}");
                Logger.Error(ex);
                return new();
            }
        }

        // Returns an empty dictionary if the setting doesn't exist and null if the types are invalid
        public static IReadOnlyDictionary<K, V>? GetDictionary<K, V>(string setting)
            where K : notnull
        {
            return _getDictionary<K, V>(setting);
        }

        public static void SetDictionary<K, V>(string setting, Dictionary<K, V> value)
            where K : notnull
        {
            dictionarySettings[setting] = value.ToDictionary(
                kvp => (object)kvp.Key,
                kvp => (object?)kvp.Value
            );

            var file = Path.Join(CoreData.UniGetUIDataDirectory, $"{setting}.json");
            try
            {
                if (value.Any()) File.WriteAllText(file, JsonSerializer.Serialize(value));
                else if (File.Exists(file)) File.Delete(file);
            }
            catch (Exception e)
            {
                Logger.Error($"CANNOT SET SETTING DICTIONARY FOR setting={setting} [{string.Join(", ", value)}]");
                Logger.Error(e);
            }
        }

        public static V? GetDictionaryItem<K, V>(string setting, K key)
            where K : notnull
        {
            Dictionary<K, V>? dictionary = _getDictionary<K, V>(setting);
            if (dictionary == null || !dictionary.TryGetValue(key, out V? value)) return default;

            return value;
        }

        // Also works as `Add`
        public static V? SetDictionaryItem<K, V>(string setting, K key, V value)
            where K : notnull
        {
            Dictionary<K, V>? dictionary = _getDictionary<K, V>(setting);
            if (dictionary == null)
            {
                dictionary = new()
                {
                    { key, value }
                };
                SetDictionary(setting, dictionary);
                return default;
            }

            if (dictionary.TryGetValue(key, out V? oldValue))
            {
                dictionary[key] = value;
                SetDictionary(setting, dictionary);
                return oldValue;
            }
            else
            {
                dictionary.Add(key, value);
                SetDictionary(setting, dictionary);
                return default;
            }
        }

        public static V? RemoveDictionaryKey<K, V>(string setting, K key)
            where K : notnull
        {
            Dictionary<K, V>? dictionary = _getDictionary<K, V>(setting);
            if (dictionary == null) return default;

            bool success = false;
            if (dictionary.TryGetValue(key, out V? value))
            {
                success = dictionary.Remove(key);
                SetDictionary(setting, dictionary);
            }

            if (!success) return default;
            return value;
        }

        public static bool DictionaryContainsKey<K, V>(string setting, K key)
            where K : notnull
        {
            Dictionary<K, V>? dictionary = _getDictionary<K, V>(setting);
            if (dictionary == null) return false;

            return dictionary.ContainsKey(key);
        }

        public static bool DictionaryContainsValue<K, V>(string setting, V value)
            where K : notnull
        {
            Dictionary<K, V>? dictionary = _getDictionary<K, V>(setting);
            if (dictionary == null) return false;

            return dictionary.ContainsValue(value);
        }

        public static void ClearDictionary(string setting)
        {
            SetDictionary(setting, new Dictionary<object, object>());
        }
    }
}
