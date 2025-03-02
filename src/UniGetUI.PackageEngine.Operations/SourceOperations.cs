using UniGetUI.Core.Data;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Managers.WingetManager;
using UniGetUI.PackageOperations;

namespace UniGetUI.PackageEngine.Operations
{

    public abstract class SourceOperation : AbstractProcessOperation
    {
        protected abstract void Initialize();

        protected IManagerSource Source;
        public bool ForceAsAdministrator { get; private set; }

        public SourceOperation(IManagerSource source) : base(false, null)
        {
            Source = source;
            Initialize();
        }

        public override Task<Uri> GetOperationIcon()
        {
            return Task.FromResult(new Uri($"ms-appx:///Assets/Images/{Source.Manager.Properties.ColorIconId}.png"));
        }

        protected override void ApplyRetryAction(string retryMode)
        {
            switch (retryMode)
            {
                case RetryMode.Retry:
                    break;
                case RetryMode.Retry_AsAdmin:
                    ForceAsAdministrator = true;
                    break;
                default:
                    throw new InvalidOperationException($"Retry mode {retryMode} is not supported in this context");
            }
        }
    }

    public class AddSourceOperation : SourceOperation
    {
        public AddSourceOperation(IManagerSource source) : base(source)
        { }

        protected override void PrepareProcessStartInfo()
        {
            bool admin = false;
           if (ForceAsAdministrator || Source.Manager.Capabilities.Sources.MustBeInstalledAsAdmin)
           {
                if (Settings.Get("DoCacheAdminRights") || Settings.Get("DoCacheAdminRightsForBatches"))
                {
                    CoreTools.CacheUACForCurrentProcess().GetAwaiter().GetResult();
                }

                if (Source.Manager is WinGet)
                    RedirectWinGetTempFolder();

                admin = true;
                process.StartInfo.FileName = CoreData.ElevatorPath;
                process.StartInfo.Arguments = $"\"{Source.Manager.Status.ExecutablePath}\" " + Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Source.Manager.SourcesHelper.GetAddSourceParameters(Source));

            }
            else
            {
                process.StartInfo.FileName = Source.Manager.Status.ExecutablePath;
                process.StartInfo.Arguments = Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Source.Manager.SourcesHelper.GetAddSourceParameters(Source));
            }

           ApplyCapabilities(admin, false, false, null);
        }

        protected override Task<OperationVeredict> GetProcessVeredict(int ReturnCode, List<string> Output)
        {
            return Task.Run(() => Source.Manager.SourcesHelper.GetAddOperationVeredict(Source, ReturnCode, Output.ToArray()));
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

        protected override void PrepareProcessStartInfo()
        {
            bool admin = false;
            if (ForceAsAdministrator || Source.Manager.Capabilities.Sources.MustBeInstalledAsAdmin)
            {
                if (Settings.Get("DoCacheAdminRights") || Settings.Get("DoCacheAdminRightsForBatches"))
                {
                    CoreTools.CacheUACForCurrentProcess().GetAwaiter().GetResult();
                }

                if (Source.Manager is WinGet)
                    RedirectWinGetTempFolder();

                admin = true;
                process.StartInfo.FileName = CoreData.ElevatorPath;
                process.StartInfo.Arguments = $"\"{Source.Manager.Status.ExecutablePath}\" " + Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Source.Manager.SourcesHelper.GetRemoveSourceParameters(Source));
            }
            else
            {
                process.StartInfo.FileName = Source.Manager.Status.ExecutablePath;
                process.StartInfo.Arguments = Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Source.Manager.SourcesHelper.GetRemoveSourceParameters(Source));
            }
            ApplyCapabilities(admin, false, false, null);
        }

        protected override Task<OperationVeredict> GetProcessVeredict(int ReturnCode, List<string> Output)
        {
            return Task.Run(() => Source.Manager.SourcesHelper.GetRemoveOperationVeredict(Source, ReturnCode, Output.ToArray()));
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
