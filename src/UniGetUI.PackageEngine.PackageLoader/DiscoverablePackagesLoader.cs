using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.PackageLoader
{
    public class DiscoverablePackagesLoader : AbstractPackageLoader
    {
        private string QUERY_TEXT = string.Empty;

        public DiscoverablePackagesLoader(IEnumerable<IPackageManager> managers)
        : base(managers, "DISCOVERABLE_PACKAGES", AllowMultiplePackageVersions: false)
        { }

        public async Task ReloadPackages(string query)
        {
            QUERY_TEXT = query;
            await ReloadPackages();
        }

        public override async Task ReloadPackages()
        {
            if (QUERY_TEXT == "")
            {
                return;
            }

            await base.ReloadPackages();
        }

        protected override async Task<bool> IsPackageValid(IPackage package)
        {
            return await Task.FromResult(true);
        }

        protected override IEnumerable<IPackage> LoadPackagesFromManager(IPackageManager manager)
        {
            string text = QUERY_TEXT;
            text = CoreTools.EnsureSafeQueryString(text);
            if (text == string.Empty)
            {
                return [];
            }

            return  manager.FindPackages(text);
        }

        protected override Task WhenAddingPackage(IPackage package)
        {
            if (package.GetUpgradablePackage() is not null)
            {
                package.SetTag(PackageTag.IsUpgradable);
            }
            else if (package.GetInstalledPackage() is not null)
            {
                package.SetTag(PackageTag.AlreadyInstalled);
            }
            return Task.CompletedTask;
        }
    }
}
