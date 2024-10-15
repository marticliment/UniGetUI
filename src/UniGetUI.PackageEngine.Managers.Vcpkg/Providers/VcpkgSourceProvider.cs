using UniGetUI.PackageEngine.Classes.Manager.Providers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Managers.VcpkgManager
{
    internal sealed class VcpkgSourceProvider : BaseSourceProvider<PackageManager>
    {
        public VcpkgSourceProvider(Vcpkg manager) : base(manager) { }

		public override string[] GetAddSourceParameters(IManagerSource source)
        {
            throw new NotImplementedException();
        }

        public override string[] GetRemoveSourceParameters(IManagerSource source)
        {
            throw new NotImplementedException();
        }

        public override OperationVeredict GetAddSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            throw new NotImplementedException();
        }

        public override OperationVeredict GetRemoveSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<IManagerSource> GetSources_UnSafe()
        {
            throw new NotImplementedException();
        }
    }
}
