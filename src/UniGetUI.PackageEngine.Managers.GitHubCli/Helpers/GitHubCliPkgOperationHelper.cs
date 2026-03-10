using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json.Nodes;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Serializable;

namespace UniGetUI.PackageEngine.Managers.GitHubCliManager;

internal sealed class GitHubCliPkgOperationHelper : BasePkgOperationHelper
{
    private readonly GitHubCli _manager;
    private readonly ConcurrentDictionary<string, InstallerExecutionContext> _installerContexts = new(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<int> SuccessfulInstallerExitCodes = [0, 3010, 1641];
    private static readonly HashSet<int> RetryAsAdminExitCodes = [5, 740, 1925];
    private static readonly HashSet<int> CanceledInstallerExitCodes = [1223, 1602];

    private sealed class InstallerExecutionContext
    {
        public string? DownloadedAssetName { get; init; }
        public required string DownloadDirectory { get; init; }
        public required bool AutoInstallAfterDownload { get; init; }
        public required bool RunAsAdministrator { get; init; }
        public required bool InteractiveInstallation { get; init; }
    }

    private enum InstallerLaunchResult
    {
        Success,
        Failure,
        Canceled,
        RetryAsAdmin
    }

    public GitHubCliPkgOperationHelper(GitHubCli manager) : base(manager)
    {
        _manager = manager;
    }

    protected override IReadOnlyList<string> _getOperationParameters(
        IPackage package,
        InstallOptions options,
        OperationType operation)
    {
        if (!GitHubCli.IsValidRepositoryId(package.Id))
            throw new InvalidDataException($"Repository id \"{package.Id}\" is not valid");

        string contextKey = GetContextKey(package.Id, operation);
        List<string> parameters;

        if (operation is OperationType.Install or OperationType.Update)
        {
            JsonObject? release = _manager.GetLatestReleaseInfo(package.Id, LoggableTaskType.OtherTask);
            string? autoInstallableAssetName = TryGetAutoInstallableAssetName(release);
            string? selectedAssetName = autoInstallableAssetName ?? TryGetPreferredAssetName(release);

            bool autoInstallAfterDownload = !string.IsNullOrWhiteSpace(autoInstallableAssetName);
            string downloadDirectory = autoInstallAfterDownload
                ? GitHubCli.GetDownloadDirectory(package.Id)
                : GitHubCli.GetDefaultDownloadDirectory();

            parameters = BuildDownloadCommand(package.Id, selectedAssetName, downloadDirectory);

            _installerContexts[contextKey] = new InstallerExecutionContext
            {
                DownloadedAssetName = selectedAssetName,
                DownloadDirectory = downloadDirectory,
                AutoInstallAfterDownload = autoInstallAfterDownload,
                RunAsAdministrator = package.OverridenOptions.RunAsAdministrator is true || options.RunAsAdministrator,
                InteractiveInstallation = options.InteractiveInstallation
            };
        }
        else if (operation is OperationType.Uninstall)
        {
            _installerContexts.TryRemove(contextKey, out _);
            parameters = ["api", "--method", "DELETE", $"/repos/{package.Id}/subscription"];
        }
        else
        {
            throw new InvalidDataException("Invalid package operation");
        }

        parameters.AddRange(operation switch
        {
            OperationType.Update => options.CustomParameters_Update,
            OperationType.Uninstall => options.CustomParameters_Uninstall,
            _ => options.CustomParameters_Install
        });

        return parameters;
    }

    private static string GetContextKey(string repositoryId, OperationType operation)
        => $"{operation}:{repositoryId}";

    private static string? TryGetAutoInstallableAssetName(JsonObject? release)
    {
        if (release is null)
            return null;

        JsonObject? asset = GitHubCliPkgDetailsHelper.SelectBestAssetFromRelease(
            release,
            autoInstallableOnly: true);
        return asset?["name"]?.ToString();
    }

    private static string? TryGetPreferredAssetName(JsonObject? release)
    {
        if (release is null)
            return null;

        JsonObject? asset = GitHubCliPkgDetailsHelper.SelectBestAssetFromRelease(
            release,
            autoInstallableOnly: false);
        return asset?["name"]?.ToString();
    }

    private static List<string> BuildDownloadCommand(string repositoryId, string? assetName, string downloadDirectory)
    {
        Directory.CreateDirectory(downloadDirectory);

        List<string> command =
        [
            "release",
            "download",
            "--repo",
            repositoryId,
            "--clobber",
            "--dir",
            $"\"{downloadDirectory}\""
        ];

        if (!string.IsNullOrWhiteSpace(assetName))
        {
            string sanitizedName = assetName.Replace("\"", "");
            command.AddRange(["--pattern", $"\"{sanitizedName}\""]);
        }
        else
        {
            // Repository has a release but no named assets. Download release source archive.
            command.AddRange(["--archive", "zip"]);
        }

        return command;
    }

    private static string? GetDownloadedInstallerPath(string downloadDirectory, string expectedAssetName)
    {
        if (!Directory.Exists(downloadDirectory))
            return null;

        string expectedPath = Path.Join(downloadDirectory, expectedAssetName);
        return File.Exists(expectedPath) ? expectedPath : null;
    }

    private static ProcessStartInfo BuildInstallerStartInfo(
        string installerPath,
        string extension,
        InstallerExecutionContext context)
    {
        ProcessStartInfo startInfo;

        if (extension == ".msi")
        {
            string args = context.InteractiveInstallation
                ? $"/i \"{installerPath}\""
                : $"/i \"{installerPath}\" /qn /norestart";

            startInfo = new ProcessStartInfo
            {
                FileName = "msiexec.exe",
                Arguments = args,
            };
        }
        else
        {
            startInfo = new ProcessStartInfo
            {
                FileName = installerPath,
            };
        }

        startInfo.UseShellExecute = true;
        startInfo.CreateNoWindow = true;
        startInfo.WorkingDirectory = Path.GetDirectoryName(installerPath)
            ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (context.RunAsAdministrator)
            startInfo.Verb = "runas";

        return startInfo;
    }

    private static InstallerLaunchResult LaunchInstaller(
        IPackage package,
        InstallerExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(context.DownloadedAssetName))
            return InstallerLaunchResult.Failure;

        string? installerPath = GetDownloadedInstallerPath(context.DownloadDirectory, context.DownloadedAssetName);
        if (string.IsNullOrWhiteSpace(installerPath))
        {
            Logger.Warn($"Downloaded release asset {context.DownloadedAssetName} was not found for {package.Id}");
            return InstallerLaunchResult.Failure;
        }

        string extension = Path.GetExtension(installerPath).ToLowerInvariant();
        ProcessStartInfo startInfo = BuildInstallerStartInfo(installerPath, extension, context);
        Logger.Info($"Launching installer for package {package.Id}: {startInfo.FileName} {startInfo.Arguments}".Trim());

        try
        {
            using Process installerProcess = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Failed to start installer process for {package.Id}");

            installerProcess.WaitForExit();
            int exitCode = installerProcess.ExitCode;
            Logger.Info($"Installer process for {package.Id} exited with code {exitCode}");

            if (SuccessfulInstallerExitCodes.Contains(exitCode))
                return InstallerLaunchResult.Success;

            if (CanceledInstallerExitCodes.Contains(exitCode))
                return InstallerLaunchResult.Canceled;

            if (!context.RunAsAdministrator && RetryAsAdminExitCodes.Contains(exitCode))
                return InstallerLaunchResult.RetryAsAdmin;

            return InstallerLaunchResult.Failure;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            Logger.Warn($"Installer launch for {package.Id} was canceled by the user");
            return InstallerLaunchResult.Canceled;
        }
        catch (Win32Exception ex) when (!context.RunAsAdministrator && ex.NativeErrorCode == 740)
        {
            Logger.Warn($"Installer for {package.Id} requires elevation");
            return InstallerLaunchResult.RetryAsAdmin;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to launch installer for package {package.Id}");
            Logger.Error(ex);
            return InstallerLaunchResult.Failure;
        }
    }

    protected override OperationVeredict _getOperationResult(
        IPackage package,
        OperationType operation,
        IReadOnlyList<string> processOutput,
        int returnCode)
    {
        string contextKey = GetContextKey(package.Id, operation);

        if (operation is OperationType.Uninstall)
        {
            _installerContexts.TryRemove(contextKey, out _);
            bool removedTrackedRepository = GitHubCli.RemoveTrackedRepository(package.Id);
            return returnCode == 0 || removedTrackedRepository
                ? OperationVeredict.Success
                : OperationVeredict.Failure;
        }

        _installerContexts.TryRemove(contextKey, out InstallerExecutionContext? context);

        if (returnCode != 0)
            return OperationVeredict.Failure;

        if (context is not null && context.AutoInstallAfterDownload)
        {
            InstallerLaunchResult installerResult = LaunchInstaller(package, context);
            if (installerResult is InstallerLaunchResult.Canceled)
                return OperationVeredict.Canceled;

            if (installerResult is InstallerLaunchResult.RetryAsAdmin)
            {
                package.OverridenOptions.RunAsAdministrator = true;
                return OperationVeredict.AutoRetry;
            }

            if (installerResult is InstallerLaunchResult.Failure)
                return OperationVeredict.Failure;
        }

        string? latestVersion = _manager.GetLatestReleaseTag(package.Id, LoggableTaskType.OtherTask);
        if (string.IsNullOrWhiteSpace(latestVersion))
        {
            latestVersion = operation is OperationType.Update && package.IsUpgradable
                ? package.NewVersionString
                : package.VersionString;
        }

        GitHubCli.TrackRepository(package.Id, latestVersion);
        return OperationVeredict.Success;
    }
}
