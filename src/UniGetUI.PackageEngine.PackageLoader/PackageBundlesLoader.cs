using UniGetUI.Core.Logging;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Serializable;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.PackageLoader
{
    public class PackageBundlesLoader : AbstractPackageLoader
    {
        public PackageBundlesLoader(IEnumerable<PackageManager> managers)
        : base(managers, "PACKAGE_BUNDLES", AllowMultiplePackageVersions: true, DisableReload: true)
        {
        }

#pragma warning disable
        protected override async Task<bool> IsPackageValid(IPackage package)
        {
            return true;
        }
#pragma warning restore

        protected override Task<IPackage[]> LoadPackagesFromManager(IPackageManager manager)
        {
            return Task.Run(() => new IPackage[0]);
        }

#pragma warning disable CS1998
        protected override async Task WhenAddingPackage(IPackage package)
        {
            if(package.GetInstalledPackage() != null)
                package.SetTag(PackageTag.AlreadyInstalled);
        }
#pragma warning restore CS1998

        public void AddPackages(IEnumerable<IPackage> packages)
        {
            foreach (IPackage pkg in packages)
            {
                IPackage package;
                if (pkg is Package && pkg is not ImportedPackage && pkg.Source.IsVirtualManager)
                    package = new InvalidPackage(pkg.AsSerializable_Incompatible(), NullSource.Instance);
                else
                    package = pkg;

                if(!Contains(package)) AddPackage(package);
            }
            InvokePackagesChangedEvent();
        }

        public void RemoveRange(IEnumerable<IPackage> packages)
        {
            foreach(IPackage package in packages)
            {
                if (!Contains(package)) continue;
                //Packages.Remove(package);
                PackageReference.Remove(HashPackage(package));
            }
            InvokePackagesChangedEvent();
        }
    }
}
