using UniGetUI.PackageEngine.Classes.Serializable;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Serializable;

namespace UniGetUI.PackageEngine.PackageClasses
{
    public class ImportedPackage : Package
    {
        /// <summary>
        /// Construct an invalid package with a given name, id, version, source and manager, and an optional scope.
        /// </summary>
        public SerializableUpdatesOptions_v1 updates_options;
        public SerializableInstallationOptions_v1 installation_options;

        public ImportedPackage(SerializablePackage_v1 raw_data, IPackageManager manager, IManagerSource source)
            : base(raw_data.Name, raw_data.Id, raw_data.Version, source, manager)
        {
            installation_options = raw_data.InstallationOptions;
            updates_options = raw_data.Updates;
        }

        public async Task<Package> RegisterAndGetPackageAsync()
        {
            var options = await InstallationOptions.FromPackageAsync(this);
            options.FromSerializable(installation_options);
            await options.SaveToDiskAsync();

            if (updates_options.UpdatesIgnored)
            {
                await AddToIgnoredUpdatesAsync(updates_options.IgnoredVersion);
            }

            return new Package(Name, Id, Version, Source, Manager);
        }
    }
}
