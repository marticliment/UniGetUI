using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.PowerShell7Manager;
internal sealed class PowerShell7OperationProvider : BaseOperationProvider<PowerShell7>
{
    public PowerShell7OperationProvider(PowerShell7 manager) : base(manager) { }

    public override IEnumerable<string> GetOperationParameters(IPackage package, IInstallationOptions options, OperationType operation)
    {
        List<string> parameters = [operation switch
        {
            OperationType.Install => Manager.Properties.InstallVerb,
            OperationType.Update => Manager.Properties.UpdateVerb,
            OperationType.Uninstall => Manager.Properties.UninstallVerb,
            _ => throw new InvalidDataException("Invalid package operation")
        }];
        parameters.AddRange(["-Name", package.Id, "-Confirm:$false", "-Force"]);

        if (options.CustomParameters != null)
            parameters.AddRange(options.CustomParameters);

        if (operation is not OperationType.Uninstall)
        {
            if (options.PreRelease)
                parameters.Add("-AllowPrerelease");

            if (options.InstallationScope == PackageScope.Global ||
                (options.InstallationScope is null && package.Scope == PackageScope.Global))
                parameters.AddRange(["-Scope", "AllUsers"]);
            else
                parameters.AddRange(["-Scope", "CurrentUser"]);
        }

        if (operation is OperationType.Install)
        {
            if (options.SkipHashCheck)
                parameters.Add("-SkipPublisherCheck");

            if (options.Version != "")
                parameters.AddRange(["-RequiredVersion", options.Version]);
        }

        return parameters;
    }

    public override OperationVeredict GetOperationResult(IPackage package, IInstallationOptions options, OperationType operation, IEnumerable<string> processOutput, int returnCode)
    {
        string output_string = string.Join("\n", processOutput);

        if (output_string.Contains("AdminPrivilegesAreRequired") && !options.RunAsAdministrator)
        {
            options.RunAsAdministrator = true;
            return OperationVeredict.AutoRetry;
        }

        return returnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
    }
}
