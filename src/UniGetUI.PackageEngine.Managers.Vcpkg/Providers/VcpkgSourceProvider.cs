using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager;
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
            => throw new NotImplementedException();

        public override string[] GetRemoveSourceParameters(IManagerSource source)
            => throw new NotImplementedException();

        protected override OperationVeredict _getAddSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
            => throw new NotImplementedException();

        protected override OperationVeredict _getRemoveSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
            => throw new NotImplementedException();

        protected override IEnumerable<IManagerSource> GetSources_UnSafe()
        {
            List<ManagerSource> Sources = [];
            // Retrieve all triplets on the system (in %VCPKG_ROOT%\triplets{\community})
            var (vcpkgRootFound, vcpkgRoot) = Vcpkg.GetVcpkgRoot();
            if (vcpkgRootFound)
            {
                string tripletLocation = Path.Join(vcpkgRoot, "triplets");
                string communityTripletLocation = Path.Join(vcpkgRoot, "triplets", "community");

                foreach (string tripletFile in Directory.EnumerateFiles(tripletLocation).Concat(Directory.EnumerateFiles(communityTripletLocation)))
                {
                    string triplet = Path.GetFileNameWithoutExtension(tripletFile);
                    Sources.Add(new ManagerSource(Manager, triplet, Vcpkg.URI_VCPKG_IO));
                }
            }

            return Sources;
        }

    }
}
