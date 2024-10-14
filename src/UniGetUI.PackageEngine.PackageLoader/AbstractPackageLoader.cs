using System.Collections.Concurrent;
using Windows.UI.Composition;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.PackageLoader
{
    public abstract class AbstractPackageLoader
    {
        /// <summary>
        /// Checks if the loader has loaded packages
        /// </summary>
        public bool IsLoaded { get; private set; }

        /// <summary>
        /// Checks if the loader is fetching new packages right now
        /// </summary>
        public bool IsLoading { get; protected set; }

        public bool Any()
        {
            return PackageReference.Any();
        }

        /// <summary>
        /// The collection of currently available packages
        /// </summary>
        public List<IPackage> Packages
        {
            get => PackageReference.Values.ToList();
        }

        protected readonly ConcurrentDictionary<long, IPackage> PackageReference;

        /// <summary>
        /// Fires when a block of packages (one package or more) is added or removed to the loader
        /// </summary>
        public event EventHandler<EventArgs>? PackagesChanged;

        /// <summary>
        /// Fires when the loader finishes fetching packages
        /// </summary>
        public event EventHandler<EventArgs>? FinishedLoading;

        /// <summary>
        /// Fires when the manager starts fetching packages
        /// </summary>
        public event EventHandler<EventArgs>? StartedLoading;

        readonly bool ALLOW_MULTIPLE_PACKAGE_VERSIONS;
        readonly bool DISABLE_RELOAD;
        protected string LOADER_IDENTIFIER;
        private int LoadOperationIdentifier;
        protected IEnumerable<IPackageManager> Managers { get; private set; }

        public AbstractPackageLoader(IEnumerable<IPackageManager> managers, string identifier, bool AllowMultiplePackageVersions = false, bool DisableReload = false)
        {
            Managers = managers;
            PackageReference = new ConcurrentDictionary<long, IPackage>();
            IsLoaded = false;
            IsLoading = false;
            DISABLE_RELOAD = DisableReload;
            ALLOW_MULTIPLE_PACKAGE_VERSIONS = AllowMultiplePackageVersions;
            LOADER_IDENTIFIER = identifier;
            ALLOW_MULTIPLE_PACKAGE_VERSIONS = AllowMultiplePackageVersions;
        }

        /// <summary>
        /// Stops the current loading process
        /// </summary>
        public void StopLoading(bool emitFinishSignal = true)
        {
            LoadOperationIdentifier = -1;
            IsLoaded = false;
            IsLoading = false;
            if(emitFinishSignal) InvokeFinishedLoadingEvent();
        }

        protected void InvokePackagesChangedEvent()
        {
            PackagesChanged?.Invoke(this, EventArgs.Empty);
        }

        protected void InvokeStartedLoadingEvent()
        {
            StartedLoading?.Invoke(this, EventArgs.Empty);
        }

        protected void InvokeFinishedLoadingEvent()
        {
            FinishedLoading?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Will trigger a forceful reload of the packages
        /// </summary>
        public virtual async Task ReloadPackages()
        {
            if(DISABLE_RELOAD)
            {
                InvokePackagesChangedEvent();
                return;
            }

            ClearPackages(emitFinishSignal: false);
            LoadOperationIdentifier = new Random().Next();
            int current_identifier = LoadOperationIdentifier;
            IsLoading = true;
            StartedLoading?.Invoke(this, EventArgs.Empty);

            List<Task<IEnumerable<IPackage>>> tasks = new();

            foreach (IPackageManager manager in Managers)
            {
                if (manager.IsReady())
                {
                    Task<IEnumerable<IPackage>> task = Task.Run(() => LoadPackagesFromManager(manager));
                    tasks.Add(task);
                }
            }

            while (tasks.Count > 0)
            {
                foreach (Task<IEnumerable<IPackage>> task in tasks.ToArray())
                {
                    if (!task.IsCompleted)
                    {
                        await Task.Delay(100).ConfigureAwait(false);
                    }

                    if (task.IsCompleted)
                    {
                        if (LoadOperationIdentifier == current_identifier && task.IsCompletedSuccessfully)
                        {
                            foreach (IPackage package in task.Result)
                            {
                                if (Contains(package) || !await IsPackageValid(package))
                                {
                                    continue;
                                }

                                AddPackage(package);
                                await WhenAddingPackage(package);
                            }
                            InvokePackagesChangedEvent();
                        }
                        tasks.Remove(task);
                    }
                }
            }

            if (LoadOperationIdentifier == current_identifier)
            {
                InvokeFinishedLoadingEvent();
                IsLoaded = true;
            }
            IsLoading = false;
        }

        /// <summary>
        /// Resets the packages available on the loader
        /// </summary>
        public void ClearPackages(bool emitFinishSignal = true)
        {
            StopLoading(emitFinishSignal);
            PackageReference.Clear();
            IsLoaded = false;
            IsLoading = false;
            InvokePackagesChangedEvent();
        }

        /// <summary>
        /// Loads the packages from the given manager
        /// </summary>
        /// <param name="manager">The manager from which to load packages</param>
        /// <returns>A task that will load the packages</returns>
        protected abstract IEnumerable<IPackage> LoadPackagesFromManager(IPackageManager manager);

        /// <summary>
        /// Checks whether the package is valid or must be skipped
        /// </summary>
        /// <param name="package">The package to check</param>
        /// <returns>True if the package can be added, false otherwise</returns>
        protected abstract Task<bool> IsPackageValid(IPackage package);

        /// <summary>
        /// A method to post-process packages after they have been added.
        /// </summary>
        /// <param name="package">The package to process</param>
        protected abstract Task WhenAddingPackage(IPackage package);

        /// <summary>
        /// Checks whether a package is contained on the current Loader
        /// </summary>
        /// <param name="package">The package to check against</param>
        public bool Contains(IPackage package)
        {
            return PackageReference.ContainsKey(HashPackage(package));
        }

        /// <summary>
        /// Returns the appropriate hash of the package, according to the current loader configuration
        /// </summary>
        /// <param name="package">The package to hash</param>
        /// <returns>A long int containing the hash</returns>
        protected long HashPackage(IPackage package)
        {
            return ALLOW_MULTIPLE_PACKAGE_VERSIONS ? package.GetVersionedHash() : package.GetHash();
        }

        protected void AddPackage(IPackage package)
        {
            if (Contains(package))
            {
                return;
            }

            PackageReference.TryAdd(HashPackage(package), package);
        }

        /// <summary>
        /// Adds a foreign package to the current loader. Perhaps a package has been recently installed and it needs to be added to the installed packages loader
        /// </summary>
        /// <param name="package">The package to add</param>
        public void AddForeign(IPackage? package)
        {
            if (package is null)
            {
                return;
            }

            AddPackage(package);
            InvokePackagesChangedEvent();
        }

        /// <summary>
        /// Removes the given package from the list.
        /// </summary>
        public void Remove(IPackage? package)
        {
            if (package is null)
            {
                return;
            }

            if (!Contains(package))
            {
                return;
            }

            PackageReference.Remove(HashPackage(package), out IPackage? _);
            InvokePackagesChangedEvent();
        }

        /// <summary>
        /// Gets the corresponding package on the current loader.
        /// This method follows the equivalence settings for this loader
        /// </summary>
        /// <returns>A Package? object</returns>
        public IPackage? GetEquivalentPackage(IPackage? package)
        {
            if (package is null)
            {
                return null;
            }

            PackageReference.TryGetValue(HashPackage(package), out IPackage? eq);
            return eq;
        }

        /// <summary>
        /// Gets ALL of the equivalent packages on this loader.
        /// This method does NOT follow the equivalence settings for this loader
        /// </summary>
        /// <param name="package">The package for which to find the equivalent packages</param>
        /// <returns>A IEnumerable<Package> object</returns>
        public IEnumerable<IPackage> GetEquivalentPackages(IPackage? package)
        {
            if (package is null)
            {
                return [];
            }

            List<IPackage> result = [];
            long hash_to_match = package.GetHash();
            foreach (IPackage local_package in Packages)
            {
                if (local_package.GetHash() == hash_to_match)
                {
                    result.Add(local_package);
                }
            }
            return result;
        }

        public IPackage? GetPackageForId(string id, string? sourceName = null)
        {
            foreach (IPackage package in Packages)
            {
                if (package.Id == id && (sourceName is null || package.Source.Name == sourceName))
                {
                    return package;
                }
            }

            return null;
        }

        public int Count()
        {
            return PackageReference.Count;
        }
    }
}
