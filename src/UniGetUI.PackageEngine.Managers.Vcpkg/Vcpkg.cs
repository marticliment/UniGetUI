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