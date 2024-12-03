using System.Runtime.InteropServices;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Managers.ScoopManager;
internal sealed class ScoopPkgOperationHelper : PackagePkgOperationHelper
{
    public ScoopPkgOperationHelper(Scoop manager) : base(manager) { }

    protected override IEnumerable<string> _getOperationParameters(IPackage package, IInstallationOptions options, OperationType operation)
    {
        List<string> parameters = [operation switch {
            OperationType.Install => Manager.Properties.InstallVerb,
            OperationType.Update => Manager.Properties.UpdateVerb,
            OperationType.Uninstall => Manager.Properties.UninstallVerb,
            _ => throw new InvalidDataException("Invalid package operation")
        }];

        if(package.Source.Name.Contains("..."))
            parameters.Add($"{package.Id}");
        else
            parameters.Add($"{package.Source.Name}/{package.Id}");

        if (package.OverridenOptions.Scope == PackageScope.Global ||
            (package.OverridenOptions.Scope is null && options.InstallationScope == PackageScope.Global))
        {
            // Scoop requires admin rights to install global packages
            package.OverridenOptions.RunAsAdministrator = true;
            parameters.Add("--global");
        }

        if(options.CustomParameters?.Any() is true)
            parameters.AddRange(options.CustomParameters);

        if (operation is OperationType.Uninstall)
        {
            if (options.RemoveDataOnUninstall)
                parameters.Add("--purge");
        }
        else
        {
            if (options.SkipHashCheck)
                parameters.Add("--skip");

            parameters.AddRange(options.Architecture switch
            {
                Architecture.X64 => ["--arch", "64bit"],
                Architecture.X86 => ["--arch", "32bit"],
                Architecture.Arm64 => ["--arch", "arm64"],
                _ => []
            });
        }

        return parameters;
    }

    protected override OperationVeredict _getOperationResult(
        IPackage package,
        OperationType operation,
        IEnumerable<string> processOutput,
        int returnCode)
    {
        string output_string = string.Join("\n", processOutput);
        if (package.OverridenOptions.Scope != PackageScope.Global && output_string.Contains("Try again with the --global (or -g) flag instead"))
        {
            package.OverridenOptions.Scope = PackageScope.Global;
            package.OverridenOptions.RunAsAdministrator = true;
            return OperationVeredict.AutoRetry;
        }

        if (package.OverridenOptions.RunAsAdministrator != true
            && (output_string.Contains("requires admin rights")
                || output_string.Contains("requires administrator rights")
                || output_string.Contains("you need admin rights to install global apps")))
        {
            package.OverridenOptions.RunAsAdministrator = true;
            return OperationVeredict.AutoRetry;
        }

        if (operation is OperationType.Uninstall)
        {
            if (output_string.Contains("was uninstalled"))
                return OperationVeredict.Succeeded;

            return OperationVeredict.Failed;
        }
        else
        {
            if (output_string.Contains("ERROR"))
                return OperationVeredict.Failed;

            return OperationVeredict.Succeeded;
        }
    }
}
