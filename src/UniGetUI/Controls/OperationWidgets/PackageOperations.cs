using System.Diagnostics;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using UniGetUI.Core.Classes;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Packages.Classes;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageOperations;
using UniGetUI.Pages.DialogPages;

namespace UniGetUI.PackageEngine.Operations
{
    public abstract class PackageOperation : AbstractProcessOperation
    {
        protected abstract void GenerateProcessLogHeader();
        protected List<string> DesktopShortcutsBeforeStart = [];

        protected readonly IPackage Package;
        protected readonly IInstallationOptions Options;
        protected readonly OperationType Role;
        protected string ONGOING_PROGRESS_STRING = null!;

        protected abstract Task HandleSuccess();
        protected abstract Task HandleFailure();
        protected abstract void Initialize();

        public PackageOperation(
            IPackage package,
            IInstallationOptions options,
            OperationType role,
            bool IgnoreParallelInstalls = false)
            : base(!IgnoreParallelInstalls)
        {
            Package = package;
            Options = options;
            Role = role;
            if (ONGOING_PROGRESS_STRING is null)
            {
                throw new NullReferenceException("ONGOING_PROGRESS_STRING must be set to a non-null value in the Initialize method");
            }
            Initialize();
            Line(ONGOING_PROGRESS_STRING, LineType.Progress);
            GenerateProcessLogHeader();

            Enqueued += (_, _) =>
            {
                Package.SetTag(PackageTag.OnQueue);
                if ((Settings.Get("AllowParallelInstalls")
                     || Settings.Get($"AllowParallelInstallsForManager{Package.Manager.Name}")))
                {
                    Logger.Debug("Parallel installs are allowed. Skipping queue check");
                    SkipQueue();
                }
            };
            CancelRequested += (_, _) => Package.SetTag(PackageTag.Default);
            OperationStarting += (_, _) => CreateProgressToast();
            OperationFinished += (_, _) => RemoveProgressToast();
            OperationSucceeded += (_, _) => HandleSuccess();
            OperationFailed += (_, _) => HandleFailure();
        }

        public PackageOperation(
            IPackage package,
            OperationType role,
            bool IgnoreParallelInstalls = false)
            : this(package, InstallationOptions.FromPackage(package), role, IgnoreParallelInstalls) { }

        protected sealed override async Task PrepareProcessStartInfo()
        {
            Package.SetTag(PackageTag.OnQueue);
            string operation_args = string.Join(" ", Package.Manager.OperationHelper.GetParameters(Package, Options, Role));

            if (Package.OverridenOptions.RunAsAdministrator == true
                || Options.RunAsAdministrator
                || (Settings.Get("AlwaysElevate" + Package.Manager.Name)
                    && !Package.OverridenOptions.RunAsAdministrator is false)
                )
            {
                if (Settings.Get("DoCacheAdminRights") || Settings.Get("DoCacheAdminRightsForBatches"))
                {
                    await CoreTools.CacheUACForCurrentProcess();
                }

                process.StartInfo.FileName = CoreData.GSudoPath;
                process.StartInfo.Arguments =
                    $"\"{Package.Manager.Status.ExecutablePath}\" {Package.Manager.Properties.ExecutableCallArgs} {operation_args}";
            }
            else
            {
                process.StartInfo.FileName = Package.Manager.Status.ExecutablePath;
                process.StartInfo.Arguments = $"{Package.Manager.Properties.ExecutableCallArgs} {operation_args}";
            }
        }

        protected sealed override Task<OperationVeredict> GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Task.FromResult(Package.Manager.OperationHelper.GetResult(Package, Role, Output, ReturnCode));
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

        private void CreateProgressToast()
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
                        .SetTitle(ONGOING_PROGRESS_STRING)
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

        private void RemoveProgressToast()
        {
            AppNotificationManager.Default.RemoveByTagAsync(Package.Id + "progress");
        }

        public override Task<Uri> GetOperationIcon()
        {
            return TaskRecycler<Uri>.RunOrAttachAsync(Package.GetIconUrl);
        }
    }

    public class InstallPackageOperation : PackageOperation
    {

        public InstallPackageOperation(
            IPackage package,
            IInstallationOptions options,
            bool IgnoreParallelInstalls = false)
            : base(package, options, OperationType.Install, IgnoreParallelInstalls)
        { }

        public InstallPackageOperation(
            IPackage package,
            bool IgnoreParallelInstalls = false)
            : base(package, OperationType.Install, IgnoreParallelInstalls)
        { }

        protected override void GenerateProcessLogHeader()
        {
            Line("Starting package install operation for package id=" + Package.Id + " with Manager name=" +
                Package.Manager.Name, LineType.Debug);
            Line("Given installation options are " + Options.ToString(), LineType.Debug);
        }

        protected override Task HandleFailure()
        {
            Line(CoreTools.Translate("{package} installation failed", new Dictionary<string, object?> { { "package", Package.Name } }), LineType.Progress);
            Package.SetTag(PackageTag.Failed);

            ShowErrorNotification(
                CoreTools.Translate("Installation failed"),
                CoreTools.Translate("{package} could not be installed",
                    new Dictionary<string, object?> { { "package", Package.Name } })
            );

            return Task.CompletedTask;
            /*ContentDialogResult result = await DialogHelper.ShowOperationFailed(
                ProcessOutput,
                CoreTools.Translate("{package} installation failed", new Dictionary<string, object?> { { "package", Package.Name } }),
                CoreTools.Translate("{package} could not be installed", new Dictionary<string, object?> { { "package", Package.Name } })
            );*/

        }

        protected override Task HandleSuccess()
        {
            Line(CoreTools.Translate("{package} was installed successfully", new Dictionary<string, object?> { { "package", Package.Name } }), LineType.Progress);
            Package.SetTag(PackageTag.AlreadyInstalled);
            PEInterface.InstalledPackagesLoader.AddForeign(Package);

            ShowSuccessNotification(
                CoreTools.Translate("Installation succeeded"),
                CoreTools.Translate("{package} was installed successfully",
                    new Dictionary<string, object?> { { "package", Package.Name } })
            );

            if (Settings.Get("AskToDeleteNewDesktopShortcuts"))
            {
                DesktopShortcutsDatabase.TryRemoveNewShortcuts(DesktopShortcutsBeforeStart);
            }
            return Task.CompletedTask;
        }



        protected override void Initialize()
        {
            ONGOING_PROGRESS_STRING = CoreTools.Translate("{0} is being installed", Package.Name);
            if (Settings.Get("AskToDeleteNewDesktopShortcuts"))
            {
                DesktopShortcutsBeforeStart = DesktopShortcutsDatabase.GetShortcuts();
            }
        }

        public override string GetOperationTitle()
        {
            return CoreTools.Translate("{package} Installation", new Dictionary<string, object?> { { "package", Package.Name } });
        }
    }

    public class UpdatePackageOperation : PackageOperation
    {

        public UpdatePackageOperation(
            IPackage package,
            IInstallationOptions options,
            bool IgnoreParallelInstalls = false)
            : base(package, options, OperationType.Update, IgnoreParallelInstalls)
        { }

        public UpdatePackageOperation(
            IPackage package,
            bool IgnoreParallelInstalls = false)
            : base(package, OperationType.Update, IgnoreParallelInstalls)
        { }

        protected override void GenerateProcessLogHeader()
        {
            Line(
                "Starting package update operation for package id=" + Package.Id + " with Manager name=" +
                Package.Manager.Name, LineType.Debug);
            Line("Given installation options are " + Options.ToString(), LineType.Debug);
        }

        protected override Task HandleFailure()
        {
            Line(CoreTools.Translate("{package} update failed. Click here for more details.", new Dictionary<string, object?> { { "package", Package.Name } }), LineType.Progress);
            Package.SetTag(PackageTag.Failed);

            ShowErrorNotification(
                CoreTools.Translate("Update failed"),
                CoreTools.Translate("{package} could not be updated",
                    new Dictionary<string, object?> { { "package", Package.Name } })
            );

            return Task.CompletedTask;

            /*ContentDialogResult result = await DialogHelper.ShowOperationFailed(
                ProcessOutput,
                CoreTools.Translate("{package} update failed", new Dictionary<string, object?> { { "package", Package.Name } }),
                CoreTools.Translate("{package} could not be updated", new Dictionary<string, object?> { { "package", Package.Name } })
            );*/
        }

        protected override async Task HandleSuccess()
        {
            Line(CoreTools.Translate("{package} was updated successfully", new Dictionary<string, object?> { { "package", Package.Name } }), LineType.Progress);
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

            if (Settings.Get("AskToDeleteNewDesktopShortcuts"))
            {
                DesktopShortcutsDatabase.TryRemoveNewShortcuts(DesktopShortcutsBeforeStart);
            }
        }

        protected override void Initialize()
        {
            ONGOING_PROGRESS_STRING = CoreTools.Translate("{0} is being updated to version {1}", Package.Name, Package.NewVersion);
            if (Settings.Get("AskToDeleteNewDesktopShortcuts"))
            {
                DesktopShortcutsBeforeStart = DesktopShortcutsDatabase.GetShortcuts();
            }
        }

        public override string GetOperationTitle()
        {
            return CoreTools.Translate("{package} Update", new Dictionary<string, object?> { { "package", Package.Name } });
        }
    }

    public class UninstallPackageOperation : PackageOperation
    {

        public UninstallPackageOperation(
            IPackage package,
            IInstallationOptions options,
            bool IgnoreParallelInstalls = false)
            : base(package, options, OperationType.Uninstall, IgnoreParallelInstalls)
        { }

        public UninstallPackageOperation(
            IPackage package,
            bool IgnoreParallelInstalls = false)
            : base(package, OperationType.Uninstall, IgnoreParallelInstalls)
        { }

        protected override void GenerateProcessLogHeader()
        {
            Line(
                "Starting package uninstall operation for package id=" + Package.Id + " with Manager name=" +
                Package.Manager.Name, LineType.Debug);
            Line("Given installation options are " + Options.ToString(), LineType.Debug);
        }

        protected override Task HandleFailure()
        {
            Line(CoreTools.Translate("{package} uninstall failed", new Dictionary<string, object?> { { "package", Package.Name } }), LineType.Debug);
            Package.SetTag(PackageTag.Failed);

            ShowErrorNotification(
                CoreTools.Translate("Uninstall failed"),
                CoreTools.Translate("{package} could not be uninstalled",
                    new Dictionary<string, object?> { { "package", Package.Name } })
            );

            /*ContentDialogResult result = await DialogHelper.ShowOperationFailed(
                ProcessOutput,
                CoreTools.Translate("{package} uninstall failed", new Dictionary<string, object?> { { "package", Package.Name } }),
                CoreTools.Translate("{package} could not be uninstalled", new Dictionary<string, object?> { { "package", Package.Name } })
            );*/
            return Task.CompletedTask;
        }

        protected override Task HandleSuccess()
        {
            Line(CoreTools.Translate("{package} was uninstalled successfully", new Dictionary<string, object?> { { "package", Package.Name } }), LineType.Debug);
            Package.SetTag(PackageTag.Default);
            Package.GetAvailablePackage()?.SetTag(PackageTag.Default);
            PEInterface.UpgradablePackagesLoader.Remove(Package);
            PEInterface.InstalledPackagesLoader.Remove(Package);

            ShowSuccessNotification(
                CoreTools.Translate("Uninstall succeeded"),
                CoreTools.Translate("{package} was uninstalled successfully",
                    new Dictionary<string, object?> { { "package", Package.Name } })
            );

            return Task.CompletedTask;
        }

        protected override void Initialize()
        {
            ONGOING_PROGRESS_STRING = CoreTools.Translate("{0} is being uninstalled", Package.Name);
        }

        public override string GetOperationTitle()
        {
            return CoreTools.Translate("{package} Uninstall", new Dictionary<string, object?> { { "package", Package.Name } });
        }
    }
}
