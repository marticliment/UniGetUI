using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using ABI.Windows.UI.Text.Core;
using UniGetUI.Core.Data;
using UniGetUI.Core.Language;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Serializable;

namespace UniGetUI.PackageEngine.PackageClasses
{

    /// <summary>
    /// This class represents the options in which a package must be installed, updated or uninstalled.
    /// </summary>
    public static class InstallOptionsFactory
    {
        private static class StoragePath
        {
            public static string Get(IPackageManager manager)
                => "GlobalValues." + manager.Name.Replace(" ", "").Replace(".", "");

            public static string Get(IPackage package)
                => package.Manager.Name.Replace(" ", "").Replace(".", "") + "." + package.Id;
        }


        // Loading from disk (package and manager)
        public static SerializableInstallationOptions LoadForPackage(IPackage package)
            => _loadFromDisk(StoragePath.Get(package));

        public static Task<SerializableInstallationOptions> LoadForPackageAsync(IPackage package)
            => Task.Run(() => LoadForPackage(package));

        public static SerializableInstallationOptions LoadForManager(IPackageManager manager)
            => _loadFromDisk(StoragePath.Get(manager));

        public static Task<SerializableInstallationOptions> LoadForManagerAsync(IPackageManager manager)
            => Task.Run(() => _loadFromDisk(StoragePath.Get(manager)));

        // Saving to disk (package and manager)
        public static void SaveForPackage(SerializableInstallationOptions options, IPackage package)
            => _saveToDisk(options, StoragePath.Get(package));

        public static Task SaveForPackageAsync(SerializableInstallationOptions options, IPackage package)
            => Task.Run(() => _saveToDisk(options, StoragePath.Get(package)));

        public static void SaveForManager(SerializableInstallationOptions options, IPackageManager manager)
            => _saveToDisk(options, StoragePath.Get(manager));

        public static Task SaveForManagerAsync(SerializableInstallationOptions options, IPackageManager manager)
            => Task.Run(() => _saveToDisk(options, StoragePath.Get(manager)));

        // Loading applicable
        public static SerializableInstallationOptions LoadApplicable(
            IPackage package,
            bool? elevated = null,
            bool? interactive = null,
            bool? no_integrity = null,
            bool? remove_data = null)
        {
            var instance = LoadForPackage(package);
            if (!instance.OverridesNextLevelOpts)
            {
                Logger.Debug($"Package {package.Id} does not override options, will use package manager's default...");
                instance = LoadForManager(package.Manager);
            }

            if (elevated is not null) instance.RunAsAdministrator = (bool)elevated;
            if (interactive is not null) instance.InteractiveInstallation = (bool)interactive;
            if (no_integrity is not null) instance.SkipHashCheck = (bool)no_integrity;
            if (remove_data is not null) instance.RemoveDataOnUninstall = (bool)remove_data;

            return instance;
        }

        public static Task<SerializableInstallationOptions> LoadApplicableAsync(
            IPackage package,
            bool? elevated = null,
            bool? interactive = null,
            bool? no_integrity = null,
            bool? remove_data = null)
            => Task.Run(() => LoadApplicable(package, elevated, interactive, no_integrity, remove_data));

        /*
         *
         * SAVE TO DISK MECHANISMS
         *
         */

        private static readonly ConcurrentDictionary<string, SerializableInstallationOptions> _optionsCache = new();

        private static void _saveToDisk(SerializableInstallationOptions options, string key)
        {
            try
            {
                var filePath = Path.Join(CoreData.UniGetUIInstallationOptionsDirectory, key);
                _optionsCache[key] = options.Copy();

                string fileContents = JsonSerializer.Serialize(
                    options,
                    SerializationHelpers.DefaultOptions
                );
                File.WriteAllText(filePath, fileContents);
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not save {key} options to disk");
                Logger.Error(ex);
            }
        }

        private static SerializableInstallationOptions _loadFromDisk(string key)
        {
            try
            {
                SerializableInstallationOptions serializedOptions;
                if (_optionsCache.TryGetValue(key, out var cached))
                {
                    // If the wanted instance is already cached
                    return cached.Copy();
                }
                else
                {
                    var filePath = Path.Join(CoreData.UniGetUIInstallationOptionsDirectory, key);
                    if (!File.Exists(filePath))
                    {
                        // If the file where it should be stored does not exist
                        _optionsCache[key] = new SerializableInstallationOptions();
                        return new SerializableInstallationOptions();
                    }
                    else
                    {
                        // If the options are not cached, and the save file exists
                        var rawData = File.ReadAllText(filePath);
                        JsonNode? jsonData = JsonNode.Parse(rawData);
                        ArgumentNullException.ThrowIfNull(jsonData);
                        serializedOptions = new SerializableInstallationOptions(jsonData);
                        _optionsCache[key] = serializedOptions;
                        return serializedOptions;
                    }
                }
            }
            catch (JsonException)
            {
                Logger.Warn("An error occurred while parsing package " + key + ". The file will be overwritten");
                File.WriteAllText(key, "{}");
                return new();
            }
            catch (Exception e)
            {
                Logger.Error("Loading installation options for file " + key + " have failed: ");
                Logger.Error(e);
                return new();
            }
        }
    }
}
