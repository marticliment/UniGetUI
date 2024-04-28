using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUIManagers = UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Managers.WingetManager;
using UniGetUI.PackageEngine.PackageClasses;
using Microsoft.Management.Deployment;

namespace UniGetUI.PackageEngine.Managers.WingetManager
{
    internal class WinGetPackageDetailsProvider : BasePackageDetailsProvider<UniGetUIManagers.PackageManager>
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

        protected override async Task<Uri?> GetPackageIcon_Unsafe(Package package)
        {
            return null;
            // TODO: Need to work on retrieving WinGet icons

            if(WinGetHelper.Instance is not NativeWinGetHelper)
            {
                Logger.Warn("WinGet will not attempt to load icon since the helper is using bundled WinGet");
                return null;
            }

            var WinGetManager = ((NativeWinGetHelper)WinGetHelper.Instance).WinGetManager;
            var Factory = ((NativeWinGetHelper)WinGetHelper.Instance).Factory;

            // Find the native package for the given Package object
            PackageCatalogReference Catalog = WinGetManager.GetPackageCatalogByName(package.Source.Name);
            if (Catalog == null)
            {
                Logger.Error("[WINGET COM] Failed to get catalog " + package.Source.Name + ". Is the package local?");
                return null;
            }

            // Connect to catalog
            Catalog.AcceptSourceAgreements = true;
            //ConnectResult ConnectResult = await Task.Run(() => Catalog.Connect());
            ConnectResult ConnectResult = await Catalog.ConnectAsync();
            if (ConnectResult.Status != ConnectResultStatus.Ok)
            {
                Logger.Error("[WINGET COM] Failed to connect to catalog " + package.Source.Name);
                return null;
            }

            // Match only the exact same Id
            FindPackagesOptions packageMatchFilter = Factory.CreateFindPackagesOptions();
            PackageMatchFilter filters = Factory.CreatePackageMatchFilter();
            filters.Field = PackageMatchField.Id;
            filters.Value = package.Id;
            filters.Option = PackageFieldMatchOption.Equals;
            packageMatchFilter.Filters.Add(filters);
            packageMatchFilter.ResultLimit = 1;
            Task<FindPackagesResult> SearchResult = Task.Run(() => ConnectResult.PackageCatalog.FindPackages(packageMatchFilter));

            if (SearchResult.Result == null || SearchResult.Result.Matches == null || SearchResult.Result.Matches.Count() == 0)
            {
                Logger.Error("[WINGET COM] Failed to find package " + package.Id + " in catalog " + package.Source.Name);
                return null;
            }

            // Get the Native Package
            CatalogPackage NativePackage = SearchResult.Result.Matches.First().CatalogPackage;

            // Extract data from NativeDetails
            CatalogPackageMetadata NativeDetails = NativePackage.DefaultInstallVersion.GetCatalogPackageMetadata();

            foreach(var icon in NativeDetails.Icons.ToArray())
            {
                Logger.Info(icon.Url);
                Logger.Info(Convert.ToHexString(icon.Sha256));
            }

            return null;
        }

        protected override Task<Uri[]> GetPackageScreenshots_Unsafe(Package package)
        {
            throw new NotImplementedException();
        }

    }
}
