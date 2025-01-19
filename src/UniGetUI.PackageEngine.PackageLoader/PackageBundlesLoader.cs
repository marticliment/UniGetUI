using System.Text.Json;
using System.Xml.Serialization;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Serializable;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;
using YamlDotNet.Serialization;

namespace UniGetUI.PackageEngine.PackageLoader
{
    public class PackageBundlesLoader : AbstractPackageLoader
    {
        public static PackageBundlesLoader Instance = null!;

        public PackageBundlesLoader(IEnumerable<IPackageManager> managers)
        : base(managers, "PACKAGE_BUNDLES", AllowMultiplePackageVersions: true, DisableReload: true, CheckedBydefault: false)
        {
            Instance = this;
        }

        protected override Task<bool> IsPackageValid(IPackage package)
        {
            return Task.FromResult(true);
        }

        protected override IEnumerable<IPackage> LoadPackagesFromManager(IPackageManager manager)
        {
            return [];
        }

        protected override Task WhenAddingPackage(IPackage package)
        {
            if(package.GetInstalledPackage() is not null)
                package.SetTag(PackageTag.AlreadyInstalled);

            return Task.CompletedTask;
        }

        public async Task AddPackagesAsync(IEnumerable<IPackage> foreign_packages)
        {
            foreach (IPackage foreign in foreign_packages)
            {
                IPackage? package = null;

                if (foreign is not ImportedPackage && foreign is Package native)
                {
                    if (native.Source.IsVirtualManager)
                    {
                        Logger.Debug($"Adding native package with id={native.Id} to bundle as an INVALID package...");
                        package = new InvalidImportedPackage(native.AsSerializable_Incompatible(), NullSource.Instance);
                    }
                    else
                    {
                        Logger.Debug($"Adding native package with id={native.Id} to bundle as a VALID package...");
                        package = new ImportedPackage(await Task.Run(native.AsSerializable), native.Manager, native.Source);
                    }
                }
                else if (foreign is ImportedPackage imported)
                {
                    Logger.Debug($"Adding loaded imported package with id={imported.Id} to bundle...");
                    package = imported;
                }
                else if (foreign is InvalidImportedPackage invalid)
                {
                    Logger.Debug($"Adding loaded incompatible package with id={invalid.Id} to bundle...");
                    package = invalid;
                }
                else
                {
                    Logger.Error($"An IPackage instance id={foreign.Id} did not match the types Package, ImportedPackage or InvalidImportedPackage. This should never be the case");
                }
                if(package is not null && !Contains(package)) AddPackage(package);
            }
            InvokePackagesChangedEvent();
        }

        public void RemoveRange(IEnumerable<IPackage> packages)
        {
            foreach(IPackage package in packages)
            {
                if (!Contains(package)) continue;
                PackageReference.Remove(HashPackage(package), out IPackage? _);
            }
            InvokePackagesChangedEvent();
        }

        public async Task<string> CreateBundle(IEnumerable<IPackage> unsorted_packages, BundleFormatType formatType = BundleFormatType.JSON)
        {
            SerializableBundle_v1 exportable = new();
            exportable.export_version = 2.1;

            List<IPackage> packages = unsorted_packages.ToList();
            packages.Sort(Comparison);

            int Comparison(IPackage x, IPackage y)
            {
                if(x.Id != y.Id) return String.Compare(x.Id, y.Id, StringComparison.Ordinal);
                if(x.Name != y.Name) return String.Compare(x.Name, y.Name, StringComparison.Ordinal);
                return (x.VersionAsFloat > y.VersionAsFloat) ? -1 : 1;
            }

            foreach (IPackage package in packages)
                if (package is Package && !package.Source.IsVirtualManager)
                    exportable.packages.Add(await Task.Run(package.AsSerializable));
                else
                    exportable.incompatible_packages.Add(package.AsSerializable_Incompatible());

            Logger.Debug("Finished loading serializable objects. Serializing with format " + formatType);
            string ExportableData;

            if (formatType == BundleFormatType.JSON)
                ExportableData = JsonSerializer.Serialize(
                    exportable,
                    CoreData.SerializingOptions);

            else if (formatType == BundleFormatType.YAML)
            {
                ISerializer serializer = new SerializerBuilder()
                    .Build();
                ExportableData = serializer.Serialize(exportable);
            }
            else
            {
                string tempfile = Path.GetTempFileName();
                StreamWriter writer = new(tempfile);
                XmlSerializer serializer = new(typeof(SerializableBundle_v1));
                serializer.Serialize(writer, exportable);
                writer.Close();
                ExportableData = await File.ReadAllTextAsync(tempfile);
                File.Delete(tempfile);
            }

            Logger.Debug("Serialization finished successfully");

            return ExportableData;
        }

        public async Task<double> AddFromBundle(string RawBundleContent, BundleFormatType format)
        {
            // Deserialize data
            SerializableBundle_v1? DeserializedData;
            if (format is BundleFormatType.JSON)
            {
                DeserializedData = await Task.Run(() => JsonSerializer.Deserialize<SerializableBundle_v1>(RawBundleContent, CoreData.SerializingOptions));
            }
            else if (format is BundleFormatType.YAML)
            {
                IDeserializer deserializer =
                    new DeserializerBuilder()
                        .IgnoreUnmatchedProperties()
                        .Build();
                DeserializedData = await Task.Run(() => deserializer.Deserialize<SerializableBundle_v1>(RawBundleContent));
            }
            else
            {
                string tempfile = Path.GetTempFileName();
                await File.WriteAllTextAsync(tempfile, RawBundleContent);
                StreamReader reader = new(tempfile);
                XmlSerializer serializer = new(typeof(SerializableBundle_v1));
                DeserializedData = await Task.Run(() => serializer.Deserialize(reader) as SerializableBundle_v1);
                reader.Close();
                File.Delete(tempfile);
            }

            if (DeserializedData is null || DeserializedData.export_version is -1)
            {
                throw new ArgumentException("DeserializedData was null");
            }

            List<IPackage> packages = new List<IPackage>();

            foreach (SerializablePackage_v1 DeserializedPackage in DeserializedData.packages)
            {
                packages.Add(PackageFromSerializable(DeserializedPackage));
            }

            foreach (SerializableIncompatiblePackage_v1 DeserializedPackage in DeserializedData
                         .incompatible_packages)
            {
                packages.Add(InvalidPackageFromSerializable(DeserializedPackage, NullSource.Instance));
            }

            await AddPackagesAsync(packages);

            return DeserializedData.export_version;
        }

        private IPackage PackageFromSerializable(SerializablePackage_v1 raw_package)
        {
            IPackageManager? manager = null;
            IManagerSource? source;

            foreach (var possible_manager in Managers)
            {
                if (possible_manager.Name == raw_package.ManagerName)
                {
                    manager = possible_manager;
                    break;
                }
            }

            if (manager?.Capabilities.SupportsCustomSources == true)
            {
                source = manager?.SourcesHelper?.Factory.GetSourceIfExists(raw_package.Source);
            }
            else
                source = manager?.DefaultSource;

            if (manager is null || source is null)
            {
                return InvalidPackageFromSerializable(raw_package.GetInvalidEquivalent(), NullSource.Instance);
            }

            return new ImportedPackage(raw_package, manager, source);
        }

        private static IPackage InvalidPackageFromSerializable(SerializableIncompatiblePackage_v1 raw_package, IManagerSource source)
        {
            return new InvalidImportedPackage(raw_package, source);
        }
    }
}
