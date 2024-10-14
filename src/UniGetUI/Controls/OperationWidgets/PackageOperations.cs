using System.Diagnostics;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.Pages.DialogPages;

namespace UniGetUI.PackageEngine.Operations
{

    public class OperationCanceledEventArgs : EventArgs
    {
        public OperationStatus OldStatus;
        public OperationCanceledEventArgs(OperationStatus OldStatus)
        {
            this.OldStatus = OldStatus;
        }
    }

    public abstract class PackageOperation : AbstractOperation
    {
        protected readonly IPackage Package;
        protected readonly IInstallationOptions Options;
        protected readonly OperationType Role;

        public PackageOperation(
            IPackage package,
            IInstallationOptions options,
            OperationType role,
            bool IgnoreParallelInstalls = false)
            : base(IgnoreParallelInstalls)
        {
            Package = package;
            Options = options;
            Role = role;
            MainProcedure();
        }

        public PackageOperation(
            IPackage package,
            OperationType role,
            bool IgnoreParallelInstalls = false)
            : this(package, InstallationOptions.FromPackage(package), role, IgnoreParallelInstalls)
        {
        }

        protected sealed override async Task<ProcessStartInfo> BuildProcessInstance(ProcessStartInfo startInfo)
        {
            string operation_args = string.Join(" ", Package.Manager.GetOperationParameters(Package, Options, Role));

            if (Package.OverridenOptions.RunAsAdministrator == true || Options.RunAsAdministrator ||
                Settings.Get("AlwaysElevate" + Package.Manager.Name))
            {
                if (Settings.Get("DoCacheAdminRights") || Settings.Get("DoCacheAdminRightsForBatches"))
                {
                    await CoreTools.CacheUACForCurrentProcess();
                }

                startInfo.FileName = CoreData.GSudoPath;
                startInfo.Arguments =
                    $"\"{Package.Manager.Status.ExecutablePath}\" {Package.Manager.Properties.ExecutableCallArgs} {operation_args}";
            }
            else
            {
                startInfo.FileName = Package.Manager.Status.ExecutablePath;
                startInfo.Arguments = $"{Package.Manager.Properties.ExecutableCallArgs} {operation_args}";
            }

            return startInfo;
        }

        protected override Task HandleCancelation()
        {
            Package.SetTag(PackageTag.Default);
            return Task.CompletedTask;
        }

        protected sealed override Task<OperationVeredict> GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Task.FromResult(Package.Manager.GetOperationResult(Package, Role, Output, ReturnCode));
        }

        protected override async Task WaitForAvailability()
        {
            if (!IGNORE_PARALLEL_OPERATION_SETTINGS &&
                (Settings.Get("AllowParallelInstalls")
                 || Settings.Get($"AllowParallelInstallsForManager{Package.Manager.Name}")))
            {
                Logger.Debug("Parallel installs are allowed. Skipping queue check");
                Package.SetTag(PackageTag.BeingProcessed);
                return;
            }

            Package.SetTag(PackageTag.OnQueue);

            AddToQueue();
            int currentIndex = -2;
            int oldIndex = -1;
            while (currentIndex != 0)
            {
                if (Status == OperationStatus.Canceled)
                {
                    Package.Tag = PackageTag.Default;
                    return; // If the operation has been cancelled
                }

                currentIndex = MainApp.Instance.OperationQueue.IndexOf(this);
                if (currentIndex != oldIndex)
                {
                    LineInfoText = CoreTools.Translate("Operation on queue (position {0})...", currentIndex);
                    oldIndex = currentIndex;
                }

                await Task.Delay(100);
            }

            Package.SetTag(PackageTag.BeingProcessed);

        }

        protected void ShowErrorNotification(string title, string body)
        {
            if (Settings.AreErrorNotificationsDisabled())
                return;

            try
            {
                AppNotificationManager.Default.RemoveByTagAsync(Package.Id);
                AppNotificationBuilder builder = new AppNotificationBuilder()
                    .SetScenario(AppNotificationScenario.Urgent)
                    .SetTag(Package.Id)
                    .AddText(title)
                    .AddText(body)
                    .AddArgument("action", NotificationArguments.Show);
                AppNotification notification = builder.BuildNotification();
                notification.ExpiresOnReboot = true;
                AppNotificationManager.Default.Show(notification);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to show toast notification");
                Logger.Error(ex);
            }
        }

        protected void ShowSuccessNotification(string title, string body)
        {
            if (Settings.AreSuccessNotificationsDisabled())
                return;

            try
            {
                AppNotificationManager.Default.RemoveByTagAsync(Package.Id);
                AppNotificationBuilder builder = new AppNotificationBuilder()
                    .SetScenario(AppNotificationScenario.Default)
                    .SetTag(Package.Id)
                    .AddText(title)
                    .AddText(body)
                    .AddArgument("action", NotificationArguments.Show);
                AppNotification notification = builder.BuildNotification();
                notification.ExpiresOnReboot = true;
                AppNotificationManager.Default.Show(notification);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to show toast notification");
                Logger.Error(ex);
            }
        }

        protected string INSTALLING_STRING = "THIS NEEDS TO BE REDEFINED ON THE CONSTRUCTOR";


        protected override void PostProcessStartAction()
        {
            if (Settings.AreProgressNotificationsDisabled())
                return;

            try
            {
                AppNotificationManager.Default.RemoveByTagAsync(Package.Id + "progress");
                AppNotificationBuilder builder = new AppNotificationBuilder()
                    .SetScenario(AppNotificationScenario.Default)
                    .SetTag(Package.Id + "progress")
                    .AddProgressBar(new AppNotificationProgressBar()
                        .SetStatus(CoreTools.Translate("Please wait..."))
                        .SetValueStringOverride("\u2003")
                        .SetTitle(INSTALLING_STRING)
                        .SetValue(1.0))
                    .AddArgument("action", NotificationArguments.Show);
                AppNotification notification = builder.BuildNotification();
                notification.ExpiresOnReboot = true;
                notification.SuppressDisplay = true;
                AppNotificationManager.Default.Show(notification);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to show toast notification");
                Logger.Error(ex);
            }
        }

        protected override void PostProcessEndAction()
        {
            AppNotificationManager.Default.RemoveByTagAsync(Package.Id + "progress");
        }
    }

    public class InstallPackageOperation : PackageOperation
    {

        public InstallPackageOperation(
            IPackage package,
            IInstallationOptions options,
            bool IgnoreParallelInstalls = false)
            : base(package, options, OperationType.Install, IgnoreParallelInstalls)
        {
            INSTALLING_STRING = CoreTools.Translate("{0} is being installed", package.Name);
        }

        public InstallPackageOperation(
            IPackage package,
            bool IgnoreParallelInstalls = false)
            : base(package, OperationType.Install, IgnoreParallelInstalls)
        {
            INSTALLING_STRING = CoreTools.Translate("{0} is being installed", package.Name);
        }

        protected override string[] GenerateProcessLogHeader()
        {
            return
            [
                "Starting package install operation for package id=" + Package.Id + " with Manager name=" + Package.Manager.Name,
                "Given installation options are " + Options.ToString(),
            ];
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = CoreTools.Translate("{package} installation failed", new Dictionary<string, object?> { { "package", Package.Name } });
            Package.SetTag(PackageTag.Failed);

            ShowErrorNotification(
                CoreTools.Translate("Installation failed"),
                CoreTools.Translate("{package} could not be installed",
                    new Dictionary<string, object?> { { "package", Package.Name } })
            );

            ContentDialogResult result = await DialogHelper.ShowOperationFailed(
                ProcessOutput,
                CoreTools.Translate("{package} installation failed", new Dictionary<string, object?> { { "package", Package.Name } }),
                CoreTools.Translate("{package} could not be installed", new Dictionary<string, object?> { { "package", Package.Name } })
            );

            return result == ContentDialogResult.Primary? AfterFinshAction.Retry: AfterFinshAction.ManualClose;

        }

        protected override Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = CoreTools.Translate("{package} was installed successfully", new Dictionary<string, object?> { { "package", Package.Name } });
            Package.SetTag(PackageTag.AlreadyInstalled);
            PEInterface.InstalledPackagesLoader.AddForeign(Package);

            ShowSuccessNotification(
                CoreTools.Translate("Installation succeeded"),
                CoreTools.Translate("{package} was installed successfully",
                    new Dictionary<string, object?> { { "package", Package.Name } })
            );

            return Task.FromResult(AfterFinshAction.TimeoutClose);
        }

        protected override async Task Initialize()
        {
            OperationTitle = CoreTools.Translate("{package} Installation", new Dictionary<string, object?> { { "package", Package.Name } });
            IconSource = await Task.Run(Package.GetIconUrl);
        }
    }

    public class UpdatePackageOperation : PackageOperation
    {

        public UpdatePackageOperation(
            IPackage package,
            IInstallationOptions options,
            bool IgnoreParallelInstalls = false)
            : base(package, options, OperationType.Update, IgnoreParallelInstalls)
        {
            INSTALLING_STRING = CoreTools.Translate("{0} is being updated to version {1}", package.Name, package.NewVersion);
        }

        public UpdatePackageOperation(
            IPackage package,
            bool IgnoreParallelInstalls = false)
            : base(package, OperationType.Update, IgnoreParallelInstalls)
        {
            INSTALLING_STRING = CoreTools.Translate("{0} is being updated to version {1}", package.Name, package.NewVersion);
        }


        protected override string[] GenerateProcessLogHeader()
        {
            return
            [
                "Starting package update operation for package id=" + Package.Id + " with Manager name=" + Package.Manager.Name,
                "Given installation options are " + Options.ToString(),
            ];
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = CoreTools.Translate("{package} update failed. Click here for more details.", new Dictionary<string, object?> { { "package", Package.Name } });
            Package.SetTag(PackageTag.Failed);

            ShowErrorNotification(
                CoreTools.Translate("Update failed"),
                CoreTools.Translate("{package} could not be updated",
                    new Dictionary<string, object?> { { "package", Package.Name } })
            );

            ContentDialogResult result = await DialogHelper.ShowOperationFailed(
                ProcessOutput,
                CoreTools.Translate("{package} update failed", new Dictionary<string, object?> { { "package", Package.Name } }),
                CoreTools.Translate("{package} could not be updated", new Dictionary<string, object?> { { "package", Package.Name } })
            );

            return result == ContentDialogResult.Primary ? AfterFinshAction.Retry : AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = CoreTools.Translate("{package} was updated successfully", new Dictionary<string, object?> { { "package", Package.Name } });
            Package.SetTag(PackageTag.Default);
            Package.GetInstalledPackage()?.SetTag(PackageTag.Default);
            Package.GetAvailablePackage()?.SetTag(PackageTag.AlreadyInstalled);

            if (await Package.HasUpdatesIgnoredAsync() && await Package.GetIgnoredUpdatesVersionAsync() != "*")
            {
                await Package.RemoveFromIgnoredUpdatesAsync();
            }
            PEInterface.UpgradablePackagesLoader.Remove(Package);

            ShowSuccessNotification(
                CoreTools.Translate("Update succeeded"),
                CoreTools.Translate("{package} was updated successfully",
                    new Dictionary<string, object?> { { "package", Package.Name } })
            );

            if (Package.Version == "Unknown")
            {
                await Package.AddToIgnoredUpdatesAsync(Package.NewVersion);
            }

            return AfterFinshAction.TimeoutClose;
        }

        protected override async Task Initialize()
        {
            OperationTitle = CoreTools.Translate("{package} Update", new Dictionary<string, object?> { { "package", Package.Name } });
            IconSource = await Task.Run(Package.GetIconUrl);
        }
    }

    public class UninstallPackageOperation : PackageOperation
    {

        public UninstallPackageOperation(
            IPackage package,
            IInstallationOptions options,
            bool IgnoreParallelInstalls = false)
            : base(package, options, OperationType.Uninstall, IgnoreParallelInstalls)
        {
            INSTALLING_STRING = CoreTools.Translate("{0} is being uninstalled", package.Name);
        }
        public UninstallPackageOperation(
            IPackage package,
            bool IgnoreParallelInstalls = false)
            : base(package, OperationType.Uninstall, IgnoreParallelInstalls)
        {
            INSTALLING_STRING = CoreTools.Translate("{0} is being uninstalled", package.Name);
        }

        protected override string[] GenerateProcessLogHeader()
        {
            return
            [
                "Starting package uninstall operation for package id=" + Package.Id + " with Manager name=" + Package.Manager.Name,
                "Given installation options are " + Options.ToString(),
            ];
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = CoreTools.Translate("{package} uninstall failed", new Dictionary<string, object?> { { "package", Package.Name } });
            Package.SetTag(PackageTag.Failed);

            ShowErrorNotification(
                CoreTools.Translate("Uninstall failed"),
                CoreTools.Translate("{package} could not be uninstalled",
                    new Dictionary<string, object?> { { "package", Package.Name } })
            );

            ContentDialogResult result = await DialogHelper.ShowOperationFailed(
                ProcessOutput,
                CoreTools.Translate("{package} uninstall failed", new Dictionary<string, object?> { { "package", Package.Name } }),
                CoreTools.Translate("{package} could not be uninstalled", new Dictionary<string, object?> { { "package", Package.Name } })
            );

            return result == ContentDialogResult.Primary ? AfterFinshAction.Retry : AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = CoreTools.Translate("{package} was uninstalled successfully", new Dictionary<string, object?> { { "package", Package.Name } });
            Package.SetTag(PackageTag.Default);
            Package.GetAvailablePackage()?.SetTag(PackageTag.Default);
            PEInterface.UpgradablePackagesLoader.Remove(Package);
            PEInterface.InstalledPackagesLoader.Remove(Package);

            ShowSuccessNotification(
                CoreTools.Translate("Uninstall succeeded"),
                CoreTools.Translate("{package} was uninstalled successfully",
                    new Dictionary<string, object?> { { "package", Package.Name } })
            );

            return AfterFinshAction.TimeoutClose;
        }

        protected override async Task Initialize()
        {
            OperationTitle = CoreTools.Translate("{package} Uninstall", new Dictionary<string, object?> { { "package", Package.Name } });
            IconSource = await Task.Run(Package.GetIconUrl);
        }
    }
}
