using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace package_engine
{
    public abstract class PackageManager : IPackageManager
    {
        public ManagerProperties Properties { get; }
        public ManagerCapabilities Capabilities { get; }
        public string Name { get; }
        public string ExecutablePath { get; }
        public string ExecutableCommand { get; }

        public PackageManager()
        {
            Properties = new ManagerProperties()
            {
                Name = "Manager",
                Description = "A package manager",
                IconId = "manager",
                ColorIconId = "manager_color",
                ExecutablePath = "manager.exe",
                ExecutableName = "manager",
            };
        }

        bool IsEnabled()
        {
            return false;
        }

        public abstract Task<Package[]> FindPackages(string query);
        public abstract Task<UpgradablePackage[]> GetAvailableUpdates();
        public abstract Task<Package[]> GetInstalledPackages();
    }

    public abstract class PackageManagerWithSources : PackageManager, IPackageManagerWithSources
    {
        public abstract Task<ManagerSource[]> GetSources();
    }

    public struct ManagerProperties
    {
        public string Name;
        public string Description;
        public string IconId;
        public string ColorIconId;
        public string ExecutablePath;
        public string ExecutableName;
        public string InstallVerb;
        public string UpdateVerb;
        public string UninstallVerb;

    }
    public struct ManagerCapabilities
    {
        public bool CanRunAsAdmin = false;
        public bool CanSkipIntegrityChecks = false;
        public bool CanRunInteractively = false;
        public bool CanRemoveDataOnUninstall = false;
        public bool SupportsCustomVersions = false;
        public bool SupportsCustomArchitectures = false;
        public bool SupportsCustomScopes = false;
        public bool SupportsPreRelease = false;
        public bool SupportsCustomLocations = false;
        public bool SupportsCustomSources = false;
        public ManagerSource.Capabilities Sources { get; }
        public ManagerCapabilities()
        {
            Sources = new ManagerSource.Capabilities();
        }
    }

    public class ManagerSource
    {
        public struct Capabilities
        {
            public bool KnowsUpdateDate = false;
            public bool KnowsPackageCount = false;
            public Capabilities()
            { }
        }

        PackageManager Manager { get; }
        public string Name { get; }
        public Uri? Url { get; }
        public int? PackageCount { get; }
        public string? UpdateDate { get; }

        public ManagerSource(PackageManager manager, string name, Uri? url = null, int? packageCount = 0, string? updateDate = null)
        {
            Manager = manager;
            Name = name;
            Url = url;
            if(manager.Capabilities.Sources.KnowsPackageCount)
                PackageCount = packageCount;
            if(manager.Capabilities.Sources.KnowsUpdateDate)
                UpdateDate = updateDate;
        }
    }
}
