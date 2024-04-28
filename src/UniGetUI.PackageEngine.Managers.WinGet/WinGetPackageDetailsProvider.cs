using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Managers.WingetManager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.WingetManager
{
    internal class WinGetPackageDetailsProvider : BasePackageDetailsProvider<PackageManager>
    {
        public WinGetPackageDetailsProvider(WinGet manager) : base(manager) { }
        
        protected override async Task<string[]> GetPackageVersions_Unsafe(Package package)
        {
            return await WinGetHelper.Instance.GetPackageVersions_Unsafe((WinGet)Manager, package);
        }

        protected override async Task<PackageDetails> GetPackageDetails_Unsafe(Package package)
        {
            return await WinGetHelper.Instance.GetPackageDetails_UnSafe((WinGet)Manager, package);
        }

        protected override Task<string> GetPackageIcon_Unsafe(Package package)
        {
            throw new NotImplementedException();
        }

        protected override Task<string[]> GetPackageScreenshots_Unsafe(Package package)
        {
            throw new NotImplementedException();
        }

    }
}
