using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Interfaces.ManagerProviders;

namespace UniGetUI.PackageEngine.Classes.Manager.BaseProviders
{
    public abstract class BasePackageDetailsProvider<ManagerT> : IPackageDetailsProvider where ManagerT : IPackageManager
    {
        protected ManagerT Manager;

        public BasePackageDetailsProvider(ManagerT manager)
        {
            Manager = manager;
        }

        public void GetPackageDetails(IPackageDetails details)
        {
            GetDetails_UnSafe(details);
        }

        public IEnumerable<string> GetPackageVersions(IPackage package)
        {
            if (Manager.Capabilities.SupportsCustomVersions)
            {
                var result = GetInstallableVersions_UnSafe(package);
                Logger.Debug($"Found {result.Count()} versions for package Id={package.Id} on manager {Manager.Name}");
                return result;
            }

            Logger.Warn($"Manager {Manager.Name} does not support version retrieving, this method should have not been called");
            return [];
        }

        public CacheableIcon? GetPackageIconUrl(IPackage package)
        {
            if (Manager.Capabilities.SupportsCustomPackageIcons)
            {
                var nativeIcon = GetIcon_UnSafe(package);
                if (nativeIcon is not null)
                {
                    return nativeIcon;
                }
            }

            string? iconUrl = IconDatabase.Instance.GetIconUrlForId(package.GetIconId());
            if (iconUrl is not null)
            {
                return new CacheableIcon(new Uri(iconUrl), package.Version);
            }

            return null;
        }

        public IEnumerable<Uri> GetPackageScreenshotsUrl(IPackage package)
        {
            IEnumerable<Uri> URIs = [];

            if (Manager.Capabilities.SupportsCustomPackageScreenshots)
            {
                URIs = GetScreenshots_UnSafe(package);
            }
            else
            {
                Logger.Debug($"Manager {Manager.Name} does not support native screenshots");
            }

            if (!URIs.Any())
            {
                string[] UrlArray = IconDatabase.Instance.GetScreenshotsUrlForId(package.GetIconId());
                List<Uri> UriList = [];
                foreach (string url in UrlArray)
                {
                    if (url != "")
                    {
                        UriList.Add(new Uri(url));
                    }
                }

                URIs = UriList;
            }
            Logger.Info($"Found {URIs.Count()} screenshots for package Id={package.Id}");
            return URIs;
        }

        protected abstract void GetDetails_UnSafe(IPackageDetails details);
        protected abstract IEnumerable<string> GetInstallableVersions_UnSafe(IPackage package);
        protected abstract CacheableIcon? GetIcon_UnSafe(IPackage package);
        protected abstract IEnumerable<Uri> GetScreenshots_UnSafe(IPackage package);
        protected abstract string? GetInstallLocation_UnSafe(IPackage package);

        public string? GetPackageInstallLocation(IPackage package)
        {
            try
            {
                string? path = GetInstallLocation_UnSafe(package);
                if (path is not null && !Directory.Exists(path))
                {
                    Logger.Warn($"Path returned by the package manager \"{path}\" did not exist while loading package install location for package Id={package.Id} with Manager={package.Manager.Name}");
                    return null;
                }

                return path;
            }
            catch (Exception ex)
            {
                Logger.Error($"An error occurred while loading package install location for package Id={package.Id} with Manager={package.Manager.Name}");
                Logger.Error(ex);
                return null;
            }
        }


    }
}
