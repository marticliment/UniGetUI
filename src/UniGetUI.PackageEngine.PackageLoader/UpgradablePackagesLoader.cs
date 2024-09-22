using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.PackageLoader
{
    public class UpgradablePackagesLoader : AbstractPackageLoader
    {
        private System.Timers.Timer? UpdatesTimer;

        public UpgradablePackagesLoader(IEnumerable<IPackageManager> managers)
        : base(managers, "DISCOVERABLE_PACKAGES", AllowMultiplePackageVersions: false)
        {
            FinishedLoading += (_, _) => StartAutoCheckTimeout();
        }

        protected override async Task<bool> IsPackageValid(IPackage package)
        {
            if (await package.HasUpdatesIgnoredAsync(package.NewVersion))
            {
                return false;
            }

            if (package.IsUpgradable && package.NewerVersionIsInstalled())
            {
                return false;
            }

            return true;
        }

        protected override IEnumerable<IPackage> LoadPackagesFromManager(IPackageManager manager)
        {
            return manager.GetAvailableUpdates();
        }
        protected override Task WhenAddingPackage(IPackage package)
        {
            package.GetAvailablePackage()?.SetTag(PackageTag.IsUpgradable);
            package.GetInstalledPackage()?.SetTag(PackageTag.IsUpgradable);

            return Task.CompletedTask;
        }

        protected void StartAutoCheckTimeout()
        {
            if (!Settings.Get("DisableAutoCheckforUpdates"))
            {
                long waitTime = 3600;
                try
                {
                    waitTime = long.Parse(Settings.GetValue("UpdatesCheckInterval"));
                    Logger.Debug($"Starting check for updates wait interval with waitTime={waitTime}");
                }
                catch
                {
                    Logger.Debug("Invalid value for UpdatesCheckInterval, using default value of 3600 seconds");
                }

                if (UpdatesTimer is not null)
                {
                    UpdatesTimer.Stop();
                    UpdatesTimer.Dispose();
                }

                UpdatesTimer = new System.Timers.Timer(waitTime * 1000)
                {
                    Enabled = false,
                    AutoReset = false
                };
                UpdatesTimer.Elapsed += (s, e) => _ = ReloadPackages();
                UpdatesTimer.Start();
            }
        }
    }
}
