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
        throw new NotImplementedException();
    }

    public override OperationVeredict GetOperationResult(
        IPackage package,
        OperationType operation,
        IEnumerable<string> processOutput,
        int returnCode)
    {
        if (returnCode == 0)
        {
            return OperationVeredict.Succeeded;
        }

        return OperationVeredict.Failed;
    }
}
