using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

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
        public bool IsLoading { get; private set; }

        /// <summary>
        /// The collection of currently available packages
        /// </summary>
        public ObservableCollection<Package> Packages { get; private set; }
        private Dictionary<long, Package> PackageReference;

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

        bool ALLOW_MULTIPLE_PACKAGE_VERSIONS = false;
        protected string LOADER_IDENTIFIER;
        protected IEnumerable<PackageManager> Managers { get; private set; }

        public AbstractPackageLoader(IEnumerable<PackageManager> managers, bool AllowMultiplePackageVersions = false) 
        {
            Managers = managers;
            Packages = new ObservableCollection<Package>();
            PackageReference = new Dictionary<long, Package>();
            IsLoaded = false;
            IsLoading = false;
            LOADER_IDENTIFIER = "ABSTRACT";
        }

        /// <summary>
        /// Will trigger a forceful reload of the packages
        /// </summary>
        /// <returns></returns>
        public async Task ReloadPackages()
        {
            IsLoading = true;
            StartedLoading?.Invoke(this, new EventArgs());

            List<Task<Package[]>> tasks = new();

            foreach (PackageManager manager in Managers)
            {
                if (manager.IsEnabled() && manager.Status.Found)
                {
                    Task<Package[]> task = LoadPackagesFromManager(manager);
                    tasks.Add(task);
                }
            }

            while (tasks.Count > 0)
            {
                foreach (Task<Package[]> task in tasks.ToArray())
                {
                    if (!task.IsCompleted)
                        await Task.Delay(100);

                    if (task.IsCompleted)
                    {
                        if (task.IsCompletedSuccessfully)
                        {
                            int InitialCount = Packages.Count;
                            foreach (Package package in task.Result)
                            {
                                if (Contains(package) || !await IsPackageValid(package))
                                    continue;

                                AddPackage(package);
                                await WhenAddingPackage(package);
                                // TODO: AddPackageToSourcesList(package);
                            }
                            PackagesChanged?.Invoke(this, EventArgs.Empty);
                        }
                        tasks.Remove(task);
                    }
                }
            }

            IsLoading = false;
            FinishedLoading?.Invoke(this, new EventArgs());
            IsLoaded = true;
        }

        /// <summary>
        /// Resets the packages available on the loader
        /// </summary>
        public void ClearPackages()
        {
            Packages.Clear();
            PackageReference.Clear();
            IsLoaded = false;
            PackagesChanged?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Loads the packages from the given manager
        /// </summary>
        /// <param name="manager">The manager from which to load packages</param>
        /// <returns>A task that will load the packages</returns>
        protected abstract Task<Package[]> LoadPackagesFromManager(PackageManager manager);

        /// <summary>
        /// Checks whether the package is valid or must be skipped
        /// </summary>
        /// <param name="package">The package to check</param>
        /// <returns>True if the package can be added, false otherwhise</returns>
        protected abstract Task<bool> IsPackageValid(Package package);

        /// <summary>
        /// A method to post-process packages after they have been added.
        /// </summary>
        /// <param name="package">The package to process</param>
        /// <returns></returns>
        protected abstract Task WhenAddingPackage(Package package);

        /// <summary>
        /// Checks wether a package is contained on the current Loader
        /// </summary>
        /// <param name="package">The package to check against</param>
        /// <returns></returns>
        public bool Contains(Package package)
        {
            return PackageReference.ContainsKey(HashPackage(package));
        }

        /// <summary>
        /// Returns the appropiate hash of the package, according to the current loader configuration
        /// </summary>
        /// <param name="package">The pakage to hash</param>
        /// <returns>A long int containing the hash</returns>
        protected long HashPackage(Package package)
        {
            return ALLOW_MULTIPLE_PACKAGE_VERSIONS ? package.GetVersionedHash() : package.GetHash();
        }

        protected void AddPackage(Package package)
        {
            if(Contains(package))
            {
                Logger.Error($"ABORTED (Package loader {LOADER_IDENTIFIER}): Internally trying to add package {package.Id} was already found in PackageHash!");
                return;
            }

            Packages.Add(package);
            PackageReference.Add(HashPackage(package), package);
        }

        /// <summary>
        /// Adds a foreign package to the current loader. Perhaps a package has been recently installed and it needs to be added to the installed packages loader
        /// </summary>
        /// <param name="package">The package to add</param>
        public void AddForeign(Package? package)
        {
            if(package == null) return;
            if(Contains(package)) return;
            AddPackage(package);
            PackagesChanged?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Removes the given package from the list.
        /// </summary>
        /// <param name="package"></param>
        public void Remove(Package? package)
        {
            if (package == null) return;
            if (!Contains(package)) return;
            Packages.Remove(package);
            PackageReference.Remove(HashPackage(package));
            PackagesChanged?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Gets the corresponding package on the current loader.
        /// This method follows the equivalence settings for this loader
        /// </summary>
        /// <param name="package"></param>
        /// <returns>A Package? object</returns>
        public Package? GetEquivalentPackage(Package? package)
        {
            if(package == null) return null;
            if(!Contains(package)) return null;
            return PackageReference[HashPackage(package)];
        }

        /// <summary>
        /// Gets ALL of the equivalent packages on this loader.
        /// This method does NOT follow the equivalence settings for this loader
        /// </summary>
        /// <param name="package">The package for which to find the equivalent packages</param>
        /// <returns>A IEnumerable<Package> object</returns>
        public IEnumerable<Package> GetEquivalentPackages(Package? package)
        {
            if (package == null) return [];
            List<Package> result = new List<Package>();
            long hash_to_match = package.GetHash();
            foreach (Package local_package in Packages)
            {
                if (local_package.GetHash() == hash_to_match)
                {
                    result.Add(local_package);
                }
            }
            return result;
        }

        public Package? GetPackageForId(string id, string? sourceName = null)
        {
            foreach (var package in Packages)
                if (package.Id == id && (sourceName == null || package.Source.Name == sourceName))
                    return package;

            return null;
        }
    }
}
