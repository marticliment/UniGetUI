using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAppSDK.Runtime.Packages;
using ModernWindow.Structures;
using Windows.Media.Core;

namespace ModernWindow.PackageEngine
{

    public abstract class SingletonBase<T> where T : SingletonBase<T>
    {
        private static readonly Lazy<T> Lazy =
            new(() => (Activator.CreateInstance(typeof(T), true) as T)!);

        public static T Instance => Lazy.Value;
    }
    public abstract class PackageManager : SingletonBase<PackageManager>, IPackageManager
    {
        public ManagerProperties Properties { get; set; }
        public ManagerCapabilities Capabilities { get; set; }
        public ManagerStatus Status { get; set; }
        public string Name { get; set; }
        protected MainAppBindings bindings = MainAppBindings.Instance;
        public ManagerSource MainSource { get; set; }

        public bool ManagerReady { get; set; } = false;

        protected PackageManager()
        {
        }
        public async Task Initialize()
        {
            Properties = GetProperties();
            Name = Properties.Name;
            Capabilities = GetCapabilities();
            MainSource = GetMainSource();
            Status = await LoadManager();
            Debug.WriteLine("Manager " + Name + " loaded");
            if(this is PackageManagerWithSources)
                (this as PackageManagerWithSources).Sources = await (this as PackageManagerWithSources).GetSources();
            ManagerReady = true;
        }

        protected abstract ManagerProperties GetProperties();
        protected abstract ManagerCapabilities GetCapabilities();
        protected abstract Task<ManagerStatus> LoadManager();

        public bool IsEnabled()
        {
            return !bindings.GetSettings("Disable"+Name);
        }

        public abstract Task<Package[]> FindPackages(string query);
        public abstract Task<UpgradablePackage[]> GetAvailableUpdates();
        public abstract Task<Package[]> GetInstalledPackages();
        public abstract Task<PackageDetails> GetPackageDetails(Package package);
        public abstract string[] GetInstallParameters(Package package, InstallationOptions options);
        public abstract string[] GetUpdateParameters(Package package, InstallationOptions options);
        public abstract string[] GetUninstallParameters(Package package, InstallationOptions options);

        /*

        All installation thread stuff here

        */

        public abstract Task RefreshSources();

        public abstract ManagerSource GetMainSource();

    }

    public abstract class PackageManagerWithSources : PackageManager, IPackageManagerWithSources 
    {
        public ManagerSource[] Sources { get; set; }
        public abstract Task<ManagerSource[]> GetSources();
    }

    public class ManagerStatus
    {
        public string Version = "";
        public bool Found = false;
        public string ExecutablePath = "";
        public ManagerStatus()
        { }

    }
    public class ManagerProperties
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconId { get; set; }
        public string ColorIconId { get; set; }
        public string ExecutableCallArgs { get; set; }
        public string ExecutableFriendlyName { get; set; }
        public string InstallVerb { get; set; }
        public string UpdateVerb { get; set; }
        public string UninstallVerb { get; set; }

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
        public ManagerSource.Capabilities Sources { get; set; }
        public ManagerCapabilities()
        {
            Sources = new ManagerSource.Capabilities();
        }
    }

    public class ManagerSource
    {
        public struct Capabilities
        {
            public bool KnowsUpdateDate { get; set; } = false;
            public bool KnowsPackageCount { get; set; } = false;
            public Capabilities()
            { }
        }

        public PackageManager Manager { get; }
        public string Name { get; }
        public Uri? Url { get; }
        public int? PackageCount { get; }
        public string UpdateDate { get; }

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

        public new string ToString()
        {
            if(Manager.Capabilities.SupportsCustomSources)
                return Manager.Name + ": " + Name;
            else
                return Manager.Name;
        }
    }
}
