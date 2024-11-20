using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.WingetManager
{
    internal static class WinGetHelper
    {
        public static IWinGetManagerHelper Instance = null!;
    }

    internal interface IWinGetManagerHelper
    {
        public IEnumerable<Package> GetAvailableUpdates_UnSafe();
        public IEnumerable<Package> GetInstalledPackages_UnSafe();
        public IEnumerable<Package> FindPackages_UnSafe(string query);
        public IEnumerable<IManagerSource> GetSources_UnSafe();
        public IEnumerable<string> GetInstallableVersions_Unsafe(IPackage package);
        public void GetPackageDetails_UnSafe(IPackageDetails details);
    }
}
