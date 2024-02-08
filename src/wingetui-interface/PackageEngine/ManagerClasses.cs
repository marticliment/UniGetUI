using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAppSDK.Runtime.Packages;
using ModernWindow.Essentials;
using ModernWindow.PackageEngine;
using ModernWindow.Structures;
using Windows.Media.Core;

namespace ModernWindow.PackageEngine
{

    public abstract class PackageManager : SingletonBase<PackageManager>
    {
        public ManagerProperties Properties { get; set; } = new();
        public ManagerCapabilities Capabilities { get; set; } = new();
        public ManagerStatus Status { get; set; } = new() { Found = false };
        public string Name { get; set; } = "Unset";
        public static AppTools bindings = AppTools.Instance;
        public ManagerSource MainSource { get; set; }

        public static string[] FALSE_PACKAGE_NAMES = new string[] {""};
        public static string[] FALSE_PACKAGE_IDS = new string[] {""};
        public static string[] FALSE_PACKAGE_VERSIONS = new string[] {""};

        public bool ManagerReady { get; set; } = false;

        protected PackageManager()
        {
        }
        public async Task Initialize()
        {
            try
            {
                Properties = GetProperties();
                Name = Properties.Name;
                Capabilities = GetCapabilities();
                MainSource = GetMainSource();
                Status = await LoadManager();


                if (this is PackageManagerWithSources && Status.Found)
                {
                    var SourcesTask = (this as PackageManagerWithSources).GetSources();
                    var winner = await Task.WhenAny(
                        SourcesTask,
                        Task.Delay(10000));
                    if (winner == SourcesTask)
                    {
                        ManagerReady = true;
                        (this as PackageManagerWithSources).Sources = SourcesTask.Result;
                    }
                    else
                    {
                        ManagerReady = true;
                        Console.WriteLine(Name + " sources took too long to load, using known sources as default");
                        (this as PackageManagerWithSources).Sources = (this as PackageManagerWithSources).DefaultSources;
                    }
                }
                Debug.WriteLine("Manager " + Name + " loaded");
                ManagerReady = true;
            }
            catch (Exception e)
            {
                ManagerReady = true; // We need this to unblock the main thread
                Console.WriteLine("Could not initialize Package Manager " + Name + ": \n" + e.ToString());
            }
        }

        protected abstract ManagerProperties GetProperties();
        protected abstract ManagerCapabilities GetCapabilities();
        protected abstract Task<ManagerStatus> LoadManager();

        public bool IsEnabled()
        {
            return !bindings.GetSettings("Disable"+Name);
        }

        public virtual async Task<Package[]> FindPackages(string query)
        {
            try
            {
                return await FindPackages_UnSafe(query);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error finding packages on manager " + Name + " with query "  + query +  ": \n" + e.ToString());
                return new Package[] { };
            }
        }
        public virtual async Task<UpgradablePackage[]> GetAvailableUpdates()
        {
            try
            {
                return await GetAvailableUpdates_UnSafe();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error finding updates on manager " + Name + ": \n" + e.ToString());
                return new UpgradablePackage[] { };
            }
        }
        public virtual async Task<Package[]> GetInstalledPackages()
        {
            try
            {
                return await GetInstalledPackages_UnSafe();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error finding installed packages on manager " + Name + ": \n" + e.ToString());
                return new Package[] { };
            }
        }
        public virtual async Task<PackageDetails> GetPackageDetails(Package package)
        {
            try
            {
                return await GetPackageDetails_UnSafe(package);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error getting package details on manager " + Name + " for package id=" + package.Id +": \n" + e.ToString());
                return new PackageDetails(package);
            }
        }

        public virtual async Task<string[]> GetPackageVersions(Package package)
        {
            try
            {
                if(package.Manager.Capabilities.SupportsCustomVersions)
                    return await GetPackageVersions_Unsafe(package);
                else
                    return new string[0];
            }
            catch (Exception e)
            {
                Console.WriteLine("Error getting package versions on manager " + Name + " for package id=" + package.Id + ": \n" + e.ToString());
                return new string[0];
            }
        }

        protected abstract Task<string[]> GetPackageVersions_Unsafe(Package package);
        protected abstract Task<Package[]> FindPackages_UnSafe(string query);
        protected abstract Task<UpgradablePackage[]> GetAvailableUpdates_UnSafe();
        protected abstract Task<Package[]> GetInstalledPackages_UnSafe();
        public abstract Task<PackageDetails> GetPackageDetails_UnSafe(Package package);
        public abstract string[] GetInstallParameters(Package package, InstallationOptions options);
        public abstract string[] GetUpdateParameters(Package package, InstallationOptions options);
        public abstract string[] GetUninstallParameters(Package package, InstallationOptions options);
        public abstract OperationVeredict GetInstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output);
        public abstract OperationVeredict GetUpdateOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output);
        public abstract OperationVeredict GetUninstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output);
        public abstract Task RefreshSources();
        public abstract ManagerSource GetMainSource();
    }

    public abstract class PackageManagerWithSources : PackageManager 
    {
        public ManagerSource[] Sources { get; set; }
        public ManagerSource[] DefaultSources { get; set; }
        public Dictionary<string, ManagerSource> SourceReference = new Dictionary<string, ManagerSource>();
        public virtual async Task<ManagerSource[]> GetSources()
        {
            try
            {
                return await GetSources_UnSafe();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error finding sources for manager " + Name + ": \n" + e.ToString());
                return new ManagerSource[] { };
            }
        }
        protected abstract Task<ManagerSource[]> GetSources_UnSafe();
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
        public string Name { get; set; } = "Unset";
        public string Description { get; set; } = "Unset";
        public string IconId { get; set; } = "Unset";
        public string ColorIconId { get; set; } = "Unset";
        public string ExecutableCallArgs { get; set; } = "Unset";
        public string ExecutableFriendlyName { get; set; } = "Unset";
        public string InstallVerb { get; set; } = "Unset";
        public string UpdateVerb { get; set; } = "Unset";
        public string UninstallVerb { get; set; } = "Unset";

    }
    public struct ManagerCapabilities
    {
        public bool CanRunAsAdmin = false;
        public bool CanSkipIntegrityChecks = false;
        public bool CanRunInteractively = false;
        public bool CanRemoveDataOnUninstall = false;
        public bool SupportsCustomVersions = false;
        public bool SupportsCustomArchitectures = false;
        public Architecture[] SupportedCustomArchitectures = new Architecture[0];
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
        public virtual string IconId { get { return Manager.Properties.IconId; } }
        public bool IsVirtualManager = false;
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

        public override string ToString()
        {
            if(Manager.Capabilities.SupportsCustomSources)
                return Manager.Name + ": " + Name;
            else
                return Manager.Name;
        }

        public override bool Equals(object obj)
        {
            if(obj is not ManagerSource) 
                return false;
            else
                return (obj as ManagerSource).Manager == Manager && (obj as ManagerSource).Name == Name && (obj as ManagerSource).Url == Url;
        }
    }
}
