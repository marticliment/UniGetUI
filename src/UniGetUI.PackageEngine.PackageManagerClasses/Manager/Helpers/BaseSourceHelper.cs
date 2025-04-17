using UniGetUI.Core.Classes;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Interfaces.ManagerProviders;

namespace UniGetUI.PackageEngine.Classes.Manager.Providers
{
    public abstract class BaseSourceHelper : IMultiSourceHelper
    {
        public ISourceFactory Factory { get; }
        protected IPackageManager Manager;

        public BaseSourceHelper(IPackageManager manager)
        {
            Manager = manager;
            Factory = new SourceFactory(manager);
        }

        public abstract string[] GetAddSourceParameters(IManagerSource source);
        public abstract string[] GetRemoveSourceParameters(IManagerSource source);
        protected abstract OperationVeredict _getAddSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output);
        protected abstract OperationVeredict _getRemoveSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output);

        public OperationVeredict GetAddOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            TaskRecycler<IReadOnlyList<IManagerSource>>.RemoveFromCache(_getSources);
            if (ReturnCode is 999 && Output.Last() == "Error: The operation was canceled by the user.")
            {
                Logger.Warn("Elevator [or GSudo] UAC prompt was canceled, not showing error message...");
                return OperationVeredict.Canceled;
            }
            return _getAddSourceOperationVeredict(source, ReturnCode, Output);
        }

        public OperationVeredict GetRemoveOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            TaskRecycler<IReadOnlyList<IManagerSource>>.RemoveFromCache(_getSources);
            if (ReturnCode is 999 && Output.Last() == "Error: The operation was canceled by the user.")
            {
                Logger.Warn("Elevator [or GSudo] UAC prompt was canceled, not showing error message...");
                return OperationVeredict.Canceled;
            }
            return _getRemoveSourceOperationVeredict(source, ReturnCode, Output);
        }

        /// <summary>
        /// Loads the sources for the manager. This method SHOULD NOT handle exceptions
        /// </summary>
        protected abstract IReadOnlyList<IManagerSource> GetSources_UnSafe();

        public virtual IReadOnlyList<IManagerSource> GetSources()
            => TaskRecycler<IReadOnlyList<IManagerSource>>.RunOrAttach(_getSources, 15);

        public virtual IReadOnlyList<IManagerSource> _getSources()
        {
            if (!Manager.IsReady()) { Logger.Warn($"Manager {Manager.Name} is disabled but yet GetSources was called"); return []; }

            try
            {
                IReadOnlyList<IManagerSource> sources = GetSources_UnSafe().ToArray();
                Factory.Reset();

                foreach (IManagerSource source in sources)
                {
                    Factory.AddSource(source);
                }

                Logger.Debug($"Loaded {sources.Count} sources for manager {Manager.Name}");
                return sources;
            }
            catch (Exception e)
            {
                Logger.Error("Error finding sources for manager " + Manager.Name);
                Logger.Error(e);
                return [];
            }
        }
    }
}
