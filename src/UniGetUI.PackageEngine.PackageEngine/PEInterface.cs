using System.Diagnostics.CodeAnalysis;
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
using UniGetUI.PackageEngine.Managers.VcpkgManager;
using UniGetUI.PackageEngine.Managers.WingetManager;
using UniGetUI.PackageEngine.PackageClasses;
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

        public static void LoadLoaders()
        {
            DiscoverablePackagesLoader.Instance = new DiscoverablePackagesLoader(Managers);
            InstalledPackagesLoader.Instance = new InstalledPackagesLoader(Managers);
            UpgradablePackagesLoader.Instance = new UpgradablePackagesLoader(Managers);
            PackageBundlesLoader.Instance = new PackageBundlesLoader_I(Managers);
        }

        public static void LoadManagers()
        {
            try
            {
                List<Task> initializeTasks = [];

                foreach (IPackageManager manager in Managers)
                {
                    initializeTasks.Add(Task.Run(manager.Initialize));
                }

                Task ManagersMegaTask = Task.WhenAll(initializeTasks);

                if (!ManagersMegaTask.Wait(TimeSpan.FromSeconds(ManagerLoadTimeout)))
                {
                    Logger.Warn("Timeout: Not all package managers have finished initializing.");
                }

                _ = InstalledPackagesLoader.Instance.ReloadPackages();
                _ = UpgradablePackagesLoader.Instance.ReloadPackages();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }


    public class PackageBundlesLoader_I : PackageBundlesLoader
    {
        public PackageBundlesLoader_I(IReadOnlyList<IPackageManager> managers) : base(managers)
        {
        }

        public override async Task AddPackagesAsync(IReadOnlyList<IPackage> foreign_packages)
        {
            List<IPackage> added = new();
            foreach (IPackage foreign in foreign_packages)
            {
                IPackage? package = null;

                if (foreign is not ImportedPackage && foreign is Package native)
                {
                    if (native.Source.IsVirtualManager)
                    {
                        Logger.Debug($"Adding native package with id={native.Id} to bundle as an INVALID package...");
                        package = new InvalidImportedPackage(native.AsSerializable_Incompatible(), NullSource.Instance);
                    }
                    else
                    {
                        Logger.Debug($"Adding native package with id={native.Id} to bundle as a VALID package...");
                        package = new ImportedPackage(await native.AsSerializableAsync(), native.Manager, native.Source);
                    }
                }
                else if (foreign is ImportedPackage imported)
                {
                    Logger.Debug($"Adding loaded imported package with id={imported.Id} to bundle...");
                    package = imported;
                }
                else if (foreign is InvalidImportedPackage invalid)
                {
                    Logger.Debug($"Adding loaded incompatible package with id={invalid.Id} to bundle...");
                    package = invalid;
                }
                else
                {
                    Logger.Error($"An IPackage instance id={foreign.Id} did not match the types Package, ImportedPackage or InvalidImportedPackage. This should never be the case");
                }

                if (package is not null)
                {   // Here, AddForeign is not used so a single PackagesChangedEvent can be invoked.
                    await AddPackage(package);
                    added.Add(package);
                }
            }
            InvokePackagesChangedEvent(true, added, []);
        }
    }
}
