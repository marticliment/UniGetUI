using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.PackageLoader
{
    public class DiscoverablePackagesLoader : AbstractPackageLoader
    {
        public static DiscoverablePackagesLoader Instance = null!;

        private string QUERY_TEXT = string.Empty;

        public DiscoverablePackagesLoader(IReadOnlyList<IPackageManager> managers)
            : base(managers,
                identifier: "DISCOVERABLE_PACKAGES",
                AllowMultiplePackageVersions: false,
                DisableReload: false,
                CheckedBydefault: false,
                RequiresInternet: true)
        {
            Instance = this;
        }

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

        protected override IReadOnlyList<IPackage> LoadPackagesFromManager(IPackageManager manager)
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

        public (IPackage?, string?) GetPackageFromIdAndManager(string id, string managerName, string sourceName)
        {
            IPackageManager? manager = null;

            foreach (var candidate in Managers)
            {
                if (candidate.Name == managerName || candidate.DisplayName == managerName)
                {
                    manager = candidate;
                    break;
                }
            }

            if (manager is null)
                return (null, CoreTools.Translate("The package manager \"{0}\" was not found", managerName));

            if (!manager.IsEnabled())
                return (null, CoreTools.Translate("The package manager \"{0}\" is disabled", manager.DisplayName));

            if (!manager.Status.Found)
                return (null, CoreTools.Translate("There is an error with the configuration of the package manager \"{0}\"", manager.DisplayName));

            var results = manager.FindPackages(id);
            var candidates = results.Where(p => p.Id == id).ToArray();

            if (candidates.Length == 0)
                return (null, CoreTools.Translate("The package \"{0}\" was not found on the package manager \"{1}\"", id, manager.DisplayName));

            IPackage package = candidates[0];

            // Get package from best source
            if (candidates.Length >= 1 && manager.Capabilities.SupportsCustomSources)
                foreach (var candidate in candidates)
                {
                    if (candidate.Source.Name == sourceName)
                        package = candidate;
                }

            return (package, null);
        }
    }
}
