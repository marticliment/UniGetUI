using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Classes.Packages
{
    internal static class PackageFactory
    {
        private static Dictionary<PackageManager, Dictionary<string, Package>> __available_packages = new();
        private static Dictionary<PackageManager, Dictionary<string, Package>> __upgradable_packages = new();
        private static Dictionary<PackageManager, Dictionary<string, Package>> __installed_packages = new();
        
        public static Package GetAvailablePackageIfRepeated(Package p)
        {
            Package? old_package;

            if (!__available_packages.ContainsKey(p.Manager))
                __available_packages.Add(p.Manager, new());
            
            if (__available_packages[p.Manager].TryGetValue(p.GetHash(), out old_package) && old_package != null)
                return old_package;

            __available_packages[p.Manager].Add(p.GetHash(), p);
            return p;
        }

        public static Package GetUpgradablePackageIfRepeated(Package p)
        {
            Package? old_package;

            if (!__upgradable_packages.ContainsKey(p.Manager))
                __upgradable_packages.Add(p.Manager, new());

            if (__upgradable_packages[p.Manager].TryGetValue(p.GetHash(), out old_package) && old_package != null)
                return old_package;

            __upgradable_packages[p.Manager].Add(p.GetHash(), p);
            return p;
        }

        public static Package GetInstalledPackageIfRepeated(Package p)
        {
            Package? old_package;

            if (!__installed_packages.ContainsKey(p.Manager))
                __installed_packages.Add(p.Manager, new());

            if (__installed_packages[p.Manager].TryGetValue(p.GetHash(), out old_package) && old_package != null)
                return old_package;

            __installed_packages[p.Manager].Add(p.GetHash(), p);
            return p;
        }

        public static Package? FindPackageOnAvailableOrNull(Package p)
        {
            if (!__available_packages.ContainsKey(p.Manager))
                return null;

            foreach (var package in __available_packages[p.Manager].Values)
                if (p.Equals(package))
                    return package;

            return null;
        }

        public static Package? FindPackageOnUpdatesOrNull(Package p)
        {
            if (!__upgradable_packages.ContainsKey(p.Manager))
                return null;

            foreach (var package in __upgradable_packages[p.Manager].Values)
                if (p.Equals(package))
                    return package;

            return null;
        }

        public static Package? FindPackageOnInstalledOrNull(Package p)
        {
            if(!__installed_packages.ContainsKey(p.Manager))
                return null;

            foreach (var package in __installed_packages[p.Manager].Values)
                if (p.Equals(package))
                    return package;

            return null;
        }

        public static bool NewerVersionIsInstalled(UpgradablePackage p)
        {
            if (!__installed_packages.ContainsKey(p.Manager))
                return false;

            foreach (Package package in __installed_packages[p.Manager].Values)
                if (package.Manager == p.Manager && package.Id == p.Id && package.Version == p.NewVersion && package.Source.Name == p.Source.Name)
                    return true;

            return false;
        }
    }
}
