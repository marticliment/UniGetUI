using System.Diagnostics;
using Windows.ApplicationModel.Core;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.Managers.PowerShellManager;

namespace UniGetUI.PackageEngine.Managers.Chocolatey
{
    public class PowerShellDetailsProvider : BaseNuGetDetailsProvider
    {
        public PowerShellDetailsProvider(BaseNuGet manager) : base(manager)
        { }

        protected override string? GetPackageInstallLocation_Unsafe(IPackage package)
        {
            var user_path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "WindowsPowerShell", "Modules", package.Id);
            if (Directory.Exists(user_path)) return user_path;

            var system_path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "WindowsPowerShell", "Modules", package.Id);
            if (Directory.Exists(system_path)) return system_path;

            return null;
        }
    }
}
