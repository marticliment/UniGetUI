using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UniGetUI.Core;
using UniGetUI.Core.Data;
using UniGetUI.Interface.Widgets;
using UniGetUI.Interface.Enums;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.ManagerClasses;
using UniGetUI.Core.Tools;

namespace UniGetUI.PackageEngine.Operations
{

    public class OperationCancelledEventArgs : EventArgs
    {
        public OperationStatus OldStatus;
        public OperationCancelledEventArgs(OperationStatus OldStatus)
        {
            this.OldStatus = OldStatus;
        }
    }

    public abstract class PackageOperation : AbstractOperation
    {

        public Package Package;
        protected InstallationOptions Options;
        public PackageOperation(Package package, InstallationOptions options, bool IgnoreParallelInstalls = false) : base(IgnoreParallelInstalls)
        {
            Package = package;
            Options = options;
            MainProcedure();
        }

        protected override async Task WaitForAvailability()
        {
            if (!IGNORE_PARALLEL_OPERATION_SETTINGS && (Settings.Get("AllowParallelInstalls") || Settings.Get($"AllowParallelInstallsForManager{Package.Manager.Name}")))
            {
                Logger.Debug("Parallel installs are allowed. Skipping queue check");
                return;
            }

            Package.SetTag(PackageTag.OnQueue);

            AddToQueue();
            int currentIndex = -2;
            int oldIndex = -1;
            while (currentIndex != 0)
            {
                if (Status == OperationStatus.Cancelled)
                {
                    Package.Tag = PackageTag.Default;
                    return; // If th operation has been cancelled
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

        public PackageOperation(Package package, bool IgnoreParallelInstalls = false) : this(package, new InstallationOptions(package), IgnoreParallelInstalls) { }
    }

    public class InstallPackageOperation : PackageOperation
    {

        public InstallPackageOperation(Package package, InstallationOptions options, bool IgnoreParallelInstalls = false) : base(package, options, IgnoreParallelInstalls) { }
        public InstallPackageOperation(Package package, bool IgnoreParallelInstalls = false) : base(package, IgnoreParallelInstalls) { }
        protected override Process BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Options.RunAsAdministrator || Settings.Get("AlwaysElevate" + Package.Manager.Name))
            {
                if (Settings.Get("DoCacheAdminRights") || Settings.Get("DoCacheAdminRightsForBatches"))
                {
                    Logger.Info("Caching admin rights for process id " + Process.GetCurrentProcess().Id);
                    Process p = new();
                    p.StartInfo.FileName = MainApp.Instance.GSudoPath;
                    p.StartInfo.Arguments = "cache on --pid " + Process.GetCurrentProcess().Id + " -d 1";
                    p.Start();
                    p.WaitForExit();
                }
                startInfo.FileName = MainApp.Instance.GSudoPath;
                startInfo.Arguments = $"\"{Package.Manager.Status.ExecutablePath}\" " + Package.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Package.Manager.GetInstallParameters(Package, Options));

            }
            else
            {
                startInfo.FileName = Package.Manager.Status.ExecutablePath;
                startInfo.Arguments = Package.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Package.Manager.GetInstallParameters(Package, Options));
            }
            Process process = new();
            process.StartInfo = startInfo;

            return process;
        }

        protected override string[] GenerateProcessLogHeader()
        {
            return new string[]
            {
                "Starting package install operation for package id=" + Package.Id + " with Manager name=" + Package.Manager.Name,
                "Given installation options are " + Options.ToString()
            };
        }

        protected override OperationVeredict GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Package.Manager.GetInstallOperationVeredict(Package, Options, ReturnCode, Output);
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = CoreTools.Translate("{package} installation failed", new Dictionary<string, object?>{ { "package", Package.Name } });

            Package.SetTag(PackageTag.Failed);

            if (!Settings.Get("DisableErrorNotifications") && !Settings.Get("DisableNotifications"))
            {
                try
                {
                    new ToastContentBuilder()
                        .AddArgument("action", "OpenUniGetUI")
                        .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                        .AddText(CoreTools.Translate("Installation failed"))
                        .AddText(CoreTools.Translate("{package} could not be installed", new Dictionary<string, object?> { { "package", Package.Name } })).Show();

                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to show toast notification");
                    Logger.Warn(ex);
                }
            }

            ContentDialogResult result = await MainApp.Instance.MainWindow.NavigationPage.ShowOperationFailedDialog(
                ProcessOutput,
                CoreTools.Translate("{package} installation failed", new Dictionary<string, object?> { { "package", Package.Name } }),
                CoreTools.Translate("{package} could not be installed", new Dictionary<string, object?> { { "package", Package.Name } })
            );

            if (result == ContentDialogResult.Primary)
                return AfterFinshAction.Retry;
            else
                return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = CoreTools.Translate("{package} was installed successfully", new Dictionary<string, object?>{ { "package", Package.Name } });

            Package.SetTag(PackageTag.AlreadyInstalled);
            MainApp.Instance.MainWindow.NavigationPage.InstalledPage.AddInstalledPackage(Package);

            if (!Settings.Get("DisableSuccessNotifications") && !Settings.Get("DisableNotifications"))

                try
                {
                    new ToastContentBuilder()
                    .AddArgument("action", "OpenUniGetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(CoreTools.Translate("Installation succeeded"))
                    .AddText(CoreTools.Translate("{package} was installed successfully", new Dictionary<string, object?>{ { "package", Package.Name } })).Show();

                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to show toast notification");
                    Logger.Warn(ex);
                }
            await Task.Delay(0);
            return AfterFinshAction.TimeoutClose;
        }

        protected override async void Initialize()
        {
            OperationTitle = CoreTools.Translate("{package} Installation", new Dictionary<string, object?>{ { "package", Package.Name } });
            IconSource = await Package.GetIconUrl();
        }
    }

    public class UpdatePackageOperation : PackageOperation
    {

        public UpdatePackageOperation(Package package, InstallationOptions options, bool IgnoreParallelInstalls = false) : base(package, options, IgnoreParallelInstalls) { }
        public UpdatePackageOperation(Package package, bool IgnoreParallelInstalls = false) : base(package, IgnoreParallelInstalls) { }
        protected override Process BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Options.RunAsAdministrator || Settings.Get("AlwaysElevate" + Package.Manager.Name))
            {
                if (Settings.Get("DoCacheAdminRights") || Settings.Get("DoCacheAdminRightsForBatches"))
                {
                    Logger.Info("Caching admin rights for process id " + Process.GetCurrentProcess().Id);
                    Process p = new();
                    p.StartInfo.FileName = MainApp.Instance.GSudoPath;
                    p.StartInfo.Arguments = "cache on --pid " + Process.GetCurrentProcess().Id + " -d 1";
                    p.Start();
                    p.WaitForExit();
                }
                startInfo.FileName = MainApp.Instance.GSudoPath;
                startInfo.Arguments = $"\"{Package.Manager.Status.ExecutablePath}\" " + Package.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Package.Manager.GetUpdateParameters(Package, Options));
            }
            else
            {
                startInfo.FileName = Package.Manager.Status.ExecutablePath;
                startInfo.Arguments = Package.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Package.Manager.GetUpdateParameters(Package, Options));
            }
            Process process = new();
            process.StartInfo = startInfo;

            return process;
        }

        protected override string[] GenerateProcessLogHeader()
        {
            return new string[]
            {
                "Starting package update operation for package id=" + Package.Id + " with Manager name=" + Package.Manager.Name,
                "Given installation options are " + Options.ToString()
            };
        }

        protected override OperationVeredict GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Package.Manager.GetUpdateOperationVeredict(Package, Options, ReturnCode, Output);
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = CoreTools.Translate("{package} update failed. Click here for more details.", new Dictionary<string, object?>{ { "package", Package.Name } });

            Package.SetTag(PackageTag.Failed);

            if (!Settings.Get("DisableErrorNotifications") && !Settings.Get("DisableNotifications"))
            {
                try
                {
                    new ToastContentBuilder()
                    .AddArgument("action", "OpenUniGetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(CoreTools.Translate("Update failed"))
                    .AddText(CoreTools.Translate("{package} could not be updated", new Dictionary<string, object?> { { "package", Package.Name } })).Show();

                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to show toast notification");
                    Logger.Warn(ex);
                }
            }

            ContentDialogResult result = await MainApp.Instance.MainWindow.NavigationPage.ShowOperationFailedDialog(
                ProcessOutput,
                CoreTools.Translate("{package} update failed", new Dictionary<string, object?> { { "package", Package.Name } }),
                CoreTools.Translate("{package} could not be updated", new Dictionary<string, object?> { { "package", Package.Name } })
            );

            if (result == ContentDialogResult.Primary)
                return AfterFinshAction.Retry;
            else
                return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = CoreTools.Translate("{package} was updated successfully", new Dictionary<string, object?>{ { "package", Package.Name } });

            Package.GetInstalledPackage()?.SetTag(PackageTag.Default);
            Package.GetAvailablePackage()?.SetTag(PackageTag.AlreadyInstalled);

            if(await Package.HasUpdatesIgnoredAsync() && await Package.GetIgnoredUpdatesVersionAsync() != "*")
                await Package.RemoveFromIgnoredUpdatesAsync();

            MainApp.Instance.MainWindow.NavigationPage.UpdatesPage.RemoveCorrespondingPackages(Package);

            if (!Settings.Get("DisableSuccessNotifications") && !Settings.Get("DisableNotifications"))
                try
                {
                    new ToastContentBuilder()
                .AddArgument("action", "OpenUniGetUI")
                .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                .AddText(CoreTools.Translate("Update succeeded"))
                .AddText(CoreTools.Translate("{package} was updated successfully", new Dictionary<string, object?>{ { "package", Package.Name } })).Show();

                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to show toast notification");
                    Logger.Warn(ex);
                }

            if (Package.Version == "Unknown")
                await Package.AddToIgnoredUpdatesAsync(Package.NewVersion);

            return AfterFinshAction.TimeoutClose;
        }

        protected override async void Initialize()
        {
            OperationTitle = CoreTools.Translate("{package} Update", new Dictionary<string, object?>{ { "package", Package.Name } });
            IconSource = await Package.GetIconUrl();
        }
    }

    public class UninstallPackageOperation : PackageOperation
    {

        public UninstallPackageOperation(Package package, InstallationOptions options, bool IgnoreParallelInstalls = false) : base(package, options, IgnoreParallelInstalls) { }
        public UninstallPackageOperation(Package package, bool IgnoreParallelInstalls = false) : base(package, IgnoreParallelInstalls) { }
        protected override Process BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Options.RunAsAdministrator || Settings.Get("AlwaysElevate" + Package.Manager.Name))
            {
                if (Settings.Get("DoCacheAdminRights") || Settings.Get("DoCacheAdminRightsForBatches"))
                {
                    Logger.Info("Caching admin rights for process id " + Process.GetCurrentProcess().Id);
                    Process p = new();
                    p.StartInfo.FileName = MainApp.Instance.GSudoPath;
                    p.StartInfo.Arguments = "cache on --pid " + Process.GetCurrentProcess().Id + " -d 1";
                    p.Start();
                    p.WaitForExit();
                }
                startInfo.FileName = MainApp.Instance.GSudoPath;
                startInfo.Arguments = $"\"{Package.Manager.Status.ExecutablePath}\" " + Package.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Package.Manager.GetUninstallParameters(Package, Options));
            }
            else
            {
                startInfo.FileName = Package.Manager.Status.ExecutablePath;
                startInfo.Arguments = Package.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Package.Manager.GetUninstallParameters(Package, Options));
            }
            Process process = new();
            process.StartInfo = startInfo;


            return process;
        }

        protected override string[] GenerateProcessLogHeader()
        {
            return new string[]
            {
                "Starting package uninstall operation for package id=" + Package.Id + " with Manager name=" + Package.Manager.Name,
                "Given installation options are " + Options.ToString()
            };
        }

        protected override OperationVeredict GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Package.Manager.GetUninstallOperationVeredict(Package, Options, ReturnCode, Output);
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = CoreTools.Translate("{package} uninstall failed", new Dictionary<string, object?>{ { "package", Package.Name } });

            Package.SetTag(PackageTag.Failed);

            if (!Settings.Get("DisableErrorNotifications") && !Settings.Get("DisableNotifications"))
                try
                {
                    new ToastContentBuilder()
                    .AddArgument("action", "OpenUniGetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(CoreTools.Translate("Uninstall failed"))
                    .AddText(CoreTools.Translate("{package} could not be uninstalled", new Dictionary<string, object?>{ { "package", Package.Name } })).Show();

                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to show toast notification");
                    Logger.Warn(ex);
                }

            ContentDialogResult result = await MainApp.Instance.MainWindow.NavigationPage.ShowOperationFailedDialog(
                ProcessOutput,
                CoreTools.Translate("{package} uninstall failed", new Dictionary<string, object?> { { "package", Package.Name } }),
                CoreTools.Translate("{package} could not be uninstalled", new Dictionary<string, object?> { { "package", Package.Name } })
            );

            if (result == ContentDialogResult.Primary)
                return AfterFinshAction.Retry;
            else
                return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = CoreTools.Translate("{package} was uninstalled successfully", new Dictionary<string, object?>{ { "package", Package.Name } });

            Package.GetAvailablePackage()?.SetTag(PackageTag.Default);
            MainApp.Instance.MainWindow.NavigationPage.UpdatesPage.RemoveCorrespondingPackages(Package);
            MainApp.Instance.MainWindow.NavigationPage.InstalledPage.RemoveCorrespondingPackages(Package);

            if (!Settings.Get("DisableSuccessNotifications") && !Settings.Get("DisableNotifications"))
                try
                {
                    new ToastContentBuilder()
                .AddArgument("action", "OpenUniGetUI")
                .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                .AddText(CoreTools.Translate("Uninstall succeeded"))
                .AddText(CoreTools.Translate("{package} was uninstalled successfully", new Dictionary<string, object?>{ { "package", Package.Name } })).Show();

                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to show toast notification");
                    Logger.Warn(ex);
                }
            await Task.Delay(0);
            return AfterFinshAction.TimeoutClose;
        }

        protected override async void Initialize()
        {
            OperationTitle = CoreTools.Translate("{package} Uninstall", new Dictionary<string, object?>{ { "package", Package.Name } });
            IconSource = await Package.GetIconUrl();
        }
    }
}
