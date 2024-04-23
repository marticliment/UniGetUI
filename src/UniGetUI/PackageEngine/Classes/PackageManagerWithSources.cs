using UniGetUI.Core;
using UniGetUI.PackageEngine.Operations;
using System;
using System.Threading.Tasks;

namespace UniGetUI.PackageEngine.Classes
{
    public abstract class PackageManagerWithSources : PackageManager
    {
        public ManagerSourceFactory SourceFactory { get; private set; }
        public ManagerSource[] KnownSources { get; set; }

        public PackageManagerWithSources() : base()
        {
            SourceFactory = new(this);
        }

        public virtual async Task<ManagerSource[]> GetSources()
        {
            try
            {
                ManagerSource[] sources = await GetSources_UnSafe();
                SourceFactory.Reset();
                foreach (ManagerSource source in sources)
                    SourceFactory.AddSource(source);
                return sources;
            }
            catch (Exception e)
            {
                AppTools.Log("Error finding sources for manager " + Name + ": \n" + e.ToString());
                return new ManagerSource[0];
            }
        }

        public abstract ManagerSource[] GetKnownSources();
        public abstract string[] GetAddSourceParameters(ManagerSource source);
        public abstract string[] GetRemoveSourceParameters(ManagerSource source);
        public abstract OperationVeredict GetAddSourceOperationVeredict(ManagerSource source, int ReturnCode, string[] Output);
        
        public abstract OperationVeredict GetRemoveSourceOperationVeredict(ManagerSource source, int ReturnCode, string[] Output);
        protected abstract Task<ManagerSource[]> GetSources_UnSafe();
    }
}
