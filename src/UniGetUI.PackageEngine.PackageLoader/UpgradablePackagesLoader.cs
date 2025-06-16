using System.Collections.Concurrent;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.PackageLoader
{
    public class UpgradablePackagesLoader : AbstractPackageLoader
    {
        private System.Timers.Timer? UpdatesTimer;
        public static UpgradablePackagesLoader Instance = null!;

        /// <summary>
        /// The collection of packages with updates ignored
        /// </summary>
        public ConcurrentDictionary<string, IPackage> IgnoredPackages = new();

        public UpgradablePackagesLoader(IReadOnlyList<IPackageManager> managers)
        : base(managers,
            identifier: "UPGRADABLE_PACKAGES",
            AllowMultiplePackageVersions: false,
            DisableReload: false,
            CheckedBydefault: !Settings.Get(Settings.K.DisableSelectingUpdatesByDefault),
            RequiresInternet: true)
        {
            Instance = this;
            FinishedLoading += (_, _) => StartAutoCheckTimeout();
        }

        protected override async Task<bool> IsPackageValid(IPackage package)
        {
            if (package.VersionString == package.NewVersionString) return false;
            if (await package.HasUpdatesIgnoredAsync(package.NewVersionString))
            {
                IgnoredPackages[package.Id] = package;
                return false;
            }
            if ((await InstallOptionsFactory.LoadApplicableAsync(package)).SkipMinorUpdates && package.IsUpdateMinor())
            {
                Logger.Info($"Ignoring package {package.Id} because it is a minor update ({package.VersionString} -> {package.NewVersionString}) and SkipMinorUpdates is set to true.");
                return false;
            }
            if (package.NewerVersionIsInstalled()) return false;
            return true;
        }

        protected override IReadOnlyList<IPackage> LoadPackagesFromManager(IPackageManager manager)
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
            if (!Settings.Get(Settings.K.DisableAutoCheckforUpdates))
            {
                long waitTime = 3600;
                try
                {
                    waitTime = long.Parse(Settings.GetValue(Settings.K.UpdatesCheckInterval));
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
