using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Managers.PowerShell7Manager;
internal sealed class PowerShell7PkgOperationHelper : PackagePkgOperationHelper
{
    public PowerShell7PkgOperationHelper(PowerShell7 manager) : base(manager) { }

    protected override IReadOnlyList<string> _getOperationParameters(IPackage package, IInstallationOptions options, OperationType operation)
    {
        List<string> parameters = [operation switch {
            OperationType.Install => Manager.Properties.InstallVerb,
            OperationType.Update => Manager.Properties.UpdateVerb,
            OperationType.Uninstall => Manager.Properties.UninstallVerb,
            _ => throw new InvalidDataException("Invalid package operation")
        }];
        parameters.AddRange(["-Name", package.Id, "-Confirm:$false"]);
        if (operation is OperationType.Update) parameters.Add("-Force");

        if (options.CustomParameters is not null)
            parameters.AddRange(options.CustomParameters);

        if (operation is not OperationType.Uninstall)
        {
            parameters.AddRange(["-TrustRepository", "-AcceptLicense"]);

            if (options.PreRelease)
                parameters.Add("-Prerelease");

            if (package.OverridenOptions.Scope == PackageScope.Global ||
                (package.OverridenOptions.Scope is null && options.InstallationScope == PackageScope.Global))
                parameters.AddRange(["-Scope", "AllUsers"]);
            else
                parameters.AddRange(["-Scope", "CurrentUser"]);
        }

        if (operation is OperationType.Install)
        {
            if (options.Version != "")
                parameters.AddRange(["-Version", options.Version]);
        }

        return parameters;
    }

    protected override OperationVeredict _getOperationResult(
        IPackage package,
        OperationType operation,
        IReadOnlyList<string> processOutput,
        int returnCode)
    {
        string output_string = string.Join("\n", processOutput);

        if (package.OverridenOptions.RunAsAdministrator is not true &&
            (output_string.Contains("AdminPrivilegesAreRequired") || output_string.Contains("AdminPrivilegeRequired")))
        {
            package.OverridenOptions.RunAsAdministrator = true;
            return OperationVeredict.AutoRetry;
        }

        return returnCode == 0 ? OperationVeredict.Success : OperationVeredict.Failure;
    }
}
