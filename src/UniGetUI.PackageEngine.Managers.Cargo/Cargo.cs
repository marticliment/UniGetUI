using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
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
    public static new string[] FALSE_PACKAGE_NAMES = [""];
    public static new string[] FALSE_PACKAGE_IDS = [""];
    public static new string[] FALSE_PACKAGE_VERSIONS = [""];

    [GeneratedRegex(@"(\w+)\s=\s""(\d+\.\d+\.\d+)""\s*#\s(.*)")]
    private static partial Regex SearchLineRegex();

    [GeneratedRegex(@"(.+)v(\d+\.\d+\.\d+)\s*v(\d+\.\d+\.\d+)\s*(Yes|No)")]
    private static partial Regex UpdateLineRegex();

    public Cargo()
    {
        Dependencies = [
            // cargo-update is required to check for and update installed packages
            new ManagerDependency(
                "cargo-update",
                Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe"),
                "-ExecutionPolicy Bypass -NoLogo -NoProfile -Command \"& {cargo install cargo-update; if($error.count -ne 0){pause}}\"",
                "cargo install cargo-update",
                async () => (await CoreTools.Which("cargo-install-update.exe")).Item1),
        ];

        Capabilities = new ManagerCapabilities { };

        var cratesIo = new ManagerSource(this, "crates.io", new Uri("https://index.crates.io/"));

        Properties = new ManagerProperties
        {
            Name = "Cargo",
            Description = CoreTools.Translate("The Rust package manager.<br>Contains: <b>Rust libraries and programs written in Rust</b>"),
            IconId = IconType.Rust,
            ColorIconId = "rust_color",
            ExecutableFriendlyName = "cargo.exe",
            InstallVerb = "install",
            UninstallVerb = "uninstall",
            UpdateVerb = "install-update",
            ExecutableCallArgs = "",
            DefaultSource = cratesIo,
            KnownSources = [cratesIo]
        };

        PackageDetailsProvider = new CargoPackageDetailsProvider(this);
        OperationProvider = new CargoOperationProvider(this);
    }

    protected override async Task<Package[]> FindPackages_UnSafe(string query)
    {
        Process p = GetProcess(Status.ExecutablePath, "search -q --color=never " + query);
        IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.FindPackages, p);
        p.Start();

        string? line;
        List<Package> Packages = [];
        while ((line = await p.StandardOutput.ReadLineAsync()) != null)
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

        logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
        await p.WaitForExitAsync();
        logger.Close(p.ExitCode);

        return Packages.ToArray();
    }
    
    protected override async Task<Package[]> GetAvailableUpdates_UnSafe()
    {
        return await GetPackages(LoggableTaskType.ListUpdates);
    }

    protected override async Task<Package[]> GetInstalledPackages_UnSafe()
    {
        return await GetPackages(LoggableTaskType.ListInstalledPackages);
    }

    protected override async Task<ManagerStatus> LoadManager()
    {
        var (found, executablePath) = await CoreTools.Which("cargo");
        Process p = GetProcess(executablePath, "--version");
        p.Start();
        string version = (await p.StandardOutput.ReadToEndAsync()).Trim();
        string error = await p.StandardError.ReadToEndAsync();
        if (!string.IsNullOrEmpty(error))
        {
            Logger.Error("cargo version error: " + error);
        }

        return new() { ExecutablePath = executablePath, Found = found, Version = version };
    }

    private async Task<Package[]> GetPackages(LoggableTaskType taskType)
    {
        List<Package> Packages = [];

        Process p = GetProcess(Status.ExecutablePath, "install-update --list");
        IProcessTaskLogger logger = TaskLogger.CreateNew(taskType, p);
        p.Start();

        string? line;
        while ((line = await p.StandardOutput.ReadLineAsync()) != null)
        {
            logger.AddToStdOut(line);
            var match = UpdateLineRegex().Match(line);
            if (match.Success)
            {
                var id = match.Groups[1].Value.Trim();
                var name = CoreTools.FormatAsName(id);
                var oldVersion = match.Groups[2].Value;
                var newVersion = match.Groups[3].Value;
                Packages.Add(new Package(name, id, oldVersion, newVersion, DefaultSource, this));
            }
        }
        logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
        await p.WaitForExitAsync();
        logger.Close(p.ExitCode);
        return Packages.ToArray();
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
