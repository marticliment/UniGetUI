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

    public abstract class SourceOperation : AbstractOperation
    {
        protected ManagerSource Source;
        public SourceOperation(ManagerSource source)
        {
            Source = source;
            MainProcedure();
        }
    }

    public class AddSourceOperation : SourceOperation
    {

        public event EventHandler<EventArgs> OperationSucceeded;
        public AddSourceOperation(ManagerSource source) : base(source) { }
        protected override Process BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Source.Manager.Capabilities.Sources.MustBeInstalledAsAdmin)
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
                startInfo.Arguments = "\"" + Source.Manager.Status.ExecutablePath + "\"" + Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", (Source.Manager as PackageManagerWithSources).GetAddSourceParameters(Source));

            }
            else
            {
                startInfo.FileName = Source.Manager.Status.ExecutablePath;
                startInfo.Arguments = Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", (Source.Manager as PackageManagerWithSources).GetAddSourceParameters(Source));
            }
            Process process = new();
            process.StartInfo = startInfo;

            return process;
        }

        protected override string[] GenerateProcessLogHeader()
        {
            return new string[]
            {
                "Starting adding source operation for source name=" + Source.Name + "with Manager name=" + Source.Manager.Name,
            };
        }

        protected override OperationVeredict GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return (Source.Manager as PackageManagerWithSources).GetAddSourceOperationVeredict(Source, ReturnCode, Output);
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = bindings.Translate("Could not add source {source} to {manager}").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name);
            if (!bindings.GetSettings("DisableErrorNotifications") && !bindings.GetSettings("DisableNotifications"))
                new ToastContentBuilder()
                    .AddArgument("action", "openWingetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(bindings.Translate("Installation failed"))
                    .AddText(bindings.Translate("Could not add source {source} to {manager}").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name)).Show();

            ContentDialog dialog = new();
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.XamlRoot = XamlRoot;
            dialog.Title = bindings.Translate("Source addition failed");
            dialog.Content = bindings.Translate("Could not add source {source} to {manager}").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name) + ". " + bindings.Translate("Please click the More Details button or refer to the Operation History for further information about the issue.");
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
            OperationSucceeded?.Invoke(this, new EventArgs());
            LineInfoText = bindings.Translate("The source {source} was added to {manager} successfully").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name);
            if (!bindings.GetSettings("DisableSuccessNotifications") && !bindings.GetSettings("DisableNotifications"))
                new ToastContentBuilder()
                    .AddArgument("action", "openWingetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(bindings.Translate("Addition succeeded"))
                    .AddText(bindings.Translate("The source {source} was added to {manager} successfully").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name)).Show();
            await Task.Delay(0);
            return AfterFinshAction.TimeoutClose;
        }

        protected override void Initialize()
        {
            OperationTitle = bindings.Translate("Adding source {source} to {manager}").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name);
            IconSource = new Uri("ms-appx:///Assets/Images/" + Source.Manager.Properties.ColorIconId + ".png");
        }
    }

    public class RemoveSourceOperation : SourceOperation
    {

        public event EventHandler<EventArgs> OperationSucceeded;
        public RemoveSourceOperation(ManagerSource source) : base(source) { }
        protected override Process BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Source.Manager.Capabilities.Sources.MustBeInstalledAsAdmin)
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
                startInfo.Arguments = "\"" + Source.Manager.Status.ExecutablePath + "\"" + Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", (Source.Manager as PackageManagerWithSources).GetRemoveSourceParameters(Source));

            }
            else
            {
                startInfo.FileName = Source.Manager.Status.ExecutablePath;
                startInfo.Arguments = Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", (Source.Manager as PackageManagerWithSources).GetRemoveSourceParameters(Source));
            }
            Process process = new();
            process.StartInfo = startInfo;

            return process;
        }

        protected override string[] GenerateProcessLogHeader()
        {
            return new string[]
            {
                "Starting remove source operation for source name=" + Source.Name + "with Manager name=" + Source.Manager.Name,
            };
        }

        protected override OperationVeredict GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return (Source.Manager as PackageManagerWithSources).GetRemoveSourceOperationVeredict(Source, ReturnCode, Output);
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = bindings.Translate("Could not remove source {source} from {manager}").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name);
            if (!bindings.GetSettings("DisableErrorNotifications") && !bindings.GetSettings("DisableNotifications"))
                new ToastContentBuilder()
                    .AddArgument("action", "openWingetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(bindings.Translate("Removal failed"))
                    .AddText(bindings.Translate("Could not remove source {source} from {manager}").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name)).Show();

            ContentDialog dialog = new();
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.XamlRoot = XamlRoot;
            dialog.Title = bindings.Translate("Source removal failed");
            dialog.Content = bindings.Translate("Could not remove source {source} from {manager}").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name) + ". " + bindings.Translate("Please click the More Details button or refer to the Operation History for further information about the issue.");
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
            OperationSucceeded?.Invoke(this, new EventArgs());
            LineInfoText = bindings.Translate("The source {source} was removed from {manager} successfully").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name);
            if (!bindings.GetSettings("DisableSuccessNotifications") && !bindings.GetSettings("DisableNotifications"))
                new ToastContentBuilder()
                    .AddArgument("action", "openWingetUI")
                    .AddArgument("notificationId", CoreData.VolatileNotificationIdCounter)
                    .AddText(bindings.Translate("Removal succeeded"))
                    .AddText(bindings.Translate("The source {source} was removed from {manager} successfully").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name)).Show();
            await Task.Delay(0);
            return AfterFinshAction.TimeoutClose;
        }

        protected override void Initialize()
        {
            OperationTitle = bindings.Translate("Removing source {source} from {manager}").Replace("{source}", Source.Name).Replace("{manager}", Source.Manager.Name);
            IconSource = new Uri("ms-appx:///Assets/Images/" + Source.Manager.Properties.ColorIconId + ".png");
        }
    }
}
