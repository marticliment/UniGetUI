using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using UniGetUI.Core.Classes;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager.Classes;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.ManagerClasses.Classes;

namespace UniGetUI.PackageEngine.Managers.CargoManager;

public partial class Cargo : PackageManager
{
    [GeneratedRegex(@"(\w+)\s=\s""(\d+\.\d+\.\d+)""\s*#\s(.*)")]
    private static partial Regex SearchLineRegex();

    [GeneratedRegex(@"(.+)v(\d+\.\d+\.\d+)\s*v(\d+\.\d+\.\d+)\s*(Yes|No)")]
    private static partial Regex UpdateLineRegex();

    public Cargo()
    {
        Dependencies = [
            // cargo-update is required to check for installed and upgradable packages
            new ManagerDependency(
                "cargo-update",
                Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe"),
                "-ExecutionPolicy Bypass -NoLogo -NoProfile -Command \"& {cargo install cargo-update; if ($error.count -ne 0){pause}}\"",
                "cargo install cargo-update",
                async () => (await CoreTools.WhichAsync("cargo-install-update.exe")).Item1),
            // Cargo-binstall is required to install and update cargo binaries
            new ManagerDependency(
                "cargo-binstall",
                Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe"),
                "-ExecutionPolicy Bypass -NoLogo -NoProfile -Command \"& {Set-ExecutionPolicy Unrestricted -Scope Process; iex (iwr \\\"https://raw.githubusercontent.com/cargo-bins/cargo-binstall/main/install-from-binstall-release.ps1\\\").Content; if ($error.count -ne 0){pause}}\"",
                "Set-ExecutionPolicy Unrestricted -Scope Process; iex (iwr \"https://raw.githubusercontent.com/cargo-bins/cargo-binstall/main/install-from-binstall-release.ps1\").Content",
                async () => (await CoreTools.WhichAsync("cargo-binstall.exe")).Item1)
        ];

        Capabilities = new ManagerCapabilities
        {
            CanRunAsAdmin = true,
            CanSkipIntegrityChecks = true,
            SupportsCustomVersions = true,
            SupportsCustomLocations = true,
            SupportsProxy = ProxySupport.Partially,
            SupportsProxyAuth = true
        };

        var cratesIo = new ManagerSource(this, "crates.io", new Uri("https://index.crates.io/"));

        Properties = new ManagerProperties
        {
            Name = "Cargo",
            Description = CoreTools.Translate("The Rust package manager.<br>Contains: <b>Rust libraries and programs written in Rust</b>"),
            IconId = IconType.Rust,
            ColorIconId = "cargo_color",
            ExecutableFriendlyName = "cargo.exe",
            InstallVerb = "binstall",
            UninstallVerb = "uninstall",
            UpdateVerb = "binstall",
            ExecutableCallArgs = "",
            DefaultSource = cratesIo,
            KnownSources = [cratesIo]
        };

        DetailsHelper = new CargoPkgDetailsHelper(this);
        OperationHelper = new CargoPkgOperationHelper(this);
    }

    protected override IReadOnlyList<Package> FindPackages_UnSafe(string query)
    {
        using Process p = GetProcess(Status.ExecutablePath, "search -q --color=never " + query);
        IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.FindPackages, p);
        p.Start();

        string? line;
        List<Package> Packages = [];
        while ((line = p.StandardOutput.ReadLine()) is not null)
        {
            logger.AddToStdOut(line);
            var match = SearchLineRegex().Match(line);
            if (match.Success)
            {
                var id = match.Groups[1].Value;
                var version = match.Groups[2].Value;
                Packages.Add(new Package(CoreTools.FormatAsName(id), id, version, DefaultSource, this));
            }
        }

        logger.AddToStdErr(p.StandardError.ReadToEnd());
        p.WaitForExit();

        List<Package> BinPackages = [];

        for (int i = 0; i < Packages.Count; i++)
        {
            DateTime startTime = DateTime.Now;

            var package = Packages[i];
            try
            {
                var versionInfo = CratesIOClient.GetManifestVersion(package.Id, package.VersionString);
                if (versionInfo.bin_names?.Length > 0)
                {
                    BinPackages.Add(package);
                }
            }
            catch (Exception ex)
            {
                logger.AddToStdErr($"{ex.Message}");
            }

            if (i + 1 == Packages.Count) break;
            // Crates.io api requests that we send no more than one request per second
            Task.Delay(Math.Max(0, 1000 - (int)((DateTime.Now - startTime).TotalMilliseconds))).GetAwaiter().GetResult();
        }

        logger.Close(p.ExitCode);

        return [.. BinPackages];
    }

    protected override IReadOnlyList<Package> GetAvailableUpdates_UnSafe()
    {
        return GetPackages(LoggableTaskType.ListUpdates);
    }

    protected override IReadOnlyList<Package> GetInstalledPackages_UnSafe()
    {
        return GetPackages(LoggableTaskType.ListInstalledPackages);
    }

    protected override ManagerStatus LoadManager()
    {
        var (found, executablePath) = CoreTools.Which("cargo");
        if (!found)
        {
            return new(){ ExecutablePath = executablePath, Found = false, Version = ""};
        }

        using Process p = GetProcess(executablePath, "--version");
        p.Start();
        string version = p.StandardOutput.ReadToEnd().Trim();
        string error = p.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(error))
        {
            Logger.Error("cargo version error: " + error);
        }

        return new() { ExecutablePath = executablePath, Found = found, Version = version };
    }

    private IReadOnlyList<Package> GetPackages(LoggableTaskType taskType)
    {
        List<Package> Packages = [];
        foreach(var match in TaskRecycler<List<Match>>.RunOrAttach(GetInstalledCommandOutput, 15))
        {
            var id = match.Groups[1]?.Value?.Trim() ?? "";
            var name = CoreTools.FormatAsName(id);
            var oldVersion = match.Groups[2]?.Value?.Trim() ?? "";
            var newVersion = match.Groups[3]?.Value?.Trim() ?? "";
            if (taskType is LoggableTaskType.ListUpdates && oldVersion != newVersion)
                Packages.Add(new Package(name, id, oldVersion, newVersion, DefaultSource, this));
            else if (taskType is LoggableTaskType.ListInstalledPackages)
                Packages.Add(new Package(name, id, oldVersion, DefaultSource, this));
        }
        return Packages;
    }

    private List<Match> GetInstalledCommandOutput()
    {
        List<Match> output = [];
        using Process p = GetProcess(Status.ExecutablePath, "install-update --list");
        IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.OtherTask, p);
        logger.AddToStdOut("Other task: Call the install-update command");
        p.Start();

        string? line;
        while ((line = p.StandardOutput.ReadLine()) is not null)
        {
            logger.AddToStdOut(line);
            var match = UpdateLineRegex().Match(line);
            if (match.Success)
            {
                output.Add(match);
            }
        }
        logger.AddToStdErr(p.StandardError.ReadToEnd());
        p.WaitForExit();
        logger.Close(p.ExitCode);
        return output;
    }

    private Process GetProcess(string fileName, string extraArguments)
    {
        return new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = Properties.ExecutableCallArgs + " " + extraArguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            }
        };
    }
}
