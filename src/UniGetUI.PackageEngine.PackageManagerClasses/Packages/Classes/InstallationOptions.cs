using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.Json;
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
        private static readonly ConcurrentDictionary<long, InstallationOptions?> OptionsCache = [];

        public bool SkipHashCheck { get; set; }
        public bool InteractiveInstallation { get; set; }
        public bool RunAsAdministrator { get; set; }
        public string Version { get; set; } = "";
        public Architecture? Architecture { get; set; }
        public PackageScope? InstallationScope { get; set; }
        public List<string> CustomParameters { get; set; } = [];
        public bool RemoveDataOnUninstall { get; set; }
        public bool PreRelease { get; set; }
        public string CustomInstallLocation { get; set; } = "";

        public IPackage Package { get; }

        private readonly string __save_filename;

        private InstallationOptions(IPackage package)
        {
            Package = package;
            __save_filename = package.Manager.Name.Replace(" ", "").Replace(".", "") + "." + package.Id;
        }

        /// <summary>
        /// Returns the InstallationOptions object associated with the given package.
        /// </summary>
        /// <param name="package">The package from which to load the InstallationOptions</param>
        /// <returns>The package's InstallationOptions instance</returns>
        public static InstallationOptions FromPackage(IPackage package, bool? elevated = null, bool?
            interactive = null, bool? no_integrity = null, bool? remove_data = null)
        {
            InstallationOptions instance;
            if (OptionsCache.TryGetValue(package.GetHash(), out InstallationOptions? cached_instance) && cached_instance is not null)
            {
                instance = cached_instance;
            }
            else
            {
                instance = new(package);
                instance.LoadFromDisk();
                OptionsCache.TryAdd(package.GetHash(), instance);
            }

            if (elevated is not null)
            {
                instance.RunAsAdministrator = (bool)elevated;
            }

            if (interactive is not null)
            {
                instance.InteractiveInstallation = (bool)interactive;
            }

            if (no_integrity is not null)
            {
                instance.SkipHashCheck = (bool)no_integrity;
            }

            if (remove_data is not null)
            {
                instance.RemoveDataOnUninstall = (bool)remove_data;
            }

            return instance;
        }

        /// <summary>
        /// Returns the InstallationOptions object associated with the given package.
        /// </summary>
        /// <param name="package">The package from which to load the InstallationOptions</param>
        /// <returns>The package's InstallationOptions instance</returns>
        public static async Task<InstallationOptions> FromPackageAsync(IPackage package, bool? elevated = null,
            bool? interactive = null, bool? no_integrity = null, bool? remove_data = null)
        {
            return await Task.Run(() => FromPackage(package, elevated, interactive, no_integrity, remove_data));
        }

        /// <summary>
        /// Returns a new InstallationOptions object from a given SerializableInstallationOptions_v1 and a package.
        /// </summary>
        public static InstallationOptions FromSerialized(SerializableInstallationOptions_v1 options, IPackage package)
        {
            InstallationOptions instance = new(package);
            instance.FromSerializable(options);
            return instance;
        }

        /// <summary>
        /// Loads and applies the options from the given SerializableInstallationOptions_v1 object to the current object.
        /// </summary>
        public void FromSerializable(SerializableInstallationOptions_v1 options)
        {
            SkipHashCheck = options.SkipHashCheck;
            InteractiveInstallation = options.InteractiveInstallation;
            RunAsAdministrator = options.RunAsAdministrator;
            CustomInstallLocation = options.CustomInstallLocation;
            Version = options.Version;
            PreRelease = options.PreRelease;

            if (options.Architecture != "" && CommonTranslations.InvertedArchNames.TryGetValue(options.Architecture, out var name))
            {
                Architecture = name;
            }
            else
            {
                Architecture = null;
            }

            if (options.InstallationScope != "" && CommonTranslations.InvertedScopeNames_NonLang.TryGetValue(options.InstallationScope, out var value))
            {
                InstallationScope = value;
            }
            else
            {
                InstallationScope = null;
            }

            CustomParameters = options.CustomParameters;
        }

        /// <summary>
        /// Returns a SerializableInstallationOptions_v1 object containing the options of the current instance.
        /// </summary>
        public SerializableInstallationOptions_v1 AsSerializable()
        {
            SerializableInstallationOptions_v1 options = new()
            {
                SkipHashCheck = SkipHashCheck,
                InteractiveInstallation = InteractiveInstallation,
                RunAsAdministrator = RunAsAdministrator,
                CustomInstallLocation = CustomInstallLocation,
                PreRelease = PreRelease,
                Version = Version
            };
            if (Architecture is not null)
            {
                options.Architecture = CommonTranslations.ArchNames[Architecture.Value];
            }

            if (InstallationScope is not null)
            {
                options.InstallationScope = CommonTranslations.ScopeNames_NonLang[InstallationScope.Value];
            }

            options.CustomParameters = CustomParameters;
            return options;
        }

        private FileInfo GetPackageOptionsFile()
        {
            string optionsFileName = Package.Manager.Name + "." + Package.Id + ".json";
            return new FileInfo(Path.Join(CoreData.UniGetUIInstallationOptionsDirectory, optionsFileName));
        }

        /// <summary>
        /// Saves the current options to disk, asynchronously.
        /// </summary>
        public async Task SaveToDiskAsync()
        {
            await Task.Run(SaveToDisk);
        }

        /// <summary>
        /// Saves the current options to disk.
        /// </summary>
        public void SaveToDisk()
        {
            try
            {
                FileInfo optionsFile = GetPackageOptionsFile();
                if (optionsFile.Directory?.Exists == false)
                {
                    optionsFile.Directory.Create();
                }

                string fileContents = JsonSerializer.Serialize(
                    AsSerializable(),
                    CoreData.SerializingOptions
                );
                File.WriteAllText(optionsFile.FullName, fileContents);
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not save {Package.Id} options to disk");
                Logger.Error(ex);
            }
        }

        /// <summary>
        /// Loads the options from disk.
        /// </summary>
        public void LoadFromDisk()
        {
            FileInfo optionsFile = GetPackageOptionsFile();
            try
            {
                if (!optionsFile.Exists)
                {
                    return;
                }

                using FileStream inputStream = optionsFile.OpenRead();
                SerializableInstallationOptions_v1? options = JsonSerializer.Deserialize<SerializableInstallationOptions_v1>(
                    inputStream, CoreData.SerializingOptions);

                if (options is null)
                {
                    throw new InvalidOperationException("Deserialized options cannot be null!");
                }

                FromSerializable(options);
            }
            catch (JsonException)
            {
                Logger.Warn("An error occurred while parsing package " + optionsFile + ". The file will be overwritten");
                File.WriteAllText(optionsFile.FullName, "{}");
            }
            catch (Exception e)
            {
                Logger.Error("Loading installation options for file " + optionsFile + " have failed: ");
                Logger.Error(e);
            }
        }

        /// <summary>
        /// Returns a string representation of the current options.
        /// </summary>
        public override string ToString()
        {
            string customparams = CustomParameters is not null ? string.Join(",", CustomParameters) : "[]";
            return $"<InstallationOptions: SkipHashCheck={SkipHashCheck};" +
                   $"InteractiveInstallation={InteractiveInstallation};" +
                   $"RunAsAdministrator={RunAsAdministrator};" +
                   $"Version={Version};" +
                   $"Architecture={Architecture};" +
                   $"InstallationScope={InstallationScope};" +
                   $"InstallationScope={CustomInstallLocation};" +
                   $"CustomParameters={customparams};" +
                   $"RemoveDataOnUninstall={RemoveDataOnUninstall}>";
        }
    }
}
