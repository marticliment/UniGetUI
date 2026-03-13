using System.Diagnostics;
using System.Text.RegularExpressions;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Managers.HomebrewManager.Helpers;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.HomebrewManager
{
    public partial class Homebrew : PackageManager
    {
        public static string[] FALSE_PACKAGE_IDS = ["", "==>", "Warning:", "Error:"];

        public Homebrew()
        {
            Dependencies = [];

            Capabilities = new ManagerCapabilities
            {
                CanRunAsAdmin = false, // Homebrew refuses to run as root - by design
                SupportsCustomVersions = false,
                SupportsCustomScopes = false,
                CanListDependencies = true,
                SupportsCustomSources = false, // TODO: brew tap support later
                SupportsPreRelease = false,
                CanDownloadInstaller = false,
            };

            var homebrewSource = new ManagerSource(this, "homebrew", new Uri("https://formulae.brew.sh/"));

            Properties = new ManagerProperties
            {
                Name = "Homebrew",
                Description = CoreTools.Translate(
                    "The missing package manager for macOS (and Linux). Install CLI tools, libraries, and applications.<br>Contains: <b>Command-line tools, libraries, and GUI applications (casks)</b>"
                ),
                IconId = IconType.Bucket,
                ColorIconId = "homebrew_color",
                ExecutableFriendlyName = "brew",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "upgrade",
                DefaultSource = homebrewSource,
                KnownSources = [homebrewSource],
            };

            DetailsHelper = new HomebrewPkgDetailsHelper(this);
            OperationHelper = new HomebrewPkgOperationHelper(this);
        }

        public override IReadOnlyList<string> FindCandidateExecutableFiles()
        {
            List<string> paths = [];

            // Apple Silicon
            if (File.Exists("/opt/homebrew/bin/brew"))
                paths.Add("/opt/homebrew/bin/brew");

            // Intel Mac
            if (File.Exists("/usr/local/bin/brew"))
                paths.Add("/usr/local/bin/brew");

            // Linux
            if (File.Exists("/home/linuxbrew/.linuxbrew/bin/brew"))
                paths.Add("/home/linuxbrew/.linuxbrew/bin/brew");

            // Fallback to which
            var whichPaths = CoreTools.WhichMultiple("brew");
            foreach (var p in whichPaths)
                if (!paths.Contains(p))
                    paths.Add(p);

            return paths;
        }

        protected override void _loadManagerExecutableFile(
            out bool found,
            out string path,
            out string callArguments
        )
        {
            var (_found, _executablePath) = GetExecutableFile();
            found = _found;
            path = _executablePath;
            callArguments = "";
        }

        protected override void _loadManagerVersion(out string version)
        {
            using Process p = GetProcess(Status.ExecutablePath, "--version");
            p.StartInfo.Environment["HOMEBREW_NO_AUTO_UPDATE"] = "1";
            p.Start();
            version = p.StandardOutput.ReadToEnd().Trim().Split('\n')[0];
            p.WaitForExit();
        }

        protected override IReadOnlyList<Package> FindPackages_UnSafe(string query)
        {
            List<Package> Packages = [];

            using Process p = GetProcess(Status.ExecutablePath, $"search --formula {query}");
            p.StartInfo.Environment["HOMEBREW_NO_AUTO_UPDATE"] = "1";

            INativeTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.FindPackages);

            p.Start();
            var stderrTask = p.StandardError.ReadToEndAsync();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            string stderr = stderrTask.Result;
            if (!string.IsNullOrEmpty(stderr)) Logger.Warn(stderr);
            logger.Close(p.ExitCode);

            foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string name = line.Trim();
                if (string.IsNullOrWhiteSpace(name) || FALSE_PACKAGE_IDS.Contains(name))
                    continue;

                // Search results are just package names, one per line
                Packages.Add(new Package(
                    CoreTools.FormatAsName(name),
                    name,
                    "",  // version not available from search
                    Properties.DefaultSource,
                    this
                ));
            }

            return Packages;
        }

        protected override IReadOnlyList<Package> GetInstalledPackages_UnSafe()
        {
            List<Package> Packages = [];

            // List formulae
            using Process p = GetProcess(Status.ExecutablePath, "list --formula --versions");
            p.StartInfo.Environment["HOMEBREW_NO_AUTO_UPDATE"] = "1";
            p.StartInfo.Environment["HOMEBREW_NO_INSTALL_CLEANUP"] = "1";

            INativeTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListInstalledPackages);

            p.Start();
            var stderrTask = p.StandardError.ReadToEndAsync();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            string stderr = stderrTask.Result;
            if (!string.IsNullOrEmpty(stderr)) Logger.Warn(stderr);
            logger.Close(p.ExitCode);

            // Format: "name version [version2 ...]"
            foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2 || FALSE_PACKAGE_IDS.Contains(parts[0]))
                    continue;

                string name = parts[0];
                string version = parts[1]; // First (most recent) version

                Packages.Add(new Package(
                    CoreTools.FormatAsName(name),
                    name,
                    version,
                    Properties.DefaultSource,
                    this
                ));
            }

            // Also list casks
            try
            {
                using Process pc = GetProcess(Status.ExecutablePath, "list --cask --versions");
                pc.StartInfo.Environment["HOMEBREW_NO_AUTO_UPDATE"] = "1";

                pc.Start();
                string caskOutput = pc.StandardOutput.ReadToEnd();
                pc.WaitForExit();

                foreach (string line in caskOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2 || FALSE_PACKAGE_IDS.Contains(parts[0]))
                        continue;

                    Packages.Add(new Package(
                        CoreTools.FormatAsName(parts[0]),
                        parts[0],
                        parts[1],
                        Properties.DefaultSource,
                        this
                    ));
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to list casks: {ex.Message}");
            }

            return Packages;
        }

        protected override IReadOnlyList<Package> GetAvailableUpdates_UnSafe()
        {
            List<Package> Packages = [];

            // Use --json for reliable parsing
            using Process p = GetProcess(Status.ExecutablePath, "outdated --json");
            p.StartInfo.Environment["HOMEBREW_NO_AUTO_UPDATE"] = "1";
            p.StartInfo.Environment["HOMEBREW_NO_INSTALL_CLEANUP"] = "1";
            p.StartInfo.Environment["HOMEBREW_NO_ENV_HINTS"] = "1";

            INativeTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListUpdates);

            p.Start();
            // Read stderr async to prevent deadlock
            var stderrTask = p.StandardError.ReadToEndAsync();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            string stderr = stderrTask.Result;
            if (!string.IsNullOrEmpty(stderr)) Logger.Warn(stderr);
            logger.Close(p.ExitCode);

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(output);
                var root = doc.RootElement;

                // JSON format: { "formulae": [ { "name": "...", "installed_versions": ["..."], "current_version": "..." } ] }
                if (root.TryGetProperty("formulae", out var formulae))
                {
                    foreach (var item in formulae.EnumerateArray())
                    {
                        string name = item.GetProperty("name").GetString() ?? "";
                        string newVersion = item.GetProperty("current_version").GetString() ?? "";

                        string installedVersion = "";
                        if (item.TryGetProperty("installed_versions", out var versions) && versions.GetArrayLength() > 0)
                            installedVersion = versions[0].GetString() ?? "";

                        if (!string.IsNullOrEmpty(name))
                        {
                            Packages.Add(new Package(
                                CoreTools.FormatAsName(name),
                                name,
                                installedVersion,
                                newVersion,
                                Properties.DefaultSource,
                                this
                            ));
                        }
                    }
                }

                if (root.TryGetProperty("casks", out var casks))
                {
                    foreach (var item in casks.EnumerateArray())
                    {
                        string name = item.GetProperty("name").GetString() ?? "";
                        string newVersion = item.GetProperty("current_version").GetString() ?? "";
                        string installedVersion = item.TryGetProperty("installed_versions", out var cv) && cv.GetArrayLength() > 0
                            ? cv[0].GetString() ?? ""
                            : "";

                        if (!string.IsNullOrEmpty(name))
                        {
                            Packages.Add(new Package(
                                CoreTools.FormatAsName(name),
                                name,
                                installedVersion,
                                newVersion,
                                Properties.DefaultSource,
                                this
                            ));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to parse brew outdated JSON: {ex.Message}");
            }

            return Packages;
        }

        private Process GetProcess(string fileName, string extraArguments)
        {
            return new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = extraArguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8,
                },
            };
        }
    }
}
