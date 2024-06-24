using UniGetUI.PackageEngine.Classes.Manager.Interfaces;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Classes.Manager.Providers
{
    public abstract class BaseSourceProvider<T> : ISourceProvider where T : PackageManager
    {
        public readonly ManagerSourceFactory SourceFactory;
        protected T Manager;

        public BaseSourceProvider(T manager)
        {
            Manager = manager;
            SourceFactory = new(manager);
        }

        public abstract string[] GetAddSourceParameters(ManagerSource source);
        public abstract string[] GetRemoveSourceParameters(ManagerSource source);
        public abstract OperationVeredict GetAddSourceOperationVeredict(ManagerSource source, int ReturnCode, string[] Output);
        public abstract OperationVeredict GetRemoveSourceOperationVeredict(ManagerSource source, int ReturnCode, string[] Output);

        /// <summary>
        /// Loads the sources for the manager. This method SHOULD NOT handle exceptions
        /// </summary>
        /// <returns></returns>
        protected abstract Task<ManagerSource[]> GetSources_UnSafe();
        public virtual async Task<ManagerSource[]> GetSources()
        {
            ManagerSource[] sources = await GetSources_UnSafe();
            SourceFactory.Reset();

            foreach (ManagerSource source in sources)
                SourceFactory.AddSource(source);

            return sources;
        }
    }
}
