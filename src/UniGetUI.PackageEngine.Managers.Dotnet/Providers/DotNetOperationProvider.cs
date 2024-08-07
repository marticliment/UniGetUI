using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Managers.DotNetManager;
using UniGetUI.PackageEngine.PackageClasses;
using Windows.ApplicationModel.Appointments;

namespace UniGetUI.PackageEngine.Managers.DotNetManager;
internal sealed class DotNetOperationProvider : BaseOperationProvider<DotNet>
{
    public DotNetOperationProvider(DotNet manager) : base(manager) { }

    public override IEnumerable<string> GetOperationParameters(
        IPackage package,
        IInstallationOptions options,
        OperationType operation)
    {
        List<string> parameters = [operation switch {
            OperationType.Install => Manager.Properties.InstallVerb,
            OperationType.Update => Manager.Properties.UpdateVerb,
            OperationType.Uninstall => Manager.Properties.UninstallVerb,
            _ => throw new InvalidDataException("Invalid package operation")
        }];
        parameters.Add(package.Id);

        if (options.CustomParameters != null)
            parameters.AddRange(options.CustomParameters);

        if (options.CustomInstallLocation != "")
            parameters.AddRange(["--tool-path", "\"" + options.CustomInstallLocation + "\""]);

        if (options.InstallationScope == PackageScope.Global || (options.InstallationScope is null && package.Scope == PackageScope.Global))
            parameters.Add("--global");

        if (operation is OperationType.Install or OperationType.Update)
        {
            parameters.AddRange(options.Architecture switch
            {
                Architecture.X86 => ["--arch", "x86"],
                Architecture.X64 => ["--arch", "x64"],
                Architecture.Arm => ["--arch", "arm32"],
                Architecture.Arm64 => ["--arch", "arm64"],
                _ => []
            });
        }

        return parameters;
    }

    public override OperationVeredict GetOperationResult(
        IPackage package,
        IInstallationOptions options,
        OperationType operation,
        IEnumerable<string> processOutput,
        int returnCode)
    {
        return returnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
    }
}
