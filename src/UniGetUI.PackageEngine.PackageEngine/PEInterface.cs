using UniGetUI.PackageEngine.ManagerClasses.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.PackageEngine.Managers.WingetManager;
using UniGetUI.PackageEngine.Managers.ScoopManager;
using UniGetUI.PackageEngine.Managers.ChocolateyManager;
using UniGetUI.PackageEngine.Managers.DotNetManager;
using UniGetUI.PackageEngine.Managers.NpmManager;
using UniGetUI.PackageEngine.Managers.PowerShellManager;
using UniGetUI.PackageEngine.Managers.PipManager;
using UniGetUI.PackageEngine.PackageLoader;
using UniGetUI.Core.Logging;

namespace UniGetUI.PackageEngine
{
    public static class PEInterface
    {
        private const int ManagerLoadTimeout = 10000; // 10 seconds timeout for Package Manager initialization

        public static readonly WinGet WinGet = new WinGet();
        public static readonly Scoop Scoop = new Scoop();
        public static readonly Chocolatey Chocolatey = new Chocolatey();
        public static readonly Npm Npm = new Npm();
        public static readonly Pip Pip = new Pip();
        public static readonly DotNet DotNet = new DotNet();
        public static readonly PowerShell PowerShell = new PowerShell();

        public static readonly PackageManager[] Managers = [WinGet, Scoop, Chocolatey, Npm, Pip, DotNet, PowerShell];

        public static readonly DiscoverablePackagesLoader DiscoveredPackagesLoader = new(Managers);
        public static readonly UpgradablePackagesLoader UpgradablePackagesLoader = new(Managers);
        public static readonly InstalledPackagesLoader InstalledPackagesLoader = new(Managers);

        public static async Task Initialize()
        {
            List<Task> initializeTasks = new();

            foreach (PackageManager manager in Managers)
            {
                initializeTasks.Add(manager.InitializeAsync());
            }

            Task ManagersMetaTask = Task.WhenAll(initializeTasks);
            try
            {
                await ManagersMetaTask.WaitAsync(TimeSpan.FromMilliseconds(ManagerLoadTimeout));
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            if (ManagersMetaTask.IsCompletedSuccessfully == false)
                Logger.Warn("Timeout: Not all package managers have finished initializing.");
        }
    }
}
