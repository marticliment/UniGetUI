using System.Runtime.InteropServices;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Managers.DotNetManager;
internal sealed class DotNetPkgOperationHelper : PackagePkgOperationHelper
{
    public DotNetPkgOperationHelper(DotNet manager) : base(manager) { }

    protected override IReadOnlyList<string> _getOperationParameters(
        IPackage package,
        IInstallationOptions options,
        OperationType operation)
    {
        List<string> parameters =
        [
            operation switch
            {
                OperationType.Install => Manager.Properties.InstallVerb,
                OperationType.Update => Manager.Properties.UpdateVerb,
                OperationType.Uninstall => Manager.Properties.UninstallVerb,
                _ => throw new InvalidDataException("Invalid package operation")
            },

            package.Id,

        ];

        if (options.CustomParameters is not null)
            parameters.AddRange(options.CustomParameters);

        if (options.CustomInstallLocation != "")
            parameters.AddRange(["--tool-path", "\"" + options.CustomInstallLocation + "\""]);

        if (package.OverridenOptions.Scope is PackageScope.Global ||
           (package.OverridenOptions.Scope is null && options.InstallationScope is PackageScope.Global))
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

        if (operation is OperationType.Install)
        {
            if (options.Version != "")
            {
                parameters.AddRange(["--version", options.Version]);
            }
        }

        return parameters;
    }

    protected override OperationVeredict _getOperationResult(
        IPackage package,
        OperationType operation,
        IReadOnlyList<string> processOutput,
        int returnCode)
    {
        if (returnCode is not 0 && package.OverridenOptions.Scope is not PackageScope.Global)
        {
            package.OverridenOptions.Scope = PackageScope.Global;
            return OperationVeredict.AutoRetry;
        }

        return returnCode is 0 ? OperationVeredict.Success : OperationVeredict.Failure;
    }
}
