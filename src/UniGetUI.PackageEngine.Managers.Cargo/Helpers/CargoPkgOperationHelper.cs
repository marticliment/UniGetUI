using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Serializable;

namespace UniGetUI.PackageEngine.Managers.CargoManager;

internal sealed class CargoPkgOperationHelper(Cargo cargo) : BasePkgOperationHelper(cargo)
{
    protected override IReadOnlyList<string> _getOperationParameters(IPackage package,
        InstallOptions options, OperationType operation)
    {
        var version = options.Version == string.Empty ? package.VersionString : options.Version;
        List<string> parameters = operation switch
        {
            OperationType.Install => [Manager.Properties.InstallVerb, "--version", version, package.Id],
            OperationType.Update => [Manager.Properties.UpdateVerb, package.Id],
            OperationType.Uninstall => [Manager.Properties.UninstallVerb, package.Id],
            _ => throw new InvalidDataException("Invalid package operation"),
        };

        if (operation is OperationType.Install or OperationType.Update)
        {
            parameters.Add("--no-confirm");

            if(options.SkipHashCheck)
                parameters.Add("--skip-signatures");

            if(options.CustomInstallLocation != "")
                parameters.AddRange(["--install-path", options.CustomInstallLocation]);
        }

        parameters.AddRange(operation switch
        {
            OperationType.Update => options.CustomParameters_Update,
            OperationType.Uninstall => options.CustomParameters_Uninstall,
            _ => options.CustomParameters_Install,
        });

        return parameters;
    }

    protected override OperationVeredict _getOperationResult(IPackage package, OperationType operation, IReadOnlyList<string> processOutput, int returnCode)
    {
        return returnCode == 0 ? OperationVeredict.Success : OperationVeredict.Failure;
    }
}
