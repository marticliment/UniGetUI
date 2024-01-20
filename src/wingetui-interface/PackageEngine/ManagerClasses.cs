using System;
using System.Collections.Generic;
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

        protected PackageManager()
        {
        }
        public async void Initialize()
        {
            Status = await LoadManager();
            Properties = GetProperties();
            Name = Properties.Name;
            Capabilities = GetCapabilities();
            MainSource = GetMainSource();
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

        public abstract void RefreshSources();

        public abstract ManagerSource GetMainSource();

    }

    public abstract class PackageManagerWithSources : PackageManager, IPackageManagerWithSources 
    {
        public ManagerSource[] Sources { get; set; }
        new public async void Initialize()
        {
            (this as PackageManager).Initialize();
            Sources = await GetSources();
        }

        public abstract Task<ManagerSource[]> GetSources();
    }

    public struct ManagerStatus
    {
        public bool Enabled = false;
        public bool Found = false;
        public string ExecutablePath = "";
        public ManagerStatus()
        { }

    }
    public struct ManagerProperties
    {
        public string Name;
        public string Description;
        public string IconId;
        public string ColorIconId;
        public string ExecutablePath;
        public string ExecutableCallArgs;
        public string ExecutableFriendlyName;
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
