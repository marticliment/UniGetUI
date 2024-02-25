using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ModernWindow.Core.Data;
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

        protected override async Task WaitForAvailability()
        {
            if(Tools.GetSettings("AllowParallelInstalls") || Tools.GetSettings("AllowParallelInstallsForManager" + Package.Manager.Name))
            {
                AppTools.Log("Parallel installs are allowed. Skipping queue check");
                return;
            }

            AddToQueue();
            int currentIndex = -2;
            int oldIndex = -1;
            while (currentIndex != 0)
            {
                if (Status == OperationStatus.Cancelled)
                    return; // If th operation has been cancelled

                currentIndex = Tools.OperationQueue.IndexOf(this);
                if (currentIndex != oldIndex)
                {
                    LineInfoText = Tools.Translate("Operation on queue (position {0})...").Replace("{0}", currentIndex.ToString());
                    oldIndex = currentIndex;
                }
                await Task.Delay(100);
            }
        }

        public PackageOperation(Package package) : this(package, new InstallationOptions(package)) { }
    }

    public class InstallPackageOperation : PackageOperation
    {

        public InstallPackageOperation(Package package, InstallationOptions options) : base(package, options) { }
        public InstallPackageOperation(Package package) : base(package) { }
        protected override Process BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Options.RunAsAdministrator || Tools.GetSettings("AlwaysElevate" + Package.Manager.Name))
            {
                if (Tools.GetSettings("DoCacheAdminRights") || Tools.GetSettings("DoCacheAdminRightsForBatches"))
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
            LineInfoText = Tools.Translate("{package} installation failed").Replace("{package}", Package.Name);
            if (!Tools.GetSettings("DisableErrorNotifications") && !Tools.GetSettings("DisableNotifications"))
                try { 
                new ToastContentBuilder()
                    .AddArgument("action", "openWingetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(Tools.Translate("Installation failed"))
                    .AddText(Tools.Translate("{package} could not be installed").Replace("{package}", Package.Name)).Show();

                }
                catch (Exception ex)
                {
                    AppTools.Log(ex);
                }
            ContentDialog dialog = new();
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.XamlRoot = XamlRoot;
            dialog.Title = Tools.Translate("{package} installation failed").Replace("{package}", Package.Name);
            dialog.Content = Tools.Translate("{package} could not be installed").Replace("{package}", Package.Name) + ". " + Tools.Translate("Please click the More Details button or refer to the Operation History for further information about the issue.");
            dialog.PrimaryButtonText = Tools.Translate("Retry");
            dialog.SecondaryButtonText = Tools.Translate("More details");
            dialog.CloseButtonText = Tools.Translate("Close");
            dialog.DefaultButton = ContentDialogButton.Primary;

            dialog.SecondaryButtonClick += (s, e) =>
            {
                OpenLiveViewDialog();
            };

            ContentDialogResult result = await Tools.App.mainWindow.ShowDialog(dialog);
            while (result == ContentDialogResult.Secondary)
                result = await Tools.App.mainWindow.ShowDialog(dialog, HighPriority: true);

            if (result == ContentDialogResult.Primary)
                return AfterFinshAction.Retry;
            else
                return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = Tools.Translate("{package} was installed successfully").Replace("{package}", Package.Name);
            Tools.App.mainWindow.NavigationPage.InstalledPage.AddInstalledPackage(Package);
            Tools.App.mainWindow.NavigationPage.UpdatesPage.RemoveCorrespondingPackages(Package);
            if (!Tools.GetSettings("DisableSuccessNotifications") && !Tools.GetSettings("DisableNotifications"))
                
                try{
                    new ToastContentBuilder()
                    .AddArgument("action", "openWingetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(Tools.Translate("Installation succeeded"))
                    .AddText(Tools.Translate("{package} was installed successfully").Replace("{package}", Package.Name)).Show();

                }
                catch (Exception ex)
                {
                    AppTools.Log(ex);
                }
            await Task.Delay(0);
            return AfterFinshAction.TimeoutClose;
        }

        protected override void Initialize()
        {
            OperationTitle = Tools.Translate("{package} Installation").Replace("{package}", Package.Name);
            IconSource = Package.GetIconUrl();
        }
    }

    public class UpdatePackageOperation : PackageOperation
    {

        public UpdatePackageOperation(Package package, InstallationOptions options) : base(package, options) { }
        public UpdatePackageOperation(Package package) : base(package) { }
        protected override Process BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Options.RunAsAdministrator || Tools.GetSettings("AlwaysElevate" + Package.Manager.Name))
            {
                if (Tools.GetSettings("DoCacheAdminRights") || Tools.GetSettings("DoCacheAdminRightsForBatches"))
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
            LineInfoText = Tools.Translate("{package} update failed. Click here for more details.").Replace("{package}", Package.Name);
            if (!Tools.GetSettings("DisableErrorNotifications") && !Tools.GetSettings("DisableNotifications"))
                try{new ToastContentBuilder()
                    .AddArgument("action", "openWingetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(Tools.Translate("Update failed"))
                    .AddText(Tools.Translate("{package} could not be updated").Replace("{package}", Package.Name)).Show();

                }
                catch (Exception ex)
                {
                    AppTools.Log(ex);
                }
            ContentDialog dialog = new();
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.XamlRoot = XamlRoot;
            dialog.Title = Tools.Translate("{package} update failed").Replace("{package}", Package.Name);
            dialog.Content = Tools.Translate("{package} could not be updated").Replace("{package}", Package.Name) + ". " + Tools.Translate("Please click the More Details button or refer to the Operation History for further information about the issue.");
            dialog.PrimaryButtonText = Tools.Translate("Retry");
            dialog.SecondaryButtonText = Tools.Translate("More details");
            dialog.CloseButtonText = Tools.Translate("Close");
            dialog.DefaultButton = ContentDialogButton.Primary;

            dialog.SecondaryButtonClick += (s, e) =>
            {
                OpenLiveViewDialog();
            };

            ContentDialogResult result = await Tools.App.mainWindow.ShowDialog(dialog);
            while (result == ContentDialogResult.Secondary)
                result = await Tools.App.mainWindow.ShowDialog(dialog, HighPriority: true);

            if (result == ContentDialogResult.Primary)
                return AfterFinshAction.Retry;
            else
                return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = Tools.Translate("{package} was updated successfully").Replace("{package}", Package.Name);
            Tools.App.mainWindow.NavigationPage.UpdatesPage.RemoveCorrespondingPackages(Package);
            if (!Tools.GetSettings("DisableSuccessNotifications") && !Tools.GetSettings("DisableNotifications"))
                try{new ToastContentBuilder()
                .AddArgument("action", "openWingetUI")
                .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                .AddText(Tools.Translate("Update succeeded"))
                .AddText(Tools.Translate("{package} was updated successfully").Replace("{package}", Package.Name)).Show();

                }
                catch (Exception ex)
                {
                    AppTools.Log(ex);
                }

            if(Package.Version == "Unknown")
                await Package.AddToIgnoredUpdatesAsync(Package.NewVersion);
            
            return AfterFinshAction.TimeoutClose;
        }

        protected override void Initialize()
        {
            OperationTitle = Tools.Translate("{package} Update").Replace("{package}", Package.Name);
            IconSource = Package.GetIconUrl();
        }
    }

    public class UninstallPackageOperation : PackageOperation
    {

        public UninstallPackageOperation(Package package, InstallationOptions options) : base(package, options) { }
        public UninstallPackageOperation(Package package) : base(package) { }
        protected override Process BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Options.RunAsAdministrator || Tools.GetSettings("AlwaysElevate" + Package.Manager.Name))
            {
                if (Tools.GetSettings("DoCacheAdminRights") || Tools.GetSettings("DoCacheAdminRightsForBatches"))
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
            LineInfoText = Tools.Translate("{package} uninstallation failed").Replace("{package}", Package.Name);

            if (!Tools.GetSettings("DisableErrorNotifications") && !Tools.GetSettings("DisableNotifications"))
                try{new ToastContentBuilder()
                    .AddArgument("action", "openWingetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(Tools.Translate("Uninstallation failed"))
                    .AddText(Tools.Translate("{package} could not be uninstalled").Replace("{package}", Package.Name)).Show();

                }
                catch (Exception ex)
                {
                    AppTools.Log(ex);
                }

            ContentDialog dialog = new();
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.XamlRoot = XamlRoot;
            dialog.Title = Tools.Translate("{package} uninstallation failed").Replace("{package}", Package.Name);
            dialog.Content = Tools.Translate("{package} could not be uninstalled").Replace("{package}", Package.Name) + ". " + Tools.Translate("Please click the More Details button or refer to the Operation History for further information about the issue.");
            dialog.PrimaryButtonText = Tools.Translate("Retry");
            dialog.SecondaryButtonText = Tools.Translate("More details");
            dialog.CloseButtonText = Tools.Translate("Close");
            dialog.DefaultButton = ContentDialogButton.Primary;

            dialog.SecondaryButtonClick += (s, e) =>
            {
                OpenLiveViewDialog();
            };

            ContentDialogResult result = await Tools.App.mainWindow.ShowDialog(dialog);
            while (result == ContentDialogResult.Secondary)
                result = await Tools.App.mainWindow.ShowDialog(dialog, HighPriority: true);

            if (result == ContentDialogResult.Primary)
                return AfterFinshAction.Retry;
            else
                return AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            LineInfoText = Tools.Translate("{package} was uninstalled successfully").Replace("{package}", Package.Name);
            Tools.App.mainWindow.NavigationPage.UpdatesPage.RemoveCorrespondingPackages(Package);
            Tools.App.mainWindow.NavigationPage.InstalledPage.RemoveCorrespondingPackages(Package);
            if (!Tools.GetSettings("DisableSuccessNotifications") && !Tools.GetSettings("DisableNotifications"))
                try{new ToastContentBuilder()
                .AddArgument("action", "openWingetUI")
                .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                .AddText(Tools.Translate("Uninstall succeeded"))
                .AddText(Tools.Translate("{package} was uninstalled successfully").Replace("{package}", Package.Name)).Show();

                }
                catch (Exception ex)
                {
                    AppTools.Log(ex);
                }
            await Task.Delay(0);
            return AfterFinshAction.TimeoutClose;
        }

        protected override void Initialize()
        {
            OperationTitle = Tools.Translate("{package} Uninstallation").Replace("{package}", Package.Name);
            IconSource = Package.GetIconUrl();
        }
    }
}
