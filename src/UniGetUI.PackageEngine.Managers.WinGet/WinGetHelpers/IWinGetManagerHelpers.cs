using Microsoft.Management.Deployment;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.PackageClasses;
using WindowsPackageManager.Interop;

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
        public Task<ManagerSource[]> GetSources_UnSafe(WinGet Manager);
        public Task<string[]> GetPackageVersions_Unsafe(WinGet Manager, Package package);
        public Task GetPackageDetails_UnSafe(WinGet Manager, PackageDetails details);
    }
}


