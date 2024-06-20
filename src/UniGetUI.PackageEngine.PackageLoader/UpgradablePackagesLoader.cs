using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.PackageLoader
{
    public class UpgradablePackagesLoader : AbstractPackageLoader
    {
        public UpgradablePackagesLoader(IEnumerable<PackageManager> managers)
        : base(managers, AllowMultiplePackageVersions: false)
        {
            LOADER_IDENTIFIER = "DISCOVERABLE_PACKAGES";
        }
        protected override async Task<bool> IsPackageValid(Package package)
        {
            if (await package.HasUpdatesIgnoredAsync(package.NewVersion))
                return false;

            if (package.IsUpgradable && package.NewerVersionIsInstalled())
                return false;

            return true;
        }

        protected override Task<Package[]> LoadPackagesFromManager(PackageManager manager)
        {
            return manager.GetAvailableUpdates();
        }
#pragma warning disable 
        protected override async Task WhenAddingPackage(Package package)
        {
            package.GetAvailablePackage()?.SetTag(PackageTag.IsUpgradable);
            package.GetInstalledPackage()?.SetTag(PackageTag.IsUpgradable);
        }
#pragma warning restore
    }
}
