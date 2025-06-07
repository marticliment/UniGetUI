using UniGetUI.Core.Logging;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.PackageLoader
{
    public class PackageBundlesLoader : AbstractPackageLoader
    {
        public static PackageBundlesLoader Instance = null!;

        public PackageBundlesLoader(IReadOnlyList<IPackageManager> managers)
        : base(managers,
            identifier: "PACKAGE_BUNDLES",
            AllowMultiplePackageVersions: true,
            DisableReload: true,
            CheckedBydefault: false,
            RequiresInternet: false)
        {
            Instance = this;
        }

        protected override Task<bool> IsPackageValid(IPackage package)
        {
            return Task.FromResult(true);
        }

        protected override IReadOnlyList<IPackage> LoadPackagesFromManager(IPackageManager manager)
        {
            return [];
        }

        protected override Task WhenAddingPackage(IPackage package)
        {
            if (package.GetInstalledPackage() is not null)
                package.SetTag(PackageTag.AlreadyInstalled);

            return Task.CompletedTask;
        }

        public async Task AddPackagesAsync(IReadOnlyList<IPackage> foreign_packages)
        {
            List<IPackage> added = new();
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
                        package = new ImportedPackage(await native.AsSerializableAsync(), native.Manager, native.Source);
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

                if (package is not null && !Contains(package))
                {
                    AddPackage(package);
                    added.Add(package);
                }
            }
            InvokePackagesChangedEvent(true, added, []);
        }

        public void RemoveRange(IReadOnlyList<IPackage> packages)
        {
            foreach(IPackage package in packages)
            {
                if (!Contains(package)) continue;
                PackageReference.Remove(HashPackage(package), out IPackage? _);
            }
            InvokePackagesChangedEvent(true, [], packages);
        }
    }
}
