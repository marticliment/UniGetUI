using UniGetUI.Core.Logging;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.PackageLoader
{
    public class InstalledPackagesLoader : AbstractPackageLoader
    {
        public InstalledPackagesLoader(IEnumerable<IPackageManager> managers)
        : base(managers, "INSTALLED_PACKAGES", AllowMultiplePackageVersions: true)
        {
        }

#pragma warning disable
        protected override async Task<bool> IsPackageValid(IPackage package)
        {
            return true;
        }
#pragma warning restore

        protected override Task<IPackage[]> LoadPackagesFromManager(IPackageManager manager)
        {
            return manager.GetInstalledPackages();
        }

        protected override async Task WhenAddingPackage(IPackage package)
        {
            if (await package.HasUpdatesIgnoredAsync(version: "*"))
            {
                package.Tag = PackageTag.Pinned;
            }
            else if (package.GetUpgradablePackage() != null)
            {
                package.Tag = PackageTag.IsUpgradable;
            }

            package.GetAvailablePackage()?.SetTag(PackageTag.AlreadyInstalled);
        }

        public async Task ReloadPackagesSilently()
        {
            IsLoading = true;
            InvokeStartedLoadingEvent();

            List<Task<IPackage[]>> tasks = new();

            foreach (IPackageManager manager in Managers)
            {
                if (manager.IsEnabled() && manager.Status.Found)
                {
                    Task<IPackage[]> task = LoadPackagesFromManager(manager);
                    tasks.Add(task);
                }
            }

            while (tasks.Count > 0)
            {
                foreach (Task<IPackage[]> task in tasks.ToArray())
                {
                    if (!task.IsCompleted)
                    {
                        await Task.Delay(100);
                    }

                    if (task.IsCompleted)
                    {
                        if (task.IsCompletedSuccessfully)
                        {
                            foreach (IPackage package in task.Result)
                            {
                                if (!Contains(package))
                                {
                                    Logger.ImportantInfo($"Adding missing package {package.Id} to installed packages list");
                                    AddPackage(package);
                                    await WhenAddingPackage(package);
                                }
                            }
                            InvokePackagesChangedEvent();
                        }
                        tasks.Remove(task);
                    }
                }
            }

            InvokeFinishedLoadingEvent();
            IsLoading = false;

        }
    }
}
