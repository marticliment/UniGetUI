using UniGetUI.Core.Classes;
using UniGetUI.Core.Data;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Packages.Classes;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Managers.WingetManager;
using UniGetUI.PackageEngine.PackageLoader;
using UniGetUI.PackageEngine.Serializable;
using UniGetUI.PackageOperations;

namespace UniGetUI.PackageEngine.Operations
{
    public abstract class PackageOperation : AbstractProcessOperation
    {
        protected List<string> DesktopShortcutsBeforeStart = [];

        public readonly IPackage Package;
        public readonly InstallOptions Options;
        public readonly OperationType Role;

        protected abstract Task HandleSuccess();
        protected abstract Task HandleFailure();
        protected abstract void Initialize();

        public PackageOperation(
            IPackage package,
            InstallOptions options,
            OperationType role,
            bool IgnoreParallelInstalls = false,
            AbstractOperation? req = null)
            : base(!IgnoreParallelInstalls, _getPreInstallOps(options, role, req), _getPostInstallOps(options, role))
        {
            Package = package;
            Options = options;
            Role = role;

            Initialize();

            Enqueued += (_, _) =>
            {
                ApplyCapabilities(RequiresAdminRights(),
                    Options.InteractiveInstallation,
                    (Options.SkipHashCheck && Role is not OperationType.Uninstall),
                    Package.OverridenOptions.Scope ?? Options.InstallationScope);

                Package.SetTag(PackageTag.OnQueue);
            };
            CancelRequested += (_, _) => Package.SetTag(PackageTag.Default);
            OperationSucceeded += (_, _) => HandleSuccess();
            OperationFailed += (_, _) => HandleFailure();
        }

        private bool RequiresAdminRights()
            => Package.OverridenOptions.RunAsAdministrator is true || Options.RunAsAdministrator;

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
                                            Package.Manager.Name + "\nUpdated installation options: " + Options.ToString()
                                            + "\nOverriden options: " + Package.OverridenOptions.ToString();
        }

        protected sealed override void PrepareProcessStartInfo()
        {
            bool IsAdmin = CoreTools.IsAdministrator();
            Package.SetTag(PackageTag.OnQueue);
            string operation_args = string.Join(" ", Package.Manager.OperationHelper.GetParameters(Package, Options, Role));
            string FileName, Arguments;

            if (RequiresAdminRights() && IsAdmin is false)
            {
                IsAdmin = true;
                if (Settings.Get(Settings.K.DoCacheAdminRights) || Settings.Get(Settings.K.DoCacheAdminRightsForBatches))
                {
                    RequestCachingOfUACPrompt();
                }

                FileName = CoreData.ElevatorPath;
                Arguments = $"\"{Package.Manager.Status.ExecutablePath}\" {Package.Manager.Properties.ExecutableCallArgs} {operation_args}";
            }
            else
            {
                FileName = Package.Manager.Status.ExecutablePath;
                Arguments = $"{Package.Manager.Properties.ExecutableCallArgs} {operation_args}";
            }

            if (IsAdmin && Package.Manager is WinGet)
            {
                RedirectWinGetTempFolder();
            }

            process.StartInfo.FileName = FileName;
            process.StartInfo.Arguments = Arguments;

            ApplyCapabilities(
                IsAdmin,
                Options.InteractiveInstallation,
                (Options.SkipHashCheck && Role is not OperationType.Uninstall),
                Package.OverridenOptions.Scope ?? Options.InstallationScope
            );
        }

        protected sealed override Task<OperationVeredict> GetProcessVeredict(int ReturnCode, List<string> Output)
        {
            return Task.FromResult(Package.Manager.OperationHelper.GetResult(Package, Role, Output, ReturnCode));
        }

        public override Task<Uri> GetOperationIcon()
        {
            return TaskRecycler<Uri>.RunOrAttachAsync(Package.GetIconUrl);
        }

        private static IReadOnlyList<InnerOperation> _getPreInstallOps(InstallOptions opts, OperationType role, AbstractOperation? preReq = null)
        {
            List<InnerOperation> l = new();
            if(preReq is not null) l.Add(new(preReq, true));

            foreach (var process in opts.KillBeforeOperation)
                l.Add(new InnerOperation(
                    new KillProcessOperation(process),
                    mustSucceed: false));

            if (role is OperationType.Install && opts.PreInstallCommand.Any())
                l.Add(new(new PrePostOperation(opts.PreInstallCommand), opts.AbortOnPreInstallFail));
            else if (role is OperationType.Update && opts.PreUpdateCommand.Any())
                l.Add(new(new PrePostOperation(opts.PreUpdateCommand), opts.AbortOnPreUpdateFail));
            else if (role is OperationType.Uninstall && opts.PreUninstallCommand.Any())
                l.Add(new(new PrePostOperation(opts.PreUninstallCommand), opts.AbortOnPreUninstallFail));

            return l;
        }

        private static IReadOnlyList<InnerOperation> _getPostInstallOps(InstallOptions opts, OperationType role)
        {
            List<InnerOperation> l = new();

            if (role is OperationType.Install && opts.PostInstallCommand.Any())
                l.Add(new(new PrePostOperation(opts.PostInstallCommand), false));
            else if (role is OperationType.Update && opts.PostUpdateCommand.Any())
                l.Add(new(new PrePostOperation(opts.PostUpdateCommand), false));
            else if (role is OperationType.Uninstall && opts.PostUninstallCommand.Any())
                l.Add(new(new PrePostOperation(opts.PostUninstallCommand), false));

            return l;
        }
    }

    /*
     *
     *
     *
     * PER-OPERATION PACKAGE OPERATIONS
     *
     *
     *
     */
    public class InstallPackageOperation : PackageOperation
    {
        public InstallPackageOperation(
            IPackage package,
            InstallOptions options,
            bool IgnoreParallelInstalls = false,
            AbstractOperation? req = null)
            : base(package, options, OperationType.Install, IgnoreParallelInstalls, req)
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

            if (Settings.Get(Settings.K.AskToDeleteNewDesktopShortcuts))
            {
                DesktopShortcutsDatabase.HandleNewShortcuts(DesktopShortcutsBeforeStart);
            }
            return Task.CompletedTask;
        }

        protected override void Initialize()
        {
            Metadata.OperationInformation = "Package install operation for Package=" + Package.Id + " with Manager="
                                            + Package.Manager.Name + "\nInstallation options: " + Options.ToString()
                                            + "\nOverriden options: " + Package.OverridenOptions.ToString();

            Metadata.Title = CoreTools.Translate("{package} Installation", new Dictionary<string, object?> { { "package", Package.Name } });
            Metadata.Status = CoreTools.Translate("{0} is being installed", Package.Name);
            Metadata.SuccessTitle = CoreTools.Translate("Installation succeeded");
            Metadata.SuccessMessage = CoreTools.Translate("{package} was installed successfully",  new Dictionary<string, object?> { { "package", Package.Name } });
            Metadata.FailureTitle = CoreTools.Translate("Installation failed", new Dictionary<string, object?> { { "package", Package.Name } });
            Metadata.FailureMessage = CoreTools.Translate("{package} could not be installed", new Dictionary<string, object?> { { "package", Package.Name } });

            if (Settings.Get(Settings.K.AskToDeleteNewDesktopShortcuts))
            {
                DesktopShortcutsBeforeStart = DesktopShortcutsDatabase.GetShortcutsOnDisk();
            }
        }
    }

    public class UpdatePackageOperation : PackageOperation
    {

        public UpdatePackageOperation(
            IPackage package,
            InstallOptions options,
            bool IgnoreParallelInstalls = false,
            AbstractOperation? req = null)
            : base(package, options, OperationType.Update, IgnoreParallelInstalls, req)
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

            if (Settings.Get(Settings.K.AskToDeleteNewDesktopShortcuts))
            {
                DesktopShortcutsDatabase.HandleNewShortcuts(DesktopShortcutsBeforeStart);
            }

            if (await Package.HasUpdatesIgnoredAsync() && await Package.GetIgnoredUpdatesVersionAsync() != "*")
                await Package.RemoveFromIgnoredUpdatesAsync();
        }

        protected override void Initialize()
        {
            Metadata.OperationInformation = "Package update operation for Package=" + Package.Id + " with Manager=" +
                                            Package.Manager.Name + "\nInstallation options: " + Options.ToString()
                                            + "\nOverriden options: " + Package.OverridenOptions.ToString();

            Metadata.Title = CoreTools.Translate("{package} Update", new Dictionary<string, object?> { { "package", Package.Name } });
            Metadata.Status = CoreTools.Translate("{0} is being updated to version {1}", Package.Name, Package.NewVersionString);
            Metadata.SuccessTitle = CoreTools.Translate("Update succeeded");
            Metadata.SuccessMessage = CoreTools.Translate("{package} was updated successfully",  new Dictionary<string, object?> { { "package", Package.Name } });
            Metadata.FailureTitle = CoreTools.Translate("Update failed", new Dictionary<string, object?> { { "package", Package.Name } });
            Metadata.FailureMessage = CoreTools.Translate("{package} could not be updated", new Dictionary<string, object?> { { "package", Package.Name } });

            if (Settings.Get(Settings.K.AskToDeleteNewDesktopShortcuts))
            {
                DesktopShortcutsBeforeStart = DesktopShortcutsDatabase.GetShortcutsOnDisk();
            }
        }
    }

    public class UninstallPackageOperation : PackageOperation
    {

        public UninstallPackageOperation(
            IPackage package,
            InstallOptions options,
            bool IgnoreParallelInstalls = false,
            AbstractOperation? req = null)
            : base(package, options, OperationType.Uninstall, IgnoreParallelInstalls, req)
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
                                            Package.Manager.Name + "\nInstallation options: " + Options.ToString()
                                            + "\nOverriden options: " + Package.OverridenOptions.ToString();

            Metadata.Title = CoreTools.Translate("{package} Uninstall", new Dictionary<string, object?> { { "package", Package.Name } });
            Metadata.Status = CoreTools.Translate("{0} is being uninstalled", Package.Name);
            Metadata.SuccessTitle = CoreTools.Translate("Uninstall succeeded");
            Metadata.SuccessMessage = CoreTools.Translate("{package} was uninstalled successfully",  new Dictionary<string, object?> { { "package", Package.Name } });
            Metadata.FailureTitle = CoreTools.Translate("Uninstall failed", new Dictionary<string, object?> { { "package", Package.Name } });
            Metadata.FailureMessage = CoreTools.Translate("{package} could not be uninstalled", new Dictionary<string, object?> { { "package", Package.Name } });
        }
    }
}
