using System.Runtime.InteropServices;
using Microsoft.Management.Deployment;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Classes.Packages.Classes;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Managers.WingetManager;
internal sealed class WinGetPkgOperationHelper : PackagePkgOperationHelper
{
    public static string GetIdNamePiece(IPackage package)
    {
        if (!package.Id.EndsWith("…"))
            return $"--id \"{package.Id.TrimEnd('…')}\" --exact";

        if (!package.Name.EndsWith("…"))
            return $"--name \"{package.Name}\" --exact";

        return $"--id \"{package.Id.TrimEnd('…')}\"";
    }

    public WinGetPkgOperationHelper(WinGet manager) : base(manager) { }

    protected override IReadOnlyList<string> _getOperationParameters(IPackage package, IInstallationOptions options, OperationType operation)
    {
        List<string> parameters = [operation switch {
            OperationType.Install => Manager.Properties.InstallVerb,
            OperationType.Update => Manager.Properties.UpdateVerb,
            OperationType.Uninstall => Manager.Properties.UninstallVerb,
            _ => throw new InvalidDataException("Invalid package operation")
        }];

        parameters.AddRange(GetIdNamePiece(package).Split(" "));
        parameters.AddRange(["--source", package.Source.IsVirtualManager ? "winget" : package.Source.Name]);
        parameters.AddRange(["--accept-source-agreements", "--disable-interactivity"]);

        // package.OverridenInstallationOptions.Scope is meaningless in WinGet packages. Default is unspecified, hence the _ => [].
        parameters.AddRange((package.OverridenOptions.Scope ?? options.InstallationScope) switch {
            PackageScope.User => ["--scope", "user"],
            PackageScope.Machine => ["--scope", "machine"],
            _ => []
        });

        if (operation is OperationType.Uninstall && package.VersionString != "Unknown" && package.OverridenOptions.WinGet_SpecifyVersion is not false)
        {
            parameters.AddRange(["--version", $"\"{package.VersionString}\""]);
        }
        else if (operation is OperationType.Install && options.Version != "")
        {
            parameters.AddRange(["--version", $"\"{options.Version}\""]);
        }

        parameters.Add(options.InteractiveInstallation ? "--interactive" : "--silent");
        parameters.AddRange(options.CustomParameters);

        if (operation is OperationType.Update)
        {
            if (package.Name.Contains("64-bit") || package.Id.ToLower().Contains("x64"))
            {
                options.Architecture = Architecture.X64;
            }
            else if (package.Name.Contains("32-bit") || package.Id.ToLower().Contains("x86"))
            {
                options.Architecture = Architecture.X86;
            }
            parameters.Add("--include-unknown");
        }

        if (operation is not OperationType.Uninstall)
        {
            parameters.AddRange(["--accept-package-agreements", "--force"]);

            if (options.SkipHashCheck)
                parameters.Add("--ignore-security-hash");

            if (options.CustomInstallLocation != "")
                parameters.AddRange(["--location", $"\"{options.CustomInstallLocation}\""]);

            parameters.AddRange(options.Architecture switch
            {
                Architecture.X86 => ["--architecture", "x86"],
                Architecture.X64 => ["--architecture", "x64"],
                Architecture.Arm64 => ["--architecture", "arm64"],
                _ => []
            });
        }

        try
        {
            var installOptions = NativePackageHandler.GetInstallationOptions(package, options, operation);
            if (installOptions?.ElevationRequirement is ElevationRequirement.ElevationRequired or ElevationRequirement.ElevatesSelf)
            {
                Logger.Info($"WinGet package {package.Id} requires elevation, forcing administrator rights...");
                package.OverridenOptions.RunAsAdministrator = true;
            }
            else if (installOptions?.ElevationRequirement is ElevationRequirement.ElevationProhibited)
            {
                if (CoreTools.IsAdministrator())
                    throw new UnauthorizedAccessException(
                        CoreTools.Translate("This package cannot be installed from an elevated context.")
                        + CoreTools.Translate("Please run UniGetUI as a regular user and try again."));

                if (options.RunAsAdministrator)
                    throw new UnauthorizedAccessException(
                        CoreTools.Translate("This package cannot be installed from an elevated context.")
                        + CoreTools.Translate("Please check the installation options for this package and try again"));

                package.OverridenOptions.RunAsAdministrator = false;
            }
            else if(installOptions?.Scope is PackageInstallerScope.System/* or PackageInstallerScope.Unknown*/)
            {
                Logger.Info($"WinGet package {package.Id} is installed on a system-wide scope, forcing administrator rights...");
                package.OverridenOptions.RunAsAdministrator = true;
            }
        }
        catch (Exception ex)
        {
            if (ex is UnauthorizedAccessException) throw;

            Logger.Error("Recovered from fatal WinGet exception:");
            Logger.Error(ex);
        }

        parameters.Add(WinGet.GetProxyArgument());
        return parameters;
    }

    protected override OperationVeredict _getOperationResult(
        IPackage package,
        OperationType operation,
        IReadOnlyList<string> processOutput,
        int returnCode)
    {
        // See https://github.com/microsoft/winget-cli/blob/master/doc/windows/package-manager/winget/returnCodes.md for reference
        uint uintCode = (uint)returnCode;

        if (uintCode is 0x8A150109)
        {   // TODO: Restart required to finish installation
            if (operation is OperationType.Update) MarkUpgradeAsDone(package);
            return OperationVeredict.Success;
        }

        if (uintCode is 0x8A150077 or 0x8A15010C or 0x8A150005)
        {   // At some point, the user clicked cancel or Ctrl+C
            return OperationVeredict.Canceled;
        }

        if (operation is OperationType.Uninstall && uintCode is 0x8A150017 && package.OverridenOptions.WinGet_SpecifyVersion is not false)
        {   // No manifest found matching criteria
            package.OverridenOptions.WinGet_SpecifyVersion = false;
            return OperationVeredict.AutoRetry;
        }

        if (uintCode is 0x8A150011)
        {   // TODO: Integrity failed
            return OperationVeredict.Failure;
        }

        if (uintCode is 0x8A15002B)
        {
            if (Settings.Get("IgnoreUpdatesNotApplicable"))
            {
                Logger.Warn($"Ignoring update {package.Id} as the update is not applicable to the platform, and the user has enabled IgnoreUpdatesNotApplicable");
                IgnoredUpdatesDatabase.Add(IgnoredUpdatesDatabase.GetIgnoredIdForPackage(package), package.VersionString);
                return OperationVeredict.Success;
            }
            return OperationVeredict.Failure;
        }

        if (uintCode is 0x8A15010D or 0x8A15004F or 0x8A15010E)
        {   // Application is already installed
            if (operation is OperationType.Update) MarkUpgradeAsDone(package);
            return OperationVeredict.Success;
        }

        if (returnCode is 0)
        {   // Operation succeeded
            if (operation is OperationType.Update) MarkUpgradeAsDone(package);
            return OperationVeredict.Success;
        }

        if (uintCode is 0x8A150056 && package.OverridenOptions.RunAsAdministrator is not false && !CoreTools.IsAdministrator())
        {   // Installer can't run elevated, but this condition hasn't been forced on UniGetUI
            package.OverridenOptions.RunAsAdministrator = false;
            return OperationVeredict.AutoRetry;
        }

        if ((uintCode is 0x8A150019 or 0x80073D28) && package.OverridenOptions.RunAsAdministrator is not true)
        {   // Installer needs to run elevated, handle autoelevation
            // Code 0x80073D28 was added after https://github.com/marticliment/UniGetUI/issues/3093
            package.OverridenOptions.RunAsAdministrator = true;
            return OperationVeredict.AutoRetry;
        }

        return OperationVeredict.Failure;
    }

    private static void MarkUpgradeAsDone(IPackage package)
    {
        Settings.SetDictionaryItem<string, string>("WinGetAlreadyUpgradedPackages", package.Id, package.NewVersionString);
    }

    public static bool UpdateAlreadyInstalled(IPackage package)
    {
        return Settings.GetDictionaryItem<string, string>("WinGetAlreadyUpgradedPackages", package.Id) == package.NewVersionString;
    }
}
