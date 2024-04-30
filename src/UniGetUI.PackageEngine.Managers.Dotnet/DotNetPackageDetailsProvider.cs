using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Managers.Generic.NuGet;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.DotNetManager
{
    internal class DotNetPackageDetailsProvider : BasePackageDetailsProvider<PackageManager>
    {
        public DotNetPackageDetailsProvider(DotNet manager) : base(manager) { }

        protected override async Task<PackageDetails> GetPackageDetails_Unsafe(Package package)
        {
            return await NuGetPackageDetailsLoader.GetPackageDetails(package);
        }

        protected override async Task<Uri?> GetPackageIcon_Unsafe(Package package)
        {
            return await NuGetIconLoader.GetIconFromManifest(package);
        }

        protected override Task<Uri[]> GetPackageScreenshots_Unsafe(Package package)
        {
            throw new NotImplementedException();
        }

        protected override async Task<string[]> GetPackageVersions_Unsafe(Package package)
        {
            throw new Exception("DotNet does not support custom package versions");
        }
    }
}
