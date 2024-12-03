using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Managers.PowerShellManager;

namespace UniGetUI.PackageEngine.Managers.Chocolatey
{
    public class PowerShellDetailsHelper : BaseNuGetDetailsHelper
    {
        public PowerShellDetailsHelper(BaseNuGet manager) : base(manager)
        { }

        protected override string? GetInstallLocation_UnSafe(IPackage package)
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
