using System.Diagnostics;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Classes.Manager.Providers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Interfaces.ManagerProviders;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Classes.Manager
{
    public class NullPackageManager : IPackageManager
    {
        public static NullPackageManager Instance = new();
        public ManagerProperties Properties { get; set; }
        public ManagerCapabilities Capabilities { get; set; }
        public ManagerStatus Status { get; set; }
        public string Name { get => Properties.Name; set { } }
        public string DisplayName { get => Properties.DisplayName ?? Properties.Name; set { } }
        public IManagerSource DefaultSource { get => Properties.DefaultSource; set { } }
        public bool ManagerReady { get => true; set { } }

        public IManagerLogger TaskLogger { get; set; }

        public ISourceProvider? SourceProvider { get; set; }

        public IPackageDetailsProvider? PackageDetailsProvider { get; set; }

        public ISourceFactory SourceFactory { get; set; }

        public IOperationProvider? OperationProvider { get; set; }

        public NullPackageManager()
        {
            TaskLogger = new ManagerLogger(this);
            var nullsource = NullSource.Instance;
            SourceProvider = new NullSourceProvider(this);
            PackageDetailsProvider = new NullPackageDetailsProvider(this);
            OperationProvider = new NullOperationProvider(this);
            SourceFactory = new SourceFactory(this);
            Properties = new ManagerProperties
            {
                IsDummy = true,
                Name = CoreTools.Translate("Unknown"),
                Description = "Unset",
                IconId = IconType.Help,
                ColorIconId = "Unset",
                ExecutableCallArgs = "Unset",
                ExecutableFriendlyName = "Unset",
                InstallVerb = "Unset",
                UpdateVerb = "Unset",
                UninstallVerb = "Unset",
                KnownSources = [nullsource],
                DefaultSource = nullsource,
            };

            Capabilities = new ManagerCapabilities();

            Status = new ManagerStatus
            {
                ExecutablePath = "C:/file.exe",
                Found = false,
                Version = "0"
            };
        }

        public IEnumerable<IPackage> FindPackages(string query)
        {
            throw new NotImplementedException();
        }

        public OperationVeredict GetAddSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            throw new NotImplementedException();
        }

        public string[] GetAddSourceParameters(IManagerSource source)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPackage> GetAvailableUpdates()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPackage> GetInstalledPackages()
        {
            throw new NotImplementedException();
        }

        public OperationVeredict GetInstallOperationVeredict(IPackage package, IInstallationOptions options, int ReturnCode, string[] Output)
        {
            throw new NotImplementedException();
        }

        public string[] GetInstallParameters(IPackage package, IInstallationOptions options)
        {
            throw new NotImplementedException();
        }

        public void GetPackageDetails(IPackageDetails details)
        {
            throw new NotImplementedException();
        }

        public CacheableIcon? GetPackageIconUrl(IPackage package)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Uri> GetPackageScreenshotsUrl(IPackage package)
        {
            throw new NotImplementedException();
        }

        public string? GetPackageInstallLocation(IPackage package)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetPackageVersions(IPackage package)
        {
            throw new NotImplementedException();
        }

        public OperationVeredict GetRemoveSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            throw new NotImplementedException();
        }

        public string[] GetRemoveSourceParameters(IManagerSource source)
        {
            throw new NotImplementedException();
        }

        public IManagerSource? GetSourceIfExists(string SourceName)
        {
            throw new NotImplementedException();
        }

        public IManagerSource GetSourceOrDefault(string SourceName)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IManagerSource> GetSources()
        {
            throw new NotImplementedException();
        }

        public OperationVeredict GetUninstallOperationVeredict(IPackage package, IInstallationOptions options, int ReturnCode, string[] Output)
        {
            throw new NotImplementedException();
        }

        public string[] GetUninstallParameters(IPackage package, IInstallationOptions options)
        {
            throw new NotImplementedException();
        }

        public OperationVeredict GetUpdateOperationVeredict(IPackage package, IInstallationOptions options, int ReturnCode, string[] Output)
        {
            throw new NotImplementedException();
        }

        public string[] GetUpdateParameters(IPackage package, IInstallationOptions options)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled()
        {
            throw new NotImplementedException();
        }

        public bool IsReady()
        {
            throw new NotImplementedException();
        }

        public void LogOperation(Process process, string output)
        {
            throw new NotImplementedException();
        }

        public void RefreshPackageIndexes()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetOperationParameters(IPackage package, IInstallationOptions options, OperationType operation)
        {
            throw new NotImplementedException();
        }

        public OperationVeredict GetOperationResult(IPackage package, OperationType operation, IEnumerable<string> processOutput, int returnCode)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class NullSourceProvider : BaseSourceProvider<IPackageManager>
    {
        public NullSourceProvider(IPackageManager manager) : base(manager)
        {
        }

        public override string[] GetAddSourceParameters(IManagerSource source)
        {
            throw new InvalidOperationException("Package manager does not support adding sources");
        }
        public override string[] GetRemoveSourceParameters(IManagerSource source)
        {
            throw new InvalidOperationException("Package manager does not support removing sources");
        }

        public override OperationVeredict GetAddSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            return OperationVeredict.Failed;
        }

        public override OperationVeredict GetRemoveSourceOperationVeredict(IManagerSource source, int ReturnCode, string[] Output)
        {
            return OperationVeredict.Failed;
        }

        protected override IEnumerable<IManagerSource> GetSources_UnSafe()
        {
            return Array.Empty<IManagerSource>();
        }
    }

    internal sealed class NullPackageDetailsProvider : BasePackageDetailsProvider<IPackageManager>
    {
        public NullPackageDetailsProvider(IPackageManager manager) : base(manager)
        {
        }

        protected override void GetDetails_UnSafe(IPackageDetails details)
        {
            return;
        }

        protected override CacheableIcon? GetIcon_UnSafe(IPackage package)
        {
            return null;
        }

        protected override IEnumerable<Uri> GetScreenshots_UnSafe(IPackage package)
        {
            return [];
        }

        protected override string? GetInstallLocation_UnSafe(IPackage package)
        {
            return null;
        }

        protected override IEnumerable<string> GetInstallableVersions_UnSafe(IPackage package)
        {
            return [];
        }
    }

    internal sealed class NullOperationProvider : BaseOperationProvider<IPackageManager>
    {
        public NullOperationProvider(IPackageManager manager) : base(manager)
        {
        }

        public override IEnumerable<string> GetOperationParameters(IPackage package, IInstallationOptions options, OperationType operation)
        {
            return Array.Empty<string>();
        }

        public override OperationVeredict GetOperationResult(IPackage package, OperationType operation, IEnumerable<string> processOutput, int returnCode)
        {
            return OperationVeredict.Failed;
        }
    }
}
