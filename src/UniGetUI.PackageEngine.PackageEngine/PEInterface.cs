using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Managers.CargoManager;
using UniGetUI.PackageEngine.Managers.ChocolateyManager;
using UniGetUI.PackageEngine.Managers.DotNetManager;
using UniGetUI.PackageEngine.Managers.NpmManager;
using UniGetUI.PackageEngine.Managers.PipManager;
using UniGetUI.PackageEngine.Managers.PowerShell7Manager;
using UniGetUI.PackageEngine.Managers.PowerShellManager;
using UniGetUI.PackageEngine.Managers.ScoopManager;
using UniGetUI.PackageEngine.Managers.WingetManager;
using UniGetUI.PackageEngine.Managers.VcpkgManager;
using UniGetUI.PackageEngine.PackageLoader;

namespace UniGetUI.PackageEngine
{
    /// <summary>
    /// The interface/entry point for the UniGetUI Package Engine
    /// </summary>
    public static class PEInterface
    {
        private const int ManagerLoadTimeout = 60; // 60 seconds timeout for Package Manager initialization (in seconds)

        public static readonly WinGet WinGet = new();
        public static readonly Scoop Scoop = new();
        public static readonly Chocolatey Chocolatey = new();
        public static readonly Npm Npm = new();
        public static readonly Pip Pip = new();
        public static readonly DotNet DotNet = new();
        public static readonly PowerShell PowerShell = new();
        public static readonly PowerShell7 PowerShell7 = new();
        public static readonly Cargo Cargo = new();
        public static readonly Vcpkg Vcpkg = new();

        public static readonly IPackageManager[] Managers = [WinGet, Scoop, Chocolatey, Npm, Pip, Cargo, Vcpkg, DotNet, PowerShell, PowerShell7];

        public static readonly DiscoverablePackagesLoader DiscoveredPackagesLoader = new(Managers);
        public static readonly UpgradablePackagesLoader UpgradablePackagesLoader = new(Managers);
        public static readonly InstalledPackagesLoader InstalledPackagesLoader = new(Managers);
        public static readonly PackageBundlesLoader PackageBundlesLoader = new(Managers);

        public static void Initialize()
        {
            List<Task> initializeTasks = [];

            foreach (IPackageManager manager in Managers)
            {
                initializeTasks.Add(Task.Run(manager.Initialize));
            }

            Task ManagersMetaTask = Task.WhenAll(initializeTasks);
            try
            {
                ManagersMetaTask.Wait(TimeSpan.FromSeconds(ManagerLoadTimeout));
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            if (ManagersMetaTask.IsCompletedSuccessfully == false)
            {
                Logger.Warn("Timeout: Not all package managers have finished initializing.");
            }

            _ = InstalledPackagesLoader.ReloadPackages();
            _ = UpgradablePackagesLoader.ReloadPackages();
        }
    }
}
