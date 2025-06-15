using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Serializable;

namespace UniGetUI.PackageEngine.Managers.PowerShellManager;
internal sealed class PowerShellPkgOperationHelper : BasePkgOperationHelper
{
    public PowerShellPkgOperationHelper(PowerShell manager) : base(manager) { }

    protected override IReadOnlyList<string> _getOperationParameters(IPackage package,
        InstallOptions options, OperationType operation)
    {
        List<string> parameters = [operation switch {
            OperationType.Install => Manager.Properties.InstallVerb,
            OperationType.Update => Manager.Properties.UpdateVerb,
            OperationType.Uninstall => Manager.Properties.UninstallVerb,
            _ => throw new InvalidDataException("Invalid package operation")
        }];
        parameters.AddRange(["-Name", package.Id, "-Confirm:$false", "-Force"]);

        if (operation is not OperationType.Uninstall)
        {
            if (options.PreRelease)
                parameters.Add("-AllowPrerelease");

            if (!package.OverridenOptions.PowerShell_DoNotSetScopeParameter)
            {
                if (package.OverridenOptions.Scope == PackageScope.Global ||
                    (package.OverridenOptions.Scope is null && options.InstallationScope == PackageScope.Global))
                    parameters.AddRange(["-Scope", "AllUsers"]);
                else
                    parameters.AddRange(["-Scope", "CurrentUser"]);
            }
        }

        if (operation is OperationType.Install)
        {
            if (options.SkipHashCheck)
                parameters.Add("-SkipPublisherCheck");

            if (options.Version != "")
                parameters.AddRange(["-RequiredVersion", options.Version]);
        }

        parameters.AddRange(operation switch
        {
            OperationType.Update => options.CustomParameters_Update,
            OperationType.Uninstall => options.CustomParameters_Uninstall,
            _ => options.CustomParameters_Install,
        });

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

        if (output_string.Contains("-Scope") && output_string.Contains("NamedParameterNotFound") && !package.OverridenOptions.PowerShell_DoNotSetScopeParameter)
        {
            package.OverridenOptions.PowerShell_DoNotSetScopeParameter = true;
            return OperationVeredict.AutoRetry;
        }

        return returnCode == 0 ? OperationVeredict.Success : OperationVeredict.Failure;
    }
}
