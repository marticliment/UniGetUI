using System.Runtime.InteropServices;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Managers.VcpkgManager;
internal sealed class VcpkgOperationProvider : BaseOperationProvider<Vcpkg>
{
    public VcpkgOperationProvider(Vcpkg manager) : base(manager) { }

    public override IEnumerable<string> GetOperationParameters(IPackage package, IInstallationOptions options, OperationType operation)
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

    public override OperationVeredict GetOperationResult(
        IPackage package,
        OperationType operation,
        IEnumerable<string> processOutput,
        int returnCode)
    {
        return returnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
    }
}
