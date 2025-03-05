using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Managers.NpmManager;
internal sealed class NpmPkgOperationHelper : PackagePkgOperationHelper
{
    public NpmPkgOperationHelper(Npm manager) : base(manager) { }

    protected override IReadOnlyList<string> _getOperationParameters(IPackage package, IInstallationOptions options, OperationType operation)
    {
        List<string> parameters = operation switch {
            OperationType.Install => [Manager.Properties.InstallVerb, $"{package.Id}@{(options.Version == string.Empty? package.VersionString: options.Version)}"],
            OperationType.Update => [Manager.Properties.UpdateVerb, $"{package.Id}@{package.NewVersionString}"],
            OperationType.Uninstall => [Manager.Properties.UninstallVerb, package.Id],
            _ => throw new InvalidDataException("Invalid package operation")
        };
        parameters.Add(package.Id);

        if (options.CustomParameters is not null)
            parameters.AddRange(options.CustomParameters);

        if (package.OverridenOptions.Scope == PackageScope.Global ||
            (package.OverridenOptions.Scope is null && options.InstallationScope == PackageScope.Global))
            parameters.Add("--global");

        if (options.PreRelease)
            parameters.AddRange(["--include", "dev"]);

        return parameters;
    }

    protected override OperationVeredict _getOperationResult(
        IPackage package,
        OperationType operation,
        IReadOnlyList<string> processOutput,
        int returnCode)
    {
        return returnCode == 0 ? OperationVeredict.Success : OperationVeredict.Failure;
    }
}
