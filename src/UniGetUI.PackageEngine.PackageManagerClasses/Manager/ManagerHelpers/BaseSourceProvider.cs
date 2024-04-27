using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers
{
    public abstract class BaseSourceProvider<T> where T : PackageManager
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
