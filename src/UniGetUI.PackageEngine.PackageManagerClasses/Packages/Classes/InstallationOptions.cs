using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    public class InstallationOptions : IInstallationOptions
    {
        // private static readonly ConcurrentDictionary<long, InstallationOptions?> OptionsCache = [];

        public bool SkipHashCheck { get; set; }
        public bool InteractiveInstallation { get; set; }
        public bool RunAsAdministrator { get; set; }
        public string Version { get; set; } = "";
        public bool SkipMinorUpdates { get; set; }
        public Architecture? Architecture { get; set; }
        public PackageScope? InstallationScope { get; set; }
        public List<string> CustomParameters { get; set; } = [];
        public bool RemoveDataOnUninstall { get; set; }
        public bool PreRelease { get; set; }
        public string CustomInstallLocation { get; set; } = "";
        public bool OverridesNextLevelOpts { get; set; }

        private readonly string __save_filename;

        private InstallationOptions(string filename)
        {
            __save_filename = Path.Join(CoreData.UniGetUIInstallationOptionsDirectory, filename);
        }

        private static class StoragePath
        {
            public static string Get(IPackageManager manager)
                => "GlobalValues." + manager.Name.Replace(" ", "").Replace(".", "");

            public static string Get(IPackage package)
                => package.Manager.Name.Replace(" ", "").Replace(".", "") + "." + package.Id;
        }

        public static InstallationOptions CreateEmpty(IPackage package)
        {
            var pkg_path = StoragePath.Get(package);
            return new(pkg_path);
        }

        public static InstallationOptions CreateEmpty(IPackageManager manager)
        {
            var mgr_path = StoragePath.Get(manager);
            return new(mgr_path);
        }

        public static InstallationOptions FromSerialized(SerializableInstallationOptions options, IPackage package)
        {
            var instance = CreateEmpty(package);
            instance.GetValuesFromSerializable(options);
            return instance;
        }

        public static InstallationOptions FromSerialized(SerializableInstallationOptions options, IPackageManager manager)
        {
            var instance = CreateEmpty(manager);
            instance.GetValuesFromSerializable(options);
            return instance;
        }

        public static InstallationOptions LoadForPackage(IPackage package)
        {
            InstallationOptions instance = CreateEmpty(package);
            instance.LoadFromDisk();
            return instance;
        }

        public static async Task<InstallationOptions> LoadForPackageAsync(IPackage package)
        {
            InstallationOptions instance = CreateEmpty(package);
            await Task.Run(instance.LoadFromDisk);
            return instance;
        }

        public static InstallationOptions LoadForManager(IPackageManager manager)
        {
            InstallationOptions instance = CreateEmpty(manager);
            instance.LoadFromDisk();
            return instance;
        }

        public static InstallationOptions LoadApplicable(
            IPackage package,
            bool? elevated = null,
            bool? interactive = null,
            bool? no_integrity = null,
            bool? remove_data = null)
        {
            InstallationOptions instance = LoadForPackage(package);
            if (!instance.OverridesNextLevelOpts)
            {
                instance = LoadForManager(package.Manager);
                Logger.Debug($"Package {package.Id} does not override options, will use package manager's default...");
            }

            if (elevated is not null) instance.RunAsAdministrator = (bool)elevated;
            if (interactive is not null) instance.InteractiveInstallation = (bool)interactive;
            if (no_integrity is not null) instance.SkipHashCheck = (bool)no_integrity;
            if (remove_data is not null) instance.RemoveDataOnUninstall = (bool)remove_data;

            return instance;
        }

        public static Task<InstallationOptions> LoadApplicableAsync(
            IPackage package,
            bool? elevated = null,
            bool? interactive = null,
            bool? no_integrity = null,
            bool? remove_data = null)
            => Task.Run(() => LoadApplicable(package, elevated, interactive, no_integrity, remove_data));

        /// <summary>
        /// Loads and applies the options from the given SerializableInstallationOptions object to the current object.
        /// </summary>
        public void GetValuesFromSerializable(SerializableInstallationOptions options)
        {
            SkipHashCheck = options.SkipHashCheck;
            InteractiveInstallation = options.InteractiveInstallation;
            RunAsAdministrator = options.RunAsAdministrator;
            CustomInstallLocation = options.CustomInstallLocation;
            Version = options.Version;
            SkipMinorUpdates = options.SkipMinorUpdates;
            PreRelease = options.PreRelease;
            OverridesNextLevelOpts = options.OverridesNextLevelOpts;

            Architecture = null;
            if (options.Architecture.Any() &&
                CommonTranslations.InvertedArchNames.TryGetValue(options.Architecture, out var name))
            {
                Architecture = name;
            }

            InstallationScope = null;
            if (options.InstallationScope.Any() &&
                CommonTranslations.InvertedScopeNames_NonLang.TryGetValue(options.InstallationScope, out var value))
            {
                InstallationScope = value;
            }

            CustomParameters = options.CustomParameters;
        }

        /// <summary>
        /// Returns a SerializableInstallationOptions object containing the options of the current instance.
        /// </summary>
        public SerializableInstallationOptions ToSerializable()
        {
            SerializableInstallationOptions options = new()
            {
                SkipHashCheck = SkipHashCheck,
                InteractiveInstallation = InteractiveInstallation,
                RunAsAdministrator = RunAsAdministrator,
                CustomInstallLocation = CustomInstallLocation,
                PreRelease = PreRelease,
                Version = Version,
                SkipMinorUpdates = SkipMinorUpdates,
                OverridesNextLevelOpts = OverridesNextLevelOpts
            };

            if (Architecture is not null)
                options.Architecture = CommonTranslations.ArchNames[Architecture.Value];

            if (InstallationScope is not null)
                options.InstallationScope = CommonTranslations.ScopeNames_NonLang[InstallationScope.Value];

            options.CustomParameters = CustomParameters;
            return options;
        }

        /// <summary>
        /// Saves the current options to disk.
        /// </summary>
        public void SaveToDisk()
        {
            try
            {
                var dir = Path.GetDirectoryName(__save_filename);
                ArgumentException.ThrowIfNullOrEmpty(dir);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string fileContents = JsonSerializer.Serialize(
                    ToSerializable(),
                    SerializationHelpers.DefaultOptions
                );
                File.WriteAllText(__save_filename, fileContents);
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not save {__save_filename} options to disk");
                Logger.Error(ex);
            }
        }

        /// <summary>
        /// Saves the current options to disk.
        /// </summary>
        public Task SaveToDiskAsync() => Task.Run(SaveToDisk);

        /// <summary>
        /// Loads the options from disk.
        /// </summary>
        private void LoadFromDisk()
        {
            try
            {
                if (!File.Exists(__save_filename))
                    return;

                var rawData = File.ReadAllText(__save_filename);
                JsonNode? jsonData = JsonNode.Parse(rawData);
                ArgumentNullException.ThrowIfNull(jsonData);
                var serializedOptions = new SerializableInstallationOptions(jsonData);
                GetValuesFromSerializable(serializedOptions);
            }
            catch (JsonException)
            {
                Logger.Warn("An error occurred while parsing package " + __save_filename + ". The file will be overwritten");
                File.WriteAllText(__save_filename, "{}");
            }
            catch (Exception e)
            {
                Logger.Error("Loading installation options for file " + __save_filename + " have failed: ");
                Logger.Error(e);
            }
        }

        /// <summary>
        /// Returns a string representation of the current options.
        /// </summary>
        public override string ToString()
        {
            string customparams = CustomParameters.Any() ? string.Join(",", CustomParameters) : "[]";
            return $"<InstallationOptions: SkipHashCheck={SkipHashCheck};" +
                   $"InteractiveInstallation={InteractiveInstallation};" +
                   $"RunAsAdministrator={RunAsAdministrator};" +
                   $"Version={Version};" +
                   $"Architecture={Architecture};" +
                   $"InstallationScope={InstallationScope};" +
                   $"InstallationScope={CustomInstallLocation};" +
                   $"CustomParameters={customparams};" +
                   $"RemoveDataOnUninstall={RemoveDataOnUninstall};" +
                   $"PreRelease={PreRelease}>";
        }
    }
}
