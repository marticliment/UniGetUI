using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Managers.VcpkgManager;
internal sealed class VcpkgPkgOperationHelper : PackagePkgOperationHelper
{
    public VcpkgPkgOperationHelper(Vcpkg manager) : base(manager) { }

    protected override IEnumerable<string> _getOperationParameters(IPackage package, IInstallationOptions options, OperationType operation)
    {
        List<string> parameters = operation switch {
            OperationType.Install => [Manager.Properties.InstallVerb, package.Id],
            OperationType.Update => [Manager.Properties.UpdateVerb, package.Id, "--no-dry-run"],
            OperationType.Uninstall => [Manager.Properties.UninstallVerb, package.Id],
            _ => throw new InvalidDataException("Invalid package operation")
        };

        parameters.AddRange(options.CustomParameters);
        return parameters;
    }

    protected override OperationVeredict _getOperationResult(
        IPackage package,
        OperationType operation,
        IEnumerable<string> processOutput,
        int returnCode)
    {
        return returnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
    }
}
