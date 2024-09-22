using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Classes.Packages
{
    internal static class PackageCacher
    {
        private static readonly Dictionary<long, Package> __available_pkgs = [];
        private static readonly Dictionary<long, Package> __upgradable_pkgs = [];
        private static readonly Dictionary<long, Package> __installed_pkgs = [];

        /// <summary>
        /// Will check if a given Package is already in the cache. If not, it will be added to it
        /// This checks only the "Discover Packages" cache
        /// </summary>
        /// <param name="p">The package to check</param>
        /// <returns>The already existing package if any, otherwise p</returns>
        public static Package GetAvailablePackage(Package p)
        {
            Package? new_package = GetAvailablePackageOrNull(p);
            if (new_package is null)
            {
                AddPackageToCache(p, __available_pkgs);
            }

            return new_package ?? p;
        }

        /// <summary>
        /// Will check if a given Package is already in the cache. If not, it will be added to it
        /// This checks only the "Software Updates" cache
        /// </summary>
        /// <param name="p">The package to check</param>
        /// <returns>The already existing package if any, otherwise p</returns>
        public static Package GetUpgradablePackage(Package p)
        {
            Package? new_package = GetUpgradablePackageOrNull(p);
            if (new_package is null)
            {
                AddPackageToCache(p, __upgradable_pkgs);
            }

            return new_package ?? p;
        }

        /// <summary>
        /// Will check if a given Package is already in the cache. If not, it will be added to it
        /// This checks only the "Installed Packages" cache
        /// </summary>
        /// <param name="p">The package to check</param>
        /// <returns>The already existing package if any, otherwise p</returns>
        public static Package GetInstalledPackage(Package p)
        {
            Package? new_package = GetInstalledPackageOrNull(p);
            if (new_package is null)
            {
                AddPackageToCache(p, __installed_pkgs);
            }

            return new_package ?? p;
        }

        /// <summary>
        /// Will check if a given Package is already in the cache.
        /// This checks only the "Discover Packages" cache
        /// </summary>
        /// <param name="other">The package to check</param>
        /// <returns>The already existing package if any, otherwise null</returns>
        public static Package? GetAvailablePackageOrNull(Package other)
        {
            return __available_pkgs.GetValueOrDefault(other.GetHash());
        }

        /// <summary>
        /// Will check if a given Package is already in the cache.
        /// This checks only the "Software Updates" cache
        /// </summary>
        /// <param name="other">The package to check</param>
        /// <returns>The already existing package if any, otherwise null</returns>
        public static Package? GetUpgradablePackageOrNull(Package other)
        {
            return __upgradable_pkgs.GetValueOrDefault(other.GetHash());
        }

        /// <summary>
        /// Will check if a given Package is already in the cache.
        /// This checks only the "Installed Packages" cache
        /// </summary>
        /// <param name="other">The package to check</param>
        /// <returns>The already existing package if any, otherwise null</returns>
        public static Package? GetInstalledPackageOrNull(Package other)
        {
            return __installed_pkgs.GetValueOrDefault(other.GetVersionedHash());
        }

        /// <summary>
        /// Checks whether a Package with a newer version has been found in the Installed Packages cache
        /// </summary>
        /// <param name="other">The package to check again</param>
        /// <returns>True if a newer version was found, false otherwise</returns>
        public static bool NewerVersionIsInstalled(Package other)
        {
            foreach (Package found in __installed_pkgs.Values)
            {
                if (found.IsEquivalentTo(other) && found.VersionAsFloat == other.NewVersionAsFloat)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddPackageToCache(Package package, Dictionary<long, Package> map)
        {
            long hash = map == __installed_pkgs ? package.GetVersionedHash() : package.GetHash();
            map.Add(hash, package);
        }
    }
}
