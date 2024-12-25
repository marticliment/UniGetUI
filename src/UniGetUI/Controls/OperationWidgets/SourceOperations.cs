using System.Diagnostics;
using Windows.Media.Capture;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageOperations;
using UniGetUI.Pages.DialogPages;

namespace UniGetUI.PackageEngine.Operations
{

    public abstract class SourceOperation : AbstractProcessOperation
    {
        protected abstract Task HandleSuccess();
        protected abstract Task HandleFailure();
        protected abstract void Initialize();

        protected IManagerSource Source;

        public SourceOperation(IManagerSource source) : base(false)
        {
            Source = source;
            Initialize();
            OperationStarting += (_, _) => CreateProgressToast();
            OperationFinished += (_, _) => RemoveProgressToast();
            OperationSucceeded += (_, _) => HandleSuccess();
            OperationFailed += (_, _) => HandleFailure();
        }

        protected void ShowErrorNotification(string title, string body)
        {
            if (Settings.AreErrorNotificationsDisabled())
                return;

            try
            {
                AppNotificationManager.Default.RemoveByTagAsync(Source.Name);
                AppNotificationBuilder builder = new AppNotificationBuilder()
                    .SetScenario(AppNotificationScenario.Urgent)
                    .SetTag(Source.Name)
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
                AppNotificationManager.Default.RemoveByTagAsync(Source.Name);
                AppNotificationBuilder builder = new AppNotificationBuilder()
                    .SetScenario(AppNotificationScenario.Default)
                    .SetTag(Source.Name)
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

        protected void CreateProgressToast()
        {
            if (Settings.AreProgressNotificationsDisabled())
                return;

            try
            {
                AppNotificationManager.Default.RemoveByTagAsync(Source.Name + "progress");
                AppNotificationBuilder builder = new AppNotificationBuilder()
                    .SetScenario(AppNotificationScenario.Default)
                    .SetTag(Source.Name + "progress")
                    .AddProgressBar(new AppNotificationProgressBar()
                        .SetStatus(CoreTools.Translate("Please wait..."))
                        .SetValueStringOverride("\uE002")
                        .SetTitle(Metadata.Status)
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

        protected void RemoveProgressToast()
        {
            AppNotificationManager.Default.RemoveByTagAsync(Source.Name + "progress");
        }

        public override Task<Uri> GetOperationIcon()
        {
            return Task.FromResult(new Uri($"ms-appx:///Assets/Images/{Source.Manager.Properties.ColorIconId}.png"));
        }
    }

    public class AddSourceOperation : SourceOperation
    {
        public AddSourceOperation(IManagerSource source) : base(source)
        { }

        protected override async Task PrepareProcessStartInfo()
        {
           if (Source.Manager.Capabilities.Sources.MustBeInstalledAsAdmin)
            {
                if (Settings.Get("DoCacheAdminRights") || Settings.Get("DoCacheAdminRightsForBatches"))
                {
                    await CoreTools.CacheUACForCurrentProcess();
                }
                process.StartInfo.FileName = CoreData.GSudoPath;
                process.StartInfo.Arguments = $"\"{Source.Manager.Status.ExecutablePath}\" " + Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Source.Manager.SourcesHelper.GetAddSourceParameters(Source));
            }
            else
            {
                process.StartInfo.FileName = Source.Manager.Status.ExecutablePath;
                process.StartInfo.Arguments = Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Source.Manager.SourcesHelper.GetAddSourceParameters(Source));
            }
        }

        protected override Task<OperationVeredict> GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Task.Run(() => Source.Manager.SourcesHelper.GetAddOperationVeredict(Source, ReturnCode, Output));
        }

        protected override Task HandleFailure()
        {
            ShowErrorNotification(
                Metadata.FailureTitle,
                Metadata.FailureMessage);

            /*ContentDialogResult result = await DialogHelper.ShowOperationFailed(
                ProcessOutput,
                CoreTools.Translate("Source addition failed"),
                CoreTools.Translate("Could not add source {source} to {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })
            );*/

            return Task.CompletedTask;
        }

        protected override Task HandleSuccess()
        {
            ShowSuccessNotification(
                Metadata.SuccessTitle,
                Metadata.SuccessMessage);

            return Task.CompletedTask;
        }

        protected override void Initialize()
        {
            Metadata.OperationInformation = "Starting adding source operation for source=" + Source.Name +
                                            "with Manager=" + Source.Manager.Name;

            Metadata.Title = CoreTools.Translate("Adding source {source}", new Dictionary<string, object?> { { "source", Source.Name } });
            Metadata.Status = CoreTools.Translate("Adding source {source} to {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });;
            Metadata.SuccessTitle = CoreTools.Translate("Source added successfully");
            Metadata.SuccessMessage = CoreTools.Translate("The source {source} was added to {manager} successfully",
                new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });
            Metadata.FailureTitle = CoreTools.Translate("Could not add source");
            Metadata.FailureMessage = CoreTools.Translate("Could not add source {source} to {manager}",
                new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });
        }
    }

    public class RemoveSourceOperation : SourceOperation
    {
        public RemoveSourceOperation(IManagerSource source) : base(source)
        { }

        protected override async Task PrepareProcessStartInfo()
        {
            if (Source.Manager.Capabilities.Sources.MustBeInstalledAsAdmin)
            {
                if (Settings.Get("DoCacheAdminRights") || Settings.Get("DoCacheAdminRightsForBatches"))
                {
                    await CoreTools.CacheUACForCurrentProcess();
                }
                process.StartInfo.FileName = CoreData.GSudoPath;
                process.StartInfo.Arguments = $"\"{Source.Manager.Status.ExecutablePath}\" " + Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Source.Manager.SourcesHelper.GetRemoveSourceParameters(Source));

            }
            else
            {
                process.StartInfo.FileName = Source.Manager.Status.ExecutablePath;
                process.StartInfo.Arguments = Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Source.Manager.SourcesHelper.GetRemoveSourceParameters(Source));
            }
        }

        protected override Task<OperationVeredict> GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Task.Run(() => Source.Manager.SourcesHelper.GetRemoveOperationVeredict(Source, ReturnCode, Output));
        }

        protected override Task HandleFailure()
        {
            ShowErrorNotification(
                CoreTools.Translate("Removal failed"),
                CoreTools.Translate("Could not remove source {source} from {manager}",
                    new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })
            );

            /*ContentDialogResult result = await DialogHelper.ShowOperationFailed(
                ProcessOutput,
                CoreTools.Translate("Source removal failed"),
                CoreTools.Translate("Could not remove source {source} from {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })
            );*/
            return Task.CompletedTask;

        }

        protected override Task HandleSuccess()
        {
            ShowSuccessNotification(
                CoreTools.Translate("Removal succeeded"),
                CoreTools.Translate("The source {source} was removed from {manager} successfully",
                    new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })
            );
            return Task.CompletedTask;
        }

        protected override void Initialize()
        {
            Metadata.OperationInformation = "Starting remove source operation for source=" + Source.Name + "with Manager=" + Source.Manager.Name;

            Metadata.Title = CoreTools.Translate("Removing source {source}", new Dictionary<string, object?> { { "source", Source.Name } });
            Metadata.Status = CoreTools.Translate("Removing source {source} from {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });;
            Metadata.SuccessTitle = CoreTools.Translate("Source removed successfully");
            Metadata.SuccessMessage = CoreTools.Translate("The source {source} was removed from {manager} successfully",
                new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });
            Metadata.FailureTitle = CoreTools.Translate("Could not remove source");
            Metadata.FailureMessage = CoreTools.Translate("Could not remove source {source} from {manager}",
                new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });
        }
    }
}
