using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Managers.CargoManager;

internal sealed class CargoOperationProvider(Cargo cargo) : BaseOperationProvider<Cargo>(cargo)
{
    public override IEnumerable<string> GetOperationParameters(IPackage package, IInstallationOptions options, OperationType operation)
    {
        var version = options.Version == string.Empty ? package.Version : options.Version;
        List<string> parameters = operation switch
        {
            OperationType.Install => [Manager.Properties.InstallVerb, "--version", version, package.Id],
            OperationType.Update => [Manager.Properties.UpdateVerb, package.Id],
            OperationType.Uninstall => [Manager.Properties.UninstallVerb, package.Id],
            _ => throw new InvalidDataException("Invalid package operation"),
        };

        return parameters;
    }

    public override OperationVeredict GetOperationResult(IPackage package, OperationType operation, IEnumerable<string> processOutput, int returnCode)
    {
        return returnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
    }
}
