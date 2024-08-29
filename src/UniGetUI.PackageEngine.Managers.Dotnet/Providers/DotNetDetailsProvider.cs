using System.Diagnostics;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.Managers.PowerShellManager;

namespace UniGetUI.PackageEngine.Managers.Chocolatey
{
    public class DotNetDetailsProvider : BaseNuGetDetailsProvider
    {
        public DotNetDetailsProvider(BaseNuGet manager) : base(manager)
        { }

        protected override string? GetPackageInstallLocation_Unsafe(IPackage package)
        {
            return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "tools");
        }
    }
}
