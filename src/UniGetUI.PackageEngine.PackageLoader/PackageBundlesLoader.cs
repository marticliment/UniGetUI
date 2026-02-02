using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.PackageLoader
{
    public abstract class PackageBundlesLoader : AbstractPackageLoader
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
            if (package.NewerVersionIsInstalled())
            {
                package.SetTag(PackageTag.AlreadyInstalled);
            }
            else if (package.GetUpgradablePackage() is not null)
            {
                package.SetTag(PackageTag.IsUpgradable);
            }

            return Task.CompletedTask;
        }

        /*
         * This method required access to the Package, ImportedPackage and InvalidPackage classes,
         * but they are not defined here yet. This class will be inherited on PEInterface, with the missing member
         */
        public abstract Task AddPackagesAsync(IReadOnlyList<IPackage> foreign_packages);

        public void RemoveRange(IReadOnlyList<IPackage> packages)
        {
            foreach (IPackage package in packages)
            {
                if (!Contains(package)) continue;
                PackageReference.Remove(HashPackage(package), out IPackage? _);
            }
            InvokePackagesChangedEvent(true, [], packages);
        }
    }
}
