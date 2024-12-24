using System.Diagnostics;
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
        protected abstract void GenerateProcessLogHeader();
        protected abstract Task HandleSuccess();
        protected abstract Task HandleFailure();
        protected abstract void Initialize();

        protected IManagerSource Source;
        protected string OPERATION_ONGOING_STRING = null!;

        public SourceOperation(IManagerSource source) : base(false)
        {
            Source = source;
            if (OPERATION_ONGOING_STRING is null)
            {
                throw new NullReferenceException("OPERATION_ONGOING_STRING must be set to a non-null value in the Initialize method");
            }
            Initialize();
            GenerateProcessLogHeader();

            OperationStarting += (_, _) => CreateProgressToast();
            OperationFinished += (_, _) => RemoveProgressToast();
            OperationSucceeded += (_, _) => HandleSuccess();
            OperationFailed += (_, _) => HandleFailure();
        }

        public override string GetOperationTitle()
        {
            return OPERATION_ONGOING_STRING;
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
                        .SetTitle(OPERATION_ONGOING_STRING)
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

        protected override void GenerateProcessLogHeader()
        {
            Line(
                "Starting adding source operation for source name=" + Source.Name + "with Manager name=" +
                Source.Manager.Name, LineType.Debug);
        }

        protected override Task<OperationVeredict> GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Task.Run(() => Source.Manager.SourcesHelper.GetAddOperationVeredict(Source, ReturnCode, Output));
        }

        protected override Task HandleFailure()
        {
            Line(
                CoreTools.Translate("Could not add source {source} to {manager}",
                    new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } }),
                LineType.Progress);

            ShowErrorNotification(
                CoreTools.Translate("Installation failed"),
                CoreTools.Translate("Could not add source {source} to {manager}",
                    new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })
            );

            /*ContentDialogResult result = await DialogHelper.ShowOperationFailed(
                ProcessOutput,
                CoreTools.Translate("Source addition failed"),
                CoreTools.Translate("Could not add source {source} to {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })
            );*/

            return Task.CompletedTask;
        }

        protected override Task HandleSuccess()
        {
            Line(CoreTools.Translate("The source {source} was added to {manager} successfully", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } }), LineType.Progress);

            ShowSuccessNotification(
                CoreTools.Translate("Addition succeeded"),
                CoreTools.Translate("The source {source} was added to {manager} successfully",
                    new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })
            );

            return Task.CompletedTask;
        }

        protected override void Initialize()
        {
            OPERATION_ONGOING_STRING = CoreTools.Translate("Adding source {source} to {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });
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

        protected override void GenerateProcessLogHeader()
        {
            Line(
                "Starting remove source operation for source name=" + Source.Name + "with Manager name=" +
                Source.Manager.Name, LineType.Debug);
        }

        protected override Task<OperationVeredict> GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Task.Run(() => Source.Manager.SourcesHelper.GetRemoveOperationVeredict(Source, ReturnCode, Output));
        }

        protected override Task HandleFailure()
        {
            Line(
                CoreTools.Translate("Could not remove source {source} from {manager}",
                    new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } }),
                LineType.Progress);

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
            Line(CoreTools.Translate("The source {source} was removed from {manager} successfully", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } }), LineType.Progress);

            ShowSuccessNotification(
                CoreTools.Translate("Removal succeeded"),
                CoreTools.Translate("The source {source} was removed from {manager} successfully",
                    new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })
            );
            return Task.CompletedTask;
        }

        protected override void Initialize()
        {
            OPERATION_ONGOING_STRING = CoreTools.Translate("Removing source {source} from {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });
        }
    }
}
