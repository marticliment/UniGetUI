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
        public IEnumerable<Package> GetAvailableUpdates_UnSafe(WinGet Manager);
        public IEnumerable<Package> GetInstalledPackages_UnSafe(WinGet Manager);
        public IEnumerable<Package> FindPackages_UnSafe(WinGet Manager, string query);
        public IEnumerable<IManagerSource> GetSources_UnSafe(WinGet Manager);
        public IEnumerable<string> GetInstallableVersions_Unsafe(WinGet Manager, IPackage package);
        public void GetPackageDetails_UnSafe(WinGet Manager, IPackageDetails details);
    }
}
