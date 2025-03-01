using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.Providers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Managers.VcpkgManager
{
    internal sealed class VcpkgSourceHelper : BaseSourceHelper
    {
        public VcpkgSourceHelper(Vcpkg manager) : base(manager) { }

        public override string[] GetAddSourceParameters(IManagerSource source)
            => throw new NotImplementedException();

        public override string[] GetRemoveSourceParameters(IManagerSource source)
            => throw new NotImplementedException();

        protected override OperationVeredict _getAddSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
            => throw new NotImplementedException();

        protected override OperationVeredict _getRemoveSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
            => throw new NotImplementedException();

        protected override IReadOnlyList<IManagerSource> GetSources_UnSafe()
        {
            List<ManagerSource> Sources = [];

			foreach (string Triplet in Vcpkg.GetSystemTriplets()) {
				Sources.Add(new ManagerSource(Manager, Triplet, Vcpkg.URI_VCPKG_IO));
			}

            return Sources;
        }

    }
}
