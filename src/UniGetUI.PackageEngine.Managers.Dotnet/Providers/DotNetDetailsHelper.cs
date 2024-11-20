using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Managers.PowerShellManager;

namespace UniGetUI.PackageEngine.Managers.Chocolatey
{
    public class DotNetDetailsHelper : BaseNuGetDetailsHelper
    {
        public DotNetDetailsHelper(BaseNuGet manager) : base(manager)
        { }

        protected override string? GetInstallLocation_UnSafe(IPackage package)
        {
            return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "tools");
        }
    }
}
