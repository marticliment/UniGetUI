using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.WingetManager
{
    internal static class WinGetHelper
    {
        private static IWinGetManagerHelper? __helper;
        public static IWinGetManagerHelper Instance
        {
            get
            {
                if (__helper == null)
                {
                    __helper = new BundledWinGetHelper();
                }
                return __helper;
            }

            set
            {
                __helper = value;
            }
        }
    }

    internal interface IWinGetManagerHelper
    {
        public Task<Package[]> GetAvailableUpdates_UnSafe(WinGet Manager);
        public Task<Package[]> GetInstalledPackages_UnSafe(WinGet Manager);
        public Task<Package[]> FindPackages_UnSafe(WinGet Manager, string query);
        public Task<IManagerSource[]> GetSources_UnSafe(WinGet Manager);
        public Task<string[]> GetPackageVersions_Unsafe(WinGet Manager, IPackage package);
        public Task GetPackageDetails_UnSafe(WinGet Manager, IPackageDetails details);
    }
}
