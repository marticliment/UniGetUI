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
        public IReadOnlyList<Package> GetAvailableUpdates_UnSafe();
        public IReadOnlyList<Package> GetInstalledPackages_UnSafe();
        public IReadOnlyList<Package> FindPackages_UnSafe(string query);
        public IReadOnlyList<IManagerSource> GetSources_UnSafe();
        public IReadOnlyList<string> GetInstallableVersions_Unsafe(IPackage package);
        public void GetPackageDetails_UnSafe(IPackageDetails details);
    }
}
