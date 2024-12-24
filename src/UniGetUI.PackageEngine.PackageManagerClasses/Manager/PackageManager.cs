using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.Classes;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Classes.Packages;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Interfaces.ManagerProviders;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.ManagerClasses.Manager
{
    public abstract class PackageManager : IPackageManager
    {
        private const int PackageListingTaskTimeout = 60;

        public ManagerProperties Properties { get; set; } = new(IsDummy: true);
        public ManagerCapabilities Capabilities { get; set; } = new(IsDummy: true);
        public ManagerStatus Status { get; set; } = new() { Found = false };
        public string Name { get => Properties.Name; }
        public string DisplayName { get => Properties.DisplayName ?? Name; }
        public IManagerSource DefaultSource { get => Properties.DefaultSource; }
        public bool ManagerReady { get; set; }
        public IManagerLogger TaskLogger { get; }
        public IEnumerable<ManagerDependency> Dependencies { get; protected set; } = [];
        public IMultiSourceHelper SourcesHelper { get; protected set; } = new NullSourceHelper();
        public IPackageDetailsHelper DetailsHelper { get; protected set; } = null!;
        public IPackageOperationHelper OperationHelper { get; protected set; } = null!;

        private readonly bool _baseConstructorCalled;

        public PackageManager()
        {
            _baseConstructorCalled = true;
            TaskLogger = new ManagerLogger(this);
        }

        private static void Throw(string message)
        {
            throw new InvalidDataException(message);
        }

        /// <summary>
        /// Initializes the Package Manager (asynchronously). Must be run before using any other method of the manager.
        /// </summary>
        public virtual void Initialize()
        {
            // BEGIN integrity check
            if (!_baseConstructorCalled) Throw($"The Manager {Properties.Name} has not called the base constructor.");
            if (Capabilities.IsDummy) Throw($"The current instance of PackageManager with name ${Properties.Name} does not have a valid Capabilities object");
            if (Properties.IsDummy) Throw($"The current instance of PackageManager with name ${Properties.Name} does not have a valid Properties object");

            if (OperationHelper is NullPkgOperationHelper) Throw($"Manager {Name} does not have an OperationProvider");
            if (DetailsHelper is NullPkgDetailsHelper) Throw($"Manager {Name} does not have a valid DetailsHelper");

            if (Capabilities.SupportsCustomSources && SourcesHelper is NullSourceHelper)
                Throw($"Manager {Name} has been declared as SupportsCustomSources but has no helper associated with it");
            // END integrity check

            Properties.DefaultSource.RefreshSourceNames();
            foreach(var source in Properties.KnownSources) source.RefreshSourceNames();

            try
            {
                Status = LoadManager();

                if (IsReady() && Capabilities.SupportsCustomSources)
                {
                    Task<IEnumerable<IManagerSource>> sourcesTask = Task.Run(SourcesHelper.GetSources);

                    if (sourcesTask.Wait(TimeSpan.FromSeconds(15)))
                    {
                        foreach (var source in sourcesTask.Result)
                            SourcesHelper?.Factory.AddSource(source);
                    }
                    else
                    {
                        Logger.Warn(Name + " sources took too long to load, using known sources as default");
                    }
                }
                ManagerReady = true;

                string LogData = "▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄" +
                               "\n█▀▀▀▀▀▀▀▀▀▀▀▀▀ MANAGER LOADED ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀" +
                               "\n█ Name: " + Name +
                               "\n█ Enabled: " + IsEnabled().ToString() +
                               (IsEnabled() ?
                               "\n█ Found: " + Status.Found.ToString() +
                               (Status.Found ?
                               "\n█ Fancy exe name: " + Properties.ExecutableFriendlyName +
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
                               "\n▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀";

                Logger.Info(LogData);
            }
            catch (Exception e)
            {
                ManagerReady = true; // We need this to unblock the main thread
                Logger.Error("Could not initialize Package Manager " + Name);
                Logger.Error(e);
            }
        }

        /// <summary>
        /// Returns a ManagerStatus object representing the current status of the package manager. This method runs asynchronously.
        /// </summary>
        protected abstract ManagerStatus LoadManager();

        /// <summary>
        /// Returns true if the manager is enabled, false otherwise
        /// </summary>
        public bool IsEnabled()
        {
            return !Settings.GetDictionaryItem<string, bool>("DisabledManagers", Name);
        }

        /// <summary>
        /// Returns true if the manager is enabled and available (the required executable files were found). Returns false otherwise
        /// </summary>
        public bool IsReady()
        {
            return IsEnabled() && Status.Found;
        }

        /// <summary>
        /// Returns an array of Package objects that the manager lists for the given query. Depending on the manager, the list may
        /// also include similar results. This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        public IEnumerable<IPackage> FindPackages(string query)
            => _findPackages(query, false);

        private IEnumerable<IPackage> _findPackages(string query, bool SecondAttempt)
        {
            if (!IsReady()) { Logger.Warn($"Manager {Name} is disabled but yet FindPackages was called"); return []; }
            try
            {
                var task = Task.Run(() => FindPackages_UnSafe(query));
                if (!task.Wait(TimeSpan.FromSeconds(PackageListingTaskTimeout)))
                {
                    if (!Settings.Get("DisableTimeoutOnPackageListingTasks"))
                        throw new TimeoutException($"Task _getInstalledPackages for manager {Name} did not finish after " +
                                                   $"{PackageListingTaskTimeout} seconds, aborting.  You may disable " +
                                                   $"timeouts from UniGetUI Advanced Settings");
                    else
                        task.Wait();
                }

                Package[] packages = task.GetAwaiter().GetResult().ToArray();

                for (int i = 0; i < packages.Length; i++)
                {
                    packages[i] = PackageCacher.GetAvailablePackage(packages[i]);
                }
                Logger.Info($"Found {packages.Length} available packages from {Name} with the query {query}");
                return packages;
            }
            catch (Exception e)
            {
                if (!SecondAttempt)
                {
                    while (e is AggregateException) e = e.InnerException ?? new InvalidOperationException("How did we get here?");
                    Logger.Warn($"Manager {DisplayName} failed to find packages with exception {e.GetType().Name}: {e.Message}");
                    Logger.Warn($"Since this was the first attempt, {Name}.AttemptFastRepair() will be called and the procedure will be restarted");
                    AttemptFastRepair();
                    return _findPackages(query, true);
                }
                else
                {
                    Logger.Error("Error finding packages on manager " + Name + " with query " + query);
                    Logger.Error(e);
                    return [];
                }
            }
        }

        /// <summary>
        /// Returns an array of UpgradablePackage objects that represent the available updates reported by the manager.
        /// This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        public IEnumerable<IPackage> GetAvailableUpdates()
            => _getAvailableUpdates(false);

        private IEnumerable<IPackage> _getAvailableUpdates(bool SecondAttempt)
        {
            if (!IsReady()) { Logger.Warn($"Manager {Name} is disabled but yet GetAvailableUpdates was called"); return []; }
            try
            {
                Task.Run(RefreshPackageIndexes).Wait(TimeSpan.FromSeconds(60));

                var task = Task.Run(GetAvailableUpdates_UnSafe);
                if (!task.Wait(TimeSpan.FromSeconds(PackageListingTaskTimeout)))
                {
                    if (!Settings.Get("DisableTimeoutOnPackageListingTasks"))
                        throw new TimeoutException($"Task _getInstalledPackages for manager {Name} did not finish after " +
                                                   $"{PackageListingTaskTimeout} seconds, aborting.  You may disable " +
                                                   $"timeouts from UniGetUI Advanced Settings");                    else
                        task.Wait();
                }

                Package[] packages = task.GetAwaiter().GetResult().ToArray();

                for (int i = 0; i < packages.Length; i++)
                {
                    packages[i] = PackageCacher.GetUpgradablePackage(packages[i]);
                }

                Logger.Info($"Found {packages.Length} available updates from {Name}");
                return packages;
            }
            catch (Exception e)
            {
                if (!SecondAttempt)
                {
                    while (e is AggregateException) e = e.InnerException ?? new InvalidOperationException("How did we get here?");
                    Logger.Warn($"Manager {DisplayName} failed to list available updates with exception {e.GetType().Name}: {e.Message}");
                    Logger.Warn($"Since this was the first attempt, {Name}.AttemptFastRepair() will be called and the procedure will be restarted");
                    AttemptFastRepair();
                    return _getAvailableUpdates(true);
                }
                else
                {
                    Logger.Error("Error finding updates on manager " + Name);
                    Logger.Error(e);
                    return [];
                }
            }
        }

        /// <summary>
        /// Returns an array of Package objects that represent the installed reported by the manager.
        /// This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        public IEnumerable<IPackage> GetInstalledPackages()
            => _getInstalledPackages(false);

        private IEnumerable<IPackage> _getInstalledPackages(bool SecondAttempt)
        {
            if (!IsReady()) { Logger.Warn($"Manager {Name} is disabled but yet GetInstalledPackages was called"); return []; }
            try
            {
                var task = Task.Run(GetInstalledPackages_UnSafe);
                if (!task.Wait(TimeSpan.FromSeconds(PackageListingTaskTimeout)))
                {
                    if (!Settings.Get("DisableTimeoutOnPackageListingTasks"))
                        throw new TimeoutException($"Task _getInstalledPackages for manager {Name} did not finish after " +
                                                   $"{PackageListingTaskTimeout} seconds, aborting.  You may disable " +
                                                   $"timeouts from UniGetUI Advanced Settings");                    else
                        task.Wait();
                }

                Package[] packages = task.GetAwaiter().GetResult().ToArray();

                for (int i = 0; i < packages.Length; i++)
                {
                    packages[i] = PackageCacher.GetInstalledPackage(packages[i]);
                }

                Logger.Info($"Found {packages.Length} installed packages from {Name}");
                return packages;
            }
            catch (Exception e)
            {
                if (!SecondAttempt)
                {
                    while (e is AggregateException) e = e.InnerException ?? new InvalidOperationException("How did we get here?");
                    Logger.Warn($"Manager {DisplayName} failed to list installed packages with exception {e.GetType().Name}: {e.Message}");
                    Logger.Warn($"Since this was the first attempt, {Name}.AttemptFastRepair() will be called and the procedure will be restarted");
                    AttemptFastRepair();
                    return _getInstalledPackages(true);
                }
                else
                {
                    Logger.Error("Error finding installed packages on manager " + Name);
                    Logger.Error(e);
                    return [];
                }
            }
        }

        /// <summary>
        /// Returns the available packages to install for the given query.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <param name="query">The query string to search for</param>
        /// <returns>An array of Package objects</returns>
        protected abstract IEnumerable<Package> FindPackages_UnSafe(string query);

        /// <summary>
        /// Returns the available updates reported by the manager.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <returns>An array of UpgradablePackage objects</returns>
        protected abstract IEnumerable<Package> GetAvailableUpdates_UnSafe();

        /// <summary>
        /// Returns an array of Package objects containing the installed packages reported by the manager.
        /// Each manager MUST implement this method.
        /// </summary>
        /// <returns>An array of Package objects</returns>
        protected abstract IEnumerable<Package> GetInstalledPackages_UnSafe();

        /// <summary>
        /// Refreshes the Package Manager sources/indexes
        /// Each manager MUST implement this method.
        /// </summary>
        public virtual async void RefreshPackageIndexes()
        {
            Logger.Debug($"Manager {Name} has not implemented RefreshPackageIndexes");
            await Task.CompletedTask;
        }

        public virtual void AttemptFastRepair()
        {
            // Implementing this method is optional
        }
    }
}
