using System.ComponentModel;
using UniGetUI.Core.Classes;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Classes.Packages;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.ManagerClasses.Manager
{
    public abstract class PackageManager : SingletonBase<PackageManager>
    {
        public ManagerProperties Properties { get; set; } = new();
        public ManagerCapabilities Capabilities { get; set; } = new();
        public ManagerStatus Status { get; set; } = new() { Found = false };
        public string Name { get; set; } = "Unset";
        public ManagerSource DefaultSource { get; set; }
        public static string[] FALSE_PACKAGE_NAMES = new string[] { "" };
        public static string[] FALSE_PACKAGE_IDS = new string[] { "" };
        public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "" };
        public bool ManagerReady { get; set; } = false;

        public BaseSourceProvider<PackageManager>? SourceProvider;

        public PackageManager()
        {
            Properties = GetProperties();
            DefaultSource = Properties.DefaultSource;
            Name = Properties.Name;
            Capabilities = GetCapabilities();
        }


        /// <summary>
        /// Initializes the Package Manager (asynchronously). Must be run before using any other method of the manager.
        /// </summary>
        /// <returns></returns>
        public async Task InitializeAsync()
        {
            // BEGIN integrity check
            if (Capabilities.SupportsCustomSources && SourceProvider == null)
                throw new Exception($"Manager {Name} has been declared as SupportsCustomSources but has no helper associated with it");
            // END integrity check

            try
            {
                Status = await LoadManager();


                if (SourceProvider != null && Status.Found)
                {

                    Task<ManagerSource[]> SourcesTask = GetSources();
                    Task winner = await Task.WhenAny(
                        SourcesTask,
                        Task.Delay(10000));
                    if (winner == SourcesTask)
                    {
                        ManagerReady = true;
                    }
                    else
                    {
                        ManagerReady = true;
                        Logger.Log(Name + " sources took too long to load, using known sources as default");
                    }
                }
                ManagerReady = true;

                string LogData = "\n▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄" +
                                 "\n█▀▀▀▀▀▀▀▀▀▀▀▀▀ MANAGER LOADED ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀" +
                                 "\n█ Name: " + Name +
                                 "\n█ Enabled: " + IsEnabled().ToString() +
                                    (IsEnabled() ? 
                                 "\n█ Found: " + Status.Found.ToString() +
                                    (Status.Found ? 
                                 "\n█ Fancye exe name: " + Properties.ExecutableFriendlyName +
                                 "\n█ Executable path: " + Status.ExecutablePath +
                                 "\n█ Call arguments: " + Properties.ExecutableCallArgs +
                                 "\n█ Version: \n" + "█   " + Status.Version.Replace("\n", "\n█   ")
                                    :
                                 "\n█ THE MANAGER WAS NOT FOUND. PERHAPS IT IS NOT " +
                                 "\n█ INSTALLED OR IT HAS BEEN MISCONFIGURED "
                                    )
                                    :
                                 "\n█ THE MANAGER IS DISABLED"
                                    ) +
                                 "\n▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀\n";

                Logger.Log(LogData);
            }
            catch (Exception e)
            {
                ManagerReady = true; // We need this to unblock the main thread
                Logger.Log("Could not initialize Package Manager " + Name + ": \n" + e.ToString());
            }
        }

        /// <summary>
        /// Returns a ManagerProperties object representing the properties of the package manager
        /// </summary>
        /// <returns></returns>
        protected abstract ManagerProperties GetProperties();
        /// <summary>
        /// Returns a ManagerCapabilities object representing the capabilities of the package manager
        /// </summary>
        /// <returns></returns>
        protected abstract ManagerCapabilities GetCapabilities();
        /// <summary>
        /// Returns a ManagerStatus object representing the current status of the package manager. This method runs asynchronously.
        /// </summary>
        /// <returns></returns>
        protected abstract Task<ManagerStatus> LoadManager();

        /// <summary>
        /// Returns true if the manager is enabled, false otherwise
        /// </summary>
        /// <returns></returns>
        public bool IsEnabled()
        {
            return !Settings.Get("Disable" + Name);
        }

        /// <summary>
        /// Returns an array of Package objects that the manager lists for the given query. Depending on the manager, the list may 
        /// also include similar results. This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<Package[]> FindPackages(string query)
        {
            try
            {
                Package[] packages = await FindPackages_UnSafe(query);
                for (int i = 0; i < packages.Length; i++)
                {
                    packages[i] = PackageFactory.GetAvailablePackageIfRepeated(packages[i]);
                }
                return packages;
            }
            catch (Exception e)
            {
                Logger.Log("Error finding packages on manager " + Name + " with query " + query + ": \n" + e.ToString());
                return new Package[] { };
            }
        }

        /// <summary>
        /// Returns an array of UpgradablePackage objects that represent the available updates reported by the manager. 
        /// This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<UpgradablePackage[]> GetAvailableUpdates()
        {
            try
            {
                UpgradablePackage[] packages = await GetAvailableUpdates_UnSafe();
                for (int i = 0; i < packages.Length; i++)
                    packages[i] = PackageFactory.GetUpgradablePackageIfRepeated(packages[i]);
                return packages;
            }
            catch (Exception e)
            {
                Logger.Log("Error finding updates on manager " + Name + ": \n" + e.ToString());
                return new UpgradablePackage[] { };
            }
        }

        /// <summary>
        /// Returns an array of Package objects that represent the installed reported by the manager. 
        /// This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        /// <returns></returns>
        public async Task<Package[]> GetInstalledPackages()
        {
            try
            {
                Package[] packages = await GetInstalledPackages_UnSafe();
                for (int i = 0; i < packages.Length; i++)
                    packages[i] = PackageFactory.GetInstalledPackageIfRepeated(packages[i]);
                return packages;
            }
            catch (Exception e)
            {
                Logger.Log("Error finding installed packages on manager " + Name + ": \n" + e.ToString());
                return new Package[] { };
            }
        }

        /// <summary>
        /// Returns a PackageDetails object that represents the details for the given Package object.
        /// This method is fail-safe and will return a valid but empty PackageDetails object with the package 
        /// id if an error occurs.
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public async Task<PackageDetails> GetPackageDetails(Package package)
        {
            try
            {
                return await GetPackageDetails_UnSafe(package);
            }
            catch (Exception e)
            {
                Logger.Log("Error getting package details on manager " + Name + " for package id=" + package.Id + ": \n" + e.ToString());
                return new PackageDetails(package);
            }
        }

        /// <summary>
        /// Returns the available versions to install for the given package. 
        /// If the manager does not support listing the versions, an empty array will be returned.
        /// This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        /// <param name="package">The package from which to load its versions</param>
        /// <returns>An array of stings containing the found versions, an empty array if none.</returns>
        public async Task<string[]> GetPackageVersions(Package package)
        {
            try
            {
                if (package.Manager.Capabilities.SupportsCustomVersions)
                    return await GetPackageVersions_Unsafe(package);
                else
                    return new string[0];
            }
            catch (Exception e)
            {
                Logger.Log("Error getting package versions on manager " + Name + " for package id=" + package.Id + ": \n" + e.ToString());
                return new string[0];
            }
        }

        /// <summary>
        /// Returns the available versions to install for the given package. 
        /// If the manager does not support listing the versions, an empty array must be returned.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The package from which to load its versions</param>
        /// <returns>An array of stings containing the found versions, an empty array if none.</returns>
        protected abstract Task<string[]> GetPackageVersions_Unsafe(Package package);

        /// <summary>
        /// Returns the available packages to install for the given query.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="query">The query string to search for</param>
        /// <returns>An array of Package objects</returns>
        protected abstract Task<Package[]> FindPackages_UnSafe(string query);

        /// <summary>
        /// Returns the available updates reported by the manager.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <returns>An array of UpgradablePackage objects</returns>
        protected abstract Task<UpgradablePackage[]> GetAvailableUpdates_UnSafe();

        /// <summary>
        /// Returns an array of Package objects containing the installed packages reported by the manager.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <returns>An array of Package objects</returns>
        protected abstract Task<Package[]> GetInstalledPackages_UnSafe();

        /// <summary>
        /// Returns the specific details and info for the given package.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The package for which to load the details</param>
        /// <returns>A PackageDetails with the package details loaded</returns>
        public abstract Task<PackageDetails> GetPackageDetails_UnSafe(Package package);


        /// <summary>
        /// Returns the command-line parameters to install the given package.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The Package going to be installed</param>
        /// <param name="options">The options in which it is going to be installed</param>
        /// <returns>An array of strings containing the parameters without the manager executable file</returns>
        public abstract string[] GetInstallParameters(Package package, InstallationOptions options);


        /// <summary>
        /// Returns the command-line parameters to update the given package.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The Package going to be updated</param>
        /// <param name="options">The options in which it is going to be updated</param>
        /// <returns>An array of strings containing the parameters without the manager executable file</returns>
        public abstract string[] GetUpdateParameters(Package package, InstallationOptions options);

        /// <summary>
        /// Returns the command-line parameters to uninstall the given package.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The Package going to be uninstalled</param>
        /// <param name="options">The options in which it is going to be uninstalled</param>
        /// <returns>An array of strings containing the parameters without the manager executable file</returns>
        public abstract string[] GetUninstallParameters(Package package, InstallationOptions options);

        /// <summary>
        /// Decides and returns the verdict of the install operation.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The package that was installed</param>
        /// <param name="options">The options with which the package was installed. They may be modified if the returned value is OperationVeredict.AutoRetry</param>
        /// <param name="ReturnCode">The exit code of the process</param>
        /// <param name="Output">the output of the process</param>
        /// <returns>An OperationVeredict value representing the result of the installation</returns>
        public abstract OperationVeredict GetInstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output);


        /// <summary>
        /// Decides and returns the verdict of the update operation.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The package that was updated</param>
        /// <param name="options">The options with which the package was updated. They may be modified if the returned value is OperationVeredict.AutoRetry</param>
        /// <param name="ReturnCode">The exit code of the process</param>
        /// <param name="Output">the output of the process</param>
        /// <returns>An OperationVeredict value representing the result of the update</returns>
        public abstract OperationVeredict GetUpdateOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output);

        /// <summary>
        /// Decides and returns the verdict of the uninstall operation.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="package">The package that was uninstalled</param>
        /// <param name="options">The options with which the package was uninstalled. They may be modified if the returned value is OperationVeredict.AutoRetry</param>
        /// <param name="ReturnCode">The exit code of the process</param>
        /// <param name="Output">the output of the process</param>
        /// <returns>An OperationVeredict value representing the result of the uninstall</returns>
        public abstract OperationVeredict GetUninstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output);

        /// <summary>
        /// Refreshes the Package Manager sources/indexes
        /// Each manager MUST implement this method.
        /// </summary>
        /// <returns></returns>
        public abstract Task RefreshPackageIndexes();

        public virtual async Task<ManagerSource[]> GetSources()
        {
            try
            {
                if (SourceProvider == null)
                    throw new Exception($"GetSources() called on Manager {Name} when ManagerSourceProvider helper is null");

                return await SourceProvider.GetSources();
            }
            catch (Exception e)
            {
                Logger.Log($"Error finding sources for Manager {Name}: \n" + e.ToString());
                return [];
            }
        }

        /// <summary>
        /// Will check if the Manager supports custom sources, and throw an exception if not
        /// </summary>
        /// <param name="MethodName"></param>
        /// <exception cref="Exception"></exception>
        private void AssertSourceCompatibility(string MethodName)
        {
            if (!Capabilities.SupportsCustomSources)
                throw new Exception($"Manager {Name} does not support custom sources but yet a GetKnownSources method was called");
            else if (SourceProvider == null)
                throw new Exception($"Manager {Name} does support custom sources but yet the source helper is null");
        }

#pragma warning disable CS8602
        public ManagerSource GetSourceOrDefault(string SourceName)
        {
            AssertSourceCompatibility("GetSourceFromName");
            return SourceProvider.SourceFactory.GetSourceOrDefault(SourceName);
        }
        public ManagerSource? GetSourceIfExists(string SourceName)
        {
            AssertSourceCompatibility("GetSourceIfExists");
            return SourceProvider.SourceFactory.GetSourceIfExists(SourceName);
        }

        public string[] GetAddSourceParameters(ManagerSource source)
        {
            AssertSourceCompatibility("GetAddSourceParameters");
            return SourceProvider.GetAddSourceParameters(source);
        }
        
        public string[] GetRemoveSourceParameters(ManagerSource source)
        {
            AssertSourceCompatibility("GetRemoveSourceParameters");
            return SourceProvider.GetRemoveSourceParameters(source);
        }
        
        public OperationVeredict GetAddSourceOperationVeredict(ManagerSource source, int ReturnCode, string[] Output)
        {
            AssertSourceCompatibility("GetAddSourceOperationVeredict");
            return SourceProvider.GetAddSourceOperationVeredict(source, ReturnCode, Output);
        }
        
        public OperationVeredict GetRemoveSourceOperationVeredict(ManagerSource source, int ReturnCode, string[] Output)
        {
            AssertSourceCompatibility("GetRemoveSourceOperationVeredict");
            return SourceProvider.GetRemoveSourceOperationVeredict(source, ReturnCode, Output);
        }
#pragma warning restore CS8602



    }
}
