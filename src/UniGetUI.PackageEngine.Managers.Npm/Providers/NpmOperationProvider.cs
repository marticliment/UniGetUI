using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Managers.NpmManager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.NpmManager;
internal sealed class NpmOperationProvider : BaseOperationProvider<Npm>
{
    public NpmOperationProvider(Npm manager) : base(manager) { }

    public override IEnumerable<string> GetOperationParameters(IPackage package, IInstallationOptions options, OperationType operation)
    {
        List<string> parameters = operation switch {
            OperationType.Install => [Manager.Properties.InstallVerb, $"{package.Id}@{(options.Version == string.Empty? package.Version: options.Version)}"],
            OperationType.Update => [Manager.Properties.UpdateVerb, $"{package.Id}@{package.NewVersion}"],
            OperationType.Uninstall => [Manager.Properties.UninstallVerb, package.Id],
            _ => throw new InvalidDataException("Invalid package operation")
        };
        parameters.Add(package.Id);

        if (options.CustomParameters != null)
            parameters.AddRange(options.CustomParameters);

        if (options.InstallationScope == PackageScope.Global || (options.InstallationScope is null && package.Scope == PackageScope.Global))
            parameters.Add("--global");

        if (options.PreRelease)
            parameters.AddRange(["--include", "dev"]);

        return parameters;
    }

    public override OperationVeredict GetOperationResult(IPackage package, IInstallationOptions options, OperationType operation, IEnumerable<string> processOutput, int returnCode)
    {
        return returnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
    }
}
