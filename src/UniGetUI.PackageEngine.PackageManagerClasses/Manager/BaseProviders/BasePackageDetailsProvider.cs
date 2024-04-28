using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Classes.Manager.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Classes.Manager.BaseProviders
{
    public abstract class BasePackageDetailsProvider<T> : IPackageDetailsProvider where T : PackageManager
    {
        protected T Manager;

        public BasePackageDetailsProvider(T manager)
        {
            Manager = manager;
        }

        public async Task<PackageDetails> GetPackageDetails(Package package)
        {
            return await GetPackageDetails_Unsafe(package);
        }

        public async Task<string[]> GetPackageVersions(Package package)
        {
            if (Manager.Capabilities.SupportsCustomVersions)
                return await GetPackageVersions_Unsafe(package);
            else
            {
                Logger.Warn($"Manager {Manager.Name} does not support version retrieving, this method should have not been called");
                return [];
            }
        }

        public async Task<string> GetPackageIcon(Package package)
        {
            var path = await GetPackageIcon_Unsafe(package);
            if (!File.Exists(path))
            {
                if (path != "") Logger.Warn($"The icon returned by GetPackageIcon_Unsafe was non-empty but invalid for package {package.Id} on manager {Manager.Name}");
                path = Path.Join(CoreData.UniGetUIExecutableFile, "Assets", "Images", "package_color.png");
            }
            return path;
        }

        public async Task<string[]> GetPackageScreenshots(Package package)
        {
            return await GetPackageScreenshots_Unsafe(package);
        }
        protected abstract Task<PackageDetails> GetPackageDetails_Unsafe(Package package);
        protected abstract Task<string[]> GetPackageVersions_Unsafe(Package package);
        protected abstract Task<string> GetPackageIcon_Unsafe(Package package);
        protected abstract Task<string[]> GetPackageScreenshots_Unsafe(Package package);
    }
}
