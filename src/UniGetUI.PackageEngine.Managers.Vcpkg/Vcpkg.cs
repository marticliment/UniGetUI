using System.Diagnostics;
using System.Text.RegularExpressions;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.VcpkgManager {
    public class Vcpkg : PackageManager
    {
		public Vcpkg()
		{
			Capabilities = new ManagerCapabilities
			{
				CanRunAsAdmin = false, // TODO: check this; should this be true since we need admin to install to protected directories?
				SupportsCustomSources = true, // TODO: check this; are different triplets "different sources"?
			};

			Properties = new ManagerProperties
			{
				Name = "vcpkg",
				Description = CoreTools.Translate("A popular C/C++ library manager. Full of C/C++ libraries and other C/C++-related utilities<br>Contains: <b>C/C++ libraries and related utilities</b>"),
				// TODO: IconID
				// TODO: ColorIconID
				ExecutableFriendlyName = "vcpkg",
				InstallVerb = "install",
				UninstallVerb = "remove",
				UpdateVerb = "update",
				ExecutableCallArgs = "",
				// TODO: Sources
				DefaultSource = new ManagerSource(this, "Windows x64", new Uri("https://vcpkg.io/")),
                KnownSources = [new ManagerSource(this, "Windows x64", new Uri("https://vcpkg.io/"))],
			};

			SourceProvider = new VcpkgSourceProvider(this);
			OperationProvider = new VcpkgOperationProvider(this);
		}

        protected override IEnumerable<Package> FindPackages_UnSafe(string query)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<Package> GetAvailableUpdates_UnSafe()
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<Package> GetInstalledPackages_UnSafe()
        {
            throw new NotImplementedException();
        }

        protected override ManagerStatus LoadManager()
        {
            throw new NotImplementedException();
        }
    }
}