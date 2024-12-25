using System.Diagnostics;
using Windows.Media.Capture;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageOperations;

namespace UniGetUI.PackageEngine.Operations
{

    public abstract class SourceOperation : AbstractProcessOperation
    {
        protected abstract void Initialize();

        protected IManagerSource Source;

        public SourceOperation(IManagerSource source) : base(false)
        {
            Source = source;
            Initialize();
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
