using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ModernWindow.Data;
using ModernWindow.PackageEngine.Classes;
using ModernWindow.Structures;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ModernWindow.PackageEngine.Operations
{
    public enum OperationVeredict
    {
        Succeeded,
        Failed,
        AutoRetry,
    }
    public enum OperationStatus
    {
        Pending,
        Running,
        Succeeded,
        Failed,
        Cancelled
    }

    public enum OperationType
    {
        Install,
        Update,
        Uninstall,
        None
    }

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

        protected Package Package;
        protected InstallationOptions Options;
        public PackageOperation(Package package, InstallationOptions options)
        {
            Package = package;
            Options = options;
            MainProcedure();
        }
        public PackageOperation(Package package) : this(package, new InstallationOptions(package)) { }
    }

    public class InstallPackageOperation : PackageOperation
    {

        public InstallPackageOperation(Package package, InstallationOptions options) : base(package, options) { }
        public InstallPackageOperation(Package package) : base(package) { }
        protected override Process BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Options.RunAsAdministrator || bindings.GetSettings("AlwaysElevate" + Package.Manager.Name))
            {
                if (bindings.GetSettings("DoCacheAdminRights") || bindings.GetSettings("DoCacheAdminRightsForBatches"))
                {
                    AppTools.Log("Caching admin rights for process id " + Process.GetCurrentProcess().Id);
                    Process p = new();
                    p.StartInfo.FileName = CoreData.GSudoPath;
                    p.StartInfo.Arguments = "cache on --pid " + Process.GetCurrentProcess().Id + " -d 1";
                    p.Start();
                    p.WaitForExit();
                }
                startInfo.FileName = CoreData.GSudoPath;
                startInfo.Arguments = "\"" + Package.Manager.Status.ExecutablePath + "\"" + Package.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Package.Manager.GetInstallParameters(Package, Options));

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
                "Starting package install operation for package id=" + Package.Id + "with Manager name=" + Package.Manager.Name,
                "Given installation options are " + Options.ToString()
            };
        }

        protected override OperationVeredict GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Package.Manager.GetInstallOperationVeredict(Package, Options, ReturnCode, Output);
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = bindings.Translate("{package} installation failed").Replace("{package}", Package.Name);
            if (!bindings.GetSettings("DisableErrorNotifications") && !bindings.GetSettings("DisableNotifications"))
                new ToastContentBuilder()
                    .AddArgument("action", "openWingetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(bindings.Translate("Installation failed"))
                    .AddText(bindings.Translate("{package} could not be installed").Replace("{package}", Package.Name)).Show();

            ContentDialog dialog = new();
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.XamlRoot = XamlRoot;
            dialog.Title = bindings.Translate("{package} installation failed").Replace("{package}", Package.Name);
            dialog.Content = bindings.Translate("{package} could not be installed").Replace("{package}", Package.Name) + ". " + bindings.Translate("Please click the More Details button or refer to the Operation History for further information about the issue.");
            dialog.PrimaryButtonText = bindings.Translate("Retry");
            dialog.SecondaryButtonText = bindings.Translate("More details");
            dialog.CloseButtonText = bindings.Translate("Close");
            dialog.DefaultButton = ContentDialogButton.Primary;

            dialog.SecondaryButtonClick += (s, e) =>
            {
                OpenLiveViewDialog();
            };

            ContentDialogResult result = await bindings.App.mainWindow.ShowDialog(dialog);
            while (result == ContentDialogResult.Secondary)
                result = await bindings.App.mainWindow.ShowDialog(dialog, HighPriority: true);

            if (result == ContentDialogResult.Primary)
                return AfterFinshAction.Retry;
            else
                return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = bindings.Translate("{package} was installed successfully").Replace("{package}", Package.Name);
            bindings.App.mainWindow.NavigationPage.InstalledPage.AddInstalledPackage(Package);
            bindings.App.mainWindow.NavigationPage.UpdatesPage.RemoveCorrespondingPackages(Package);
            if (!bindings.GetSettings("DisableSuccessNotifications") && !bindings.GetSettings("DisableNotifications"))
                new ToastContentBuilder()
                    .AddArgument("action", "openWingetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(bindings.Translate("Installation succeeded"))
                    .AddText(bindings.Translate("{package} was installed successfully").Replace("{package}", Package.Name)).Show();
            await Task.Delay(0);
            return AfterFinshAction.TimeoutClose;
        }

        protected override void Initialize()
        {
            OperationTitle = bindings.Translate("{package} Installation").Replace("{package}", Package.Name);
            IconSource = Package.GetIconUrl();
        }
    }

    public class UpdatePackageOperation : PackageOperation
    {

        public UpdatePackageOperation(Package package, InstallationOptions options) : base(package, options) { }
        public UpdatePackageOperation(Package package) : base(package) { }
        protected override Process BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Options.RunAsAdministrator || bindings.GetSettings("AlwaysElevate" + Package.Manager.Name))
            {
                if (bindings.GetSettings("DoCacheAdminRights") || bindings.GetSettings("DoCacheAdminRightsForBatches"))
                {
                    AppTools.Log("Caching admin rights for process id " + Process.GetCurrentProcess().Id);
                    Process p = new();
                    p.StartInfo.FileName = CoreData.GSudoPath;
                    p.StartInfo.Arguments = "cache on --pid " + Process.GetCurrentProcess().Id + " -d 1";
                    p.Start();
                    p.WaitForExit();
                }
                startInfo.FileName = CoreData.GSudoPath;
                startInfo.Arguments = "\"" + Package.Manager.Status.ExecutablePath + "\"" + Package.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Package.Manager.GetUpdateParameters(Package, Options));
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
                "Starting package update operation for package id=" + Package.Id + "with Manager name=" + Package.Manager.Name,
                "Given installation options are " + Options.ToString()
            };
        }

        protected override OperationVeredict GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Package.Manager.GetUpdateOperationVeredict(Package, Options, ReturnCode, Output);
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = bindings.Translate("{package} update failed. Click here for more details.").Replace("{package}", Package.Name);
            if (!bindings.GetSettings("DisableErrorNotifications") && !bindings.GetSettings("DisableNotifications"))
                new ToastContentBuilder()
                    .AddArgument("action", "openWingetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(bindings.Translate("Update failed"))
                    .AddText(bindings.Translate("{package} could not be updated").Replace("{package}", Package.Name)).Show();

            ContentDialog dialog = new();
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.XamlRoot = XamlRoot;
            dialog.Title = bindings.Translate("{package} update failed").Replace("{package}", Package.Name);
            dialog.Content = bindings.Translate("{package} could not be updated").Replace("{package}", Package.Name) + ". " + bindings.Translate("Please click the More Details button or refer to the Operation History for further information about the issue.");
            dialog.PrimaryButtonText = bindings.Translate("Retry");
            dialog.SecondaryButtonText = bindings.Translate("More details");
            dialog.CloseButtonText = bindings.Translate("Close");
            dialog.DefaultButton = ContentDialogButton.Primary;

            dialog.SecondaryButtonClick += (s, e) =>
            {
                OpenLiveViewDialog();
            };

            ContentDialogResult result = await bindings.App.mainWindow.ShowDialog(dialog);
            while (result == ContentDialogResult.Secondary)
                result = await bindings.App.mainWindow.ShowDialog(dialog, HighPriority: true);

            if (result == ContentDialogResult.Primary)
                return AfterFinshAction.Retry;
            else
                return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = bindings.Translate("{package} was updated successfully").Replace("{package}", Package.Name);
            bindings.App.mainWindow.NavigationPage.UpdatesPage.RemoveCorrespondingPackages(Package);
            if (!bindings.GetSettings("DisableSuccessNotifications") && !bindings.GetSettings("DisableNotifications"))
                new ToastContentBuilder()
                .AddArgument("action", "openWingetUI")
                .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                .AddText(bindings.Translate("Update succeeded"))
                .AddText(bindings.Translate("{package} was updated successfully").Replace("{package}", Package.Name)).Show();
            await Task.Delay(0);
            return AfterFinshAction.TimeoutClose;
        }

        protected override void Initialize()
        {
            OperationTitle = bindings.Translate("{package} Update").Replace("{package}", Package.Name);
            IconSource = Package.GetIconUrl();
        }
    }

    public class UninstallPackageOperation : PackageOperation
    {

        public UninstallPackageOperation(Package package, InstallationOptions options) : base(package, options) { }
        public UninstallPackageOperation(Package package) : base(package) { }
        protected override Process BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Options.RunAsAdministrator || bindings.GetSettings("AlwaysElevate" + Package.Manager.Name))
            {
                if (bindings.GetSettings("DoCacheAdminRights") || bindings.GetSettings("DoCacheAdminRightsForBatches"))
                {
                    AppTools.Log("Caching admin rights for process id " + Process.GetCurrentProcess().Id);
                    Process p = new();
                    p.StartInfo.FileName = CoreData.GSudoPath;
                    p.StartInfo.Arguments = "cache on --pid " + Process.GetCurrentProcess().Id + " -d 1";
                    p.Start();
                    p.WaitForExit();
                }
                startInfo.FileName = CoreData.GSudoPath;
                startInfo.Arguments = "\"" + Package.Manager.Status.ExecutablePath + "\"" + Package.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Package.Manager.GetInstallParameters(Package, Options));
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
                "Starting package uninstall operation for package id=" + Package.Id + "with Manager name=" + Package.Manager.Name,
                "Given installation options are " + Options.ToString()
            };
        }

        protected override OperationVeredict GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Package.Manager.GetUninstallOperationVeredict(Package, Options, ReturnCode, Output);
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = bindings.Translate("{package} uninstallation failed").Replace("{package}", Package.Name);

            if (!bindings.GetSettings("DisableErrorNotifications") && !bindings.GetSettings("DisableNotifications"))
                new ToastContentBuilder()
                    .AddArgument("action", "openWingetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(bindings.Translate("Uninstallation failed"))
                    .AddText(bindings.Translate("{package} could not be uninstalled").Replace("{package}", Package.Name)).Show();


            ContentDialog dialog = new();
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.XamlRoot = XamlRoot;
            dialog.Title = bindings.Translate("{package} uninstallation failed").Replace("{package}", Package.Name);
            dialog.Content = bindings.Translate("{package} could not be uninstalled").Replace("{package}", Package.Name) + ". " + bindings.Translate("Please click the More Details button or refer to the Operation History for further information about the issue.");
            dialog.PrimaryButtonText = bindings.Translate("Retry");
            dialog.SecondaryButtonText = bindings.Translate("More details");
            dialog.CloseButtonText = bindings.Translate("Close");
            dialog.DefaultButton = ContentDialogButton.Primary;

            dialog.SecondaryButtonClick += (s, e) =>
            {
                OpenLiveViewDialog();
            };

            ContentDialogResult result = await bindings.App.mainWindow.ShowDialog(dialog);
            while (result == ContentDialogResult.Secondary)
                result = await bindings.App.mainWindow.ShowDialog(dialog, HighPriority: true);

            if (result == ContentDialogResult.Primary)
                return AfterFinshAction.Retry;
            else
                return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = bindings.Translate("{package} was uninstalled successfully").Replace("{package}", Package.Name);
            bindings.App.mainWindow.NavigationPage.UpdatesPage.RemoveCorrespondingPackages(Package);
            bindings.App.mainWindow.NavigationPage.InstalledPage.RemoveCorrespondingPackages(Package);
            if (!bindings.GetSettings("DisableSuccessNotifications") && !bindings.GetSettings("DisableNotifications"))
                new ToastContentBuilder()
                .AddArgument("action", "openWingetUI")
                .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                .AddText(bindings.Translate("Uninstall succeeded"))
                .AddText(bindings.Translate("{package} was uninstalled successfully").Replace("{package}", Package.Name)).Show();
            await Task.Delay(0);
            return AfterFinshAction.TimeoutClose;
        }

        protected override void Initialize()
        {
            OperationTitle = bindings.Translate("{package} Uninstallation").Replace("{package}", Package.Name);
            IconSource = Package.GetIconUrl();
        }
    }
}
