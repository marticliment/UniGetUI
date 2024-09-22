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
                async () => (await CoreTools.WhichAsync("cargo-install-update.exe")).Item1),
        ];

        Capabilities = new ManagerCapabilities { };

        var cratesIo = new ManagerSource(this, "crates.io", new Uri("https://index.crates.io/"));

        Properties = new ManagerProperties
        {
            Name = "Cargo",
            Description = CoreTools.Translate("The Rust package manager.<br>Contains: <b>Rust libraries and programs written in Rust</b>"),
            IconId = IconType.Rust,
            ColorIconId = "cargo_color",
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

    protected override IEnumerable<Package> FindPackages_UnSafe(string query)
    {
        Process p = GetProcess(Status.ExecutablePath, "search -q --color=never " + query);
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
                var versionInfo = CratesIOClient.GetManifestVersion(package.Id, package.Version);
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

    protected override IEnumerable<Package> GetAvailableUpdates_UnSafe()
    {
        return GetPackages(LoggableTaskType.ListUpdates);
    }

    protected override IEnumerable<Package> GetInstalledPackages_UnSafe()
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

        Process p = GetProcess(executablePath, "--version");
        p.Start();
        string version = p.StandardOutput.ReadToEnd().Trim();
        string error = p.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(error))
        {
            Logger.Error("cargo version error: " + error);
        }

        return new() { ExecutablePath = executablePath, Found = found, Version = version };
    }

    private IEnumerable<Package> GetPackages(LoggableTaskType taskType)
    {
        List<Package> Packages = [];

        Process p = GetProcess(Status.ExecutablePath, "install-update --list");
        IProcessTaskLogger logger = TaskLogger.CreateNew(taskType, p);
        p.Start();

        string? line;
        while ((line = p.StandardOutput.ReadLine()) is not null)
        {
            logger.AddToStdOut(line);
            var match = UpdateLineRegex().Match(line);
            if (match.Success)
            {
                var id = match.Groups[1].Value.Trim();
                var name = CoreTools.FormatAsName(id);
                var oldVersion = match.Groups[2].Value;
                var newVersion = match.Groups[3].Value;
                if(taskType is LoggableTaskType.ListUpdates && oldVersion != newVersion)
                    Packages.Add(new Package(name, id, oldVersion, newVersion, DefaultSource, this));
                else if(taskType is LoggableTaskType.ListInstalledPackages)
                    Packages.Add(new Package(name, id, oldVersion, DefaultSource, this));
            }
        }
        logger.AddToStdErr(p.StandardError.ReadToEnd());
        p.WaitForExit();
        logger.Close(p.ExitCode);
        return Packages;
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
