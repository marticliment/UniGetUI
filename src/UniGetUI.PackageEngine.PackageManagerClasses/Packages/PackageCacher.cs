using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Classes.Packages
{
    internal static class PackageCacher
    {
        private static Dictionary<PackageManager, Dictionary<string, Package>> __available_packages = new();
        private static Dictionary<PackageManager, Dictionary<string, Package>> __upgradable_packages = new();
        private static Dictionary<PackageManager, Dictionary<string, Package>> __installed_packages = new();

        /// <summary>
        /// Will check if a given Package is already in the cache.
        /// This checks only the "Discover Packages" cache
        /// </summary>
        /// <param name="p">The package to check</param>
        /// <returns>The already existing package if any, otherwhise p</returns>
        public static Package GetAvailablePackage(Package p)
        {
            Package? new_package = GetAvailablePackageOrNull(p);
            return new_package ?? p;
        }

        /// <summary>
        /// Will check if a given Package is already in the cache.
        /// This checks only the "Software Updates" cache
        /// </summary>
        /// <param name="p">The package to check</param>
        /// <returns>The already existing package if any, otherwhise p</returns>
        public static Package GetUpgradablePackage(Package p)
        {
            Package? new_package = GetUpgradablePackageOrNull(p);
            return new_package ?? p;
        }

        /// <summary>
        /// Will check if a given Package is already in the cache.
        /// This checks only the "Installed Packages" cache
        /// </summary>
        /// <param name="p">The package to check</param>
        /// <returns>The already existing package if any, otherwhise p</returns>
        public static Package GetInstalledPackage(Package p)
        {
            Package? new_package = GetInstalledPackageOrNull(p);
            return new_package ?? p;
        }

        /// <summary>
        /// Will check if a given Package is already in the cache.
        /// This checks only the "Discover Packages" cache
        /// </summary>
        /// <param name="p">The package to check</param>
        /// <returns>The already existing package if any, otherwhise null</returns>
        public static Package? GetAvailablePackageOrNull(Package p)
        {
            if (!__available_packages.ContainsKey(p.Manager))
            {
                return null;
            }

            foreach (Package package in __available_packages[p.Manager].Values)
            {
                if (p.Equals(package))
                {
                    return package;
                }
            }

            return null;
        }

        /// <summary>
        /// Will check if a given Package is already in the cache.
        /// This checks only the "Software Updates" cache
        /// </summary>
        /// <param name="p">The package to check</param>
        /// <returns>The already existing package if any, otherwhise null</returns>
        public static Package? GetUpgradablePackageOrNull(Package p)
        {
            if (!__upgradable_packages.ContainsKey(p.Manager))
            {
                return null;
            }

            foreach (Package package in __upgradable_packages[p.Manager].Values)
            {
                if (p.Equals(package))
                {
                    return package;
                }
            }

            return null;
        }

        /// <summary>
        /// Will check if a given Package is already in the cache.
        /// This checks only the "Installed Packages" cache
        /// </summary>
        /// <param name="p">The package to check</param>
        /// <returns>The already existing package if any, otherwhise null</returns>
        public static Package? GetInstalledPackageOrNull(Package p)
        {
            if (!__installed_packages.ContainsKey(p.Manager))
            {
                return null;
            }

            foreach (Package package in __installed_packages[p.Manager].Values)
            {
                if (p.Equals(package))
                {
                    return package;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks wether a Package with a newer version has been found in the Installed Packages cache
        /// </summary>
        /// <param name="p">The package to check agains</param>
        /// <returns>True if a newer version was found, false otherwhise</returns>
        public static bool NewerVersionIsInstalled(Package p)
        {
            if (!__installed_packages.ContainsKey(p.Manager))
            {
                return false;
            }

            foreach (Package package in __installed_packages[p.Manager].Values)
            {
                if (package.Manager == p.Manager && package.Id == p.Id && package.Version == p.NewVersion && package.Source.Name == p.Source.Name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
