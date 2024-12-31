using System.Diagnostics;
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
using UniGetUI.PackageEngine.PackageLoader;
using UniGetUI.PackageOperations;

namespace UniGetUI.PackageEngine.Operations
{
    public abstract class PackageOperation : AbstractProcessOperation
    {
        protected List<string> DesktopShortcutsBeforeStart = [];

        public readonly IPackage Package;
        public readonly IInstallationOptions Options;
        public readonly OperationType Role;

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

            Initialize();

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
            OperationSucceeded += (_, _) => HandleSuccess();
            OperationFailed += (_, _) => HandleFailure();
        }

        public PackageOperation(
            IPackage package,
            OperationType role,
            bool IgnoreParallelInstalls = false)
            : this(package, InstallationOptions.FromPackage(package), role, IgnoreParallelInstalls) { }

        protected override void ApplyRetryAction(string retryMode)
        {
            switch (retryMode)
            {
                case RetryMode.Retry_AsAdmin:
                    Options.RunAsAdministrator = true;
                    break;
                case RetryMode.Retry_Interactive:
                    Options.InteractiveInstallation = true;
                    break;
                case RetryMode.Retry_SkipIntegrity:
                    Options.SkipHashCheck = true;
                    break;
                case RetryMode.Retry:
                    break;
                default:
                    throw new InvalidOperationException($"Retry mode {retryMode} is not supported in this context");
            }
            Metadata.OperationInformation = "Retried package operation for Package=" + Package.Id + " with Manager=" +
                                            Package.Manager.Name + "\nUpdated installation options: " + Options.ToString();

        }

        protected sealed override void PrepareProcessStartInfo()
        {
            bool admin = false;
            Package.SetTag(PackageTag.OnQueue);
            string operation_args = string.Join(" ", Package.Manager.OperationHelper.GetParameters(Package, Options, Role));

            if (Package.OverridenOptions.RunAsAdministrator == true
                || Options.RunAsAdministrator
                || (Settings.Get("AlwaysElevate" + Package.Manager.Name)
                    && !Package.OverridenOptions.RunAsAdministrator is false)
                )
            {
                admin = true;
                if (Settings.Get("DoCacheAdminRights") || Settings.Get("DoCacheAdminRightsForBatches"))
                {
                    CoreTools.CacheUACForCurrentProcess().GetAwaiter().GetResult();
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
            ApplyCapabilities(admin,
                Options.InteractiveInstallation,
                (Options.SkipHashCheck && Role is not OperationType.Uninstall),
                Package.OverridenOptions.Scope ?? Options.InstallationScope);
        }

        protected sealed override Task<OperationVeredict> GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Task.FromResult(Package.Manager.OperationHelper.GetResult(Package, Role, Output, ReturnCode));
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

        protected override Task HandleFailure()
        {
            Package.SetTag(PackageTag.Failed);
            return Task.CompletedTask;
        }

        protected override Task HandleSuccess()
        {
            Package.SetTag(PackageTag.AlreadyInstalled);
            InstalledPackagesLoader.Instance.AddForeign(Package);

            if (Settings.Get("AskToDeleteNewDesktopShortcuts"))
            {
                DesktopShortcutsDatabase.TryRemoveNewShortcuts(DesktopShortcutsBeforeStart);
            }
            return Task.CompletedTask;
        }

        protected override void Initialize()
        {
            Metadata.OperationInformation = "Package install operation for Package=" + Package.Id + " with Manager=" +
                                            Package.Manager.Name + "\nInstallation options: " + Options.ToString();

            Metadata.Title = CoreTools.Translate("{package} Installation", new Dictionary<string, object?> { { "package", Package.Name } });
            Metadata.Status = CoreTools.Translate("{0} is being installed", Package.Name);
            Metadata.SuccessTitle = CoreTools.Translate("Installation succeeded");
            Metadata.SuccessMessage = CoreTools.Translate("{package} was installed successfully",  new Dictionary<string, object?> { { "package", Package.Name } });
            Metadata.FailureTitle = CoreTools.Translate("Installation failed", new Dictionary<string, object?> { { "package", Package.Name } });
            Metadata.FailureMessage = CoreTools.Translate("{package} could not be installed", new Dictionary<string, object?> { { "package", Package.Name } });

            if (Settings.Get("AskToDeleteNewDesktopShortcuts"))
            {
                DesktopShortcutsBeforeStart = DesktopShortcutsDatabase.GetShortcuts();
            }
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

        protected override Task HandleFailure()
        {
            Package.SetTag(PackageTag.Failed);
            return Task.CompletedTask;
        }

        protected override async Task HandleSuccess()
        {
            Package.SetTag(PackageTag.Default);
            Package.GetInstalledPackage()?.SetTag(PackageTag.Default);
            Package.GetAvailablePackage()?.SetTag(PackageTag.AlreadyInstalled);

            UpgradablePackagesLoader.Instance.Remove(Package);

            if (await Package.HasUpdatesIgnoredAsync() && await Package.GetIgnoredUpdatesVersionAsync() != "*")
                await Package.RemoveFromIgnoredUpdatesAsync();

            if (Settings.Get("AskToDeleteNewDesktopShortcuts"))
            {
                DesktopShortcutsDatabase.TryRemoveNewShortcuts(DesktopShortcutsBeforeStart);
            }
        }

        protected override void Initialize()
        {
            Metadata.OperationInformation = "Package update operation for Package=" + Package.Id + " with Manager=" +
                                            Package.Manager.Name + "\nInstallation options: " + Options.ToString();

            Metadata.Title = CoreTools.Translate("{package} Update", new Dictionary<string, object?> { { "package", Package.Name } });
            Metadata.Status = CoreTools.Translate("{0} is being updated to version {1}", Package.Name, Package.NewVersion);
            Metadata.SuccessTitle = CoreTools.Translate("Update succeeded");
            Metadata.SuccessMessage = CoreTools.Translate("{package} was updated successfully",  new Dictionary<string, object?> { { "package", Package.Name } });
            Metadata.FailureTitle = CoreTools.Translate("Update failed", new Dictionary<string, object?> { { "package", Package.Name } });
            Metadata.FailureMessage = CoreTools.Translate("{package} could not be updated", new Dictionary<string, object?> { { "package", Package.Name } });

            if (Settings.Get("AskToDeleteNewDesktopShortcuts"))
            {
                DesktopShortcutsBeforeStart = DesktopShortcutsDatabase.GetShortcuts();
            }
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

        protected override Task HandleFailure()
        {
            Package.SetTag(PackageTag.Failed);
            return Task.CompletedTask;
        }

        protected override Task HandleSuccess()
        {
            Package.SetTag(PackageTag.Default);
            Package.GetAvailablePackage()?.SetTag(PackageTag.Default);
            UpgradablePackagesLoader.Instance.Remove(Package);
            InstalledPackagesLoader.Instance.Remove(Package);

            return Task.CompletedTask;
        }

        protected override void Initialize()
        {
            Metadata.OperationInformation = "Package uninstall operation for Package=" + Package.Id + " with Manager=" +
                                            Package.Manager.Name + "\nInstallation options: " + Options.ToString();

            Metadata.Title = CoreTools.Translate("{package} Uninstall", new Dictionary<string, object?> { { "package", Package.Name } });
            Metadata.Status = CoreTools.Translate("{0} is being uninstalled", Package.Name);
            Metadata.SuccessTitle = CoreTools.Translate("Uninstall succeeded");
            Metadata.SuccessMessage = CoreTools.Translate("{package} was uninstalled successfully",  new Dictionary<string, object?> { { "package", Package.Name } });
            Metadata.FailureTitle = CoreTools.Translate("Uninstall failed", new Dictionary<string, object?> { { "package", Package.Name } });
            Metadata.FailureMessage = CoreTools.Translate("{package} could not be uninstalled", new Dictionary<string, object?> { { "package", Package.Name } });
        }
    }
}
