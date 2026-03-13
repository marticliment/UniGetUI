using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Managers.HomebrewManager;
using UniGetUI.PackageEngine.Serializable;

namespace UniGetUI.PackageEngine.Managers.HomebrewManager.Helpers
{
    internal sealed class HomebrewPkgOperationHelper(Homebrew homebrew) : BasePkgOperationHelper(homebrew)
    {
        protected override IReadOnlyList<string> _getOperationParameters(
            IPackage package,
            InstallOptions options,
            OperationType operation
        )
        {
            List<string> parameters = operation switch
            {
                OperationType.Install => ["install", package.Id],
                OperationType.Update => ["upgrade", package.Id],
                OperationType.Uninstall => ["uninstall", package.Id],
                _ => throw new InvalidDataException("Invalid package operation"),
            };

            // Add custom parameters
            var customParams = operation switch
            {
                OperationType.Install => options.CustomParameters_Install,
                OperationType.Update => options.CustomParameters_Update,
                OperationType.Uninstall => options.CustomParameters_Uninstall,
                _ => [],
            };
            if (customParams != null)
                parameters.AddRange(customParams);

            return parameters;
        }

        protected override OperationVeredict _getOperationResult(
            IPackage package,
            OperationType operation,
            IReadOnlyList<string> processOutput,
            int returnCode
        )
        {
            return returnCode == 0 ? OperationVeredict.Success : OperationVeredict.Failure;
        }
    }
}
