using System.Diagnostics;
using System.Text.RegularExpressions;
using UniGetUI.Core.Classes;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.Classes;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Structs;
using Architecture = UniGetUI.PackageEngine.Enums.Architecture;

namespace UniGetUI.PackageEngine.Managers.ScoopManager
{

    public class Scoop : PackageManager
    {
        public static new string[] FALSE_PACKAGE_IDS = ["No"];
        public static new string[] FALSE_PACKAGE_VERSIONS = ["Matches", "Install", "failed", "failed,", "Manifest", "removed", "removed,"];

        private long LastScoopSourceUpdateTime;

        public Scoop()
        {
            Dependencies = [
                // Scoop-Search is required for search to work
                new ManagerDependency(
                    "Scoop-Search",
                    CoreData.PowerShell5,
                    "-ExecutionPolicy Bypass -NoLogo -NoProfile -Command \"& {scoop install main/scoop-search; if($error.count -ne 0){pause}}\"",
                    "scoop install main/scoop-search",
                    async () => (await CoreTools.WhichAsync("scoop-search.exe")).Item1),
                // GIT is required for scoop updates to work
                new ManagerDependency(
                    "Git",
                    CoreData.PowerShell5,
                    "-ExecutionPolicy Bypass -NoLogo -NoProfile -Command \"& {scoop install main/git; if($error.count -ne 0){pause}}\"",
                    "scoop install main/git",
                    async () => (await CoreTools.WhichAsync("git.exe")).Item1)
            ];

            Capabilities = new ManagerCapabilities
            {
                CanRunAsAdmin = true,
                CanSkipIntegrityChecks = true,
                CanDownloadInstaller = true,
                CanRemoveDataOnUninstall = true,
                SupportsCustomArchitectures = true,
                SupportedCustomArchitectures = [Architecture.x86, Architecture.x64, Architecture.arm64],
                SupportsCustomScopes = true,
                SupportsCustomSources = true,
                Sources = new SourceCapabilities
                {
                    KnowsPackageCount = true,
                    KnowsUpdateDate = true
                },
                SupportsProxy = ProxySupport.No,
                SupportsProxyAuth = false
            };

            Properties = new ManagerProperties
            {
                Name = "Scoop",
                Description = CoreTools.Translate("Great repository of unknown but useful utilities and other interesting packages.<br>Contains: <b>Utilities, Command-line programs, General Software (extras bucket required)</b>"),
                IconId = IconType.Scoop,
                ColorIconId = "scoop_color",
                ExecutableFriendlyName = "scoop",
                InstallVerb = "install",
                UpdateVerb = "update",
                UninstallVerb = "uninstall",
                KnownSources = [new ManagerSource(this, "main", new Uri("https://github.com/ScoopInstaller/Main")),
                                new ManagerSource(this, "extras", new Uri("https://github.com/ScoopInstaller/Extras")),
                                new ManagerSource(this, "versions", new Uri("https://github.com/ScoopInstaller/Versions")),
                                new ManagerSource(this, "nirsoft", new Uri("https://github.com/ScoopInstaller/Nirsoft")),
                                new ManagerSource(this, "sysinternals", new Uri("https://github.com/niheaven/scoop-sysinternals")),
                                new ManagerSource(this, "php", new Uri("https://github.com/ScoopInstaller/PHP")),
                                new ManagerSource(this, "nerd-fonts", new Uri("https://github.com/matthewjberger/scoop-nerd-fonts")),
                                new ManagerSource(this, "nonportable", new Uri("https://github.com/ScoopInstaller/Nonportable")),
                                new ManagerSource(this, "java", new Uri("https://github.com/ScoopInstaller/Java")),
                                new ManagerSource(this, "games", new Uri("https://github.com/Calinou/scoop-games"))],
                DefaultSource = new ManagerSource(this, "main", new Uri("https://github.com/ScoopInstaller/Main")),
            };

            SourcesHelper = new ScoopSourceHelper(this);
            DetailsHelper = new ScoopPkgDetailsHelper(this);
            OperationHelper = new ScoopPkgOperationHelper(this);
        }

        protected override IReadOnlyList<Package> FindPackages_UnSafe(string query)
        {
            List<Package> Packages = [];

            var (found, path) = CoreTools.Which("scoop-search.exe");
            if (!found)
            {
                Process proc = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Status.ExecutablePath,
                        Arguments = Status.ExecutableCallArgs + " install main/scoop-search",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                IProcessTaskLogger aux_logger = TaskLogger.CreateNew(LoggableTaskType.InstallManagerDependency, proc);
                proc.Start();
                aux_logger.AddToStdOut(proc.StandardOutput.ReadToEnd());
                aux_logger.AddToStdErr(proc.StandardError.ReadToEnd());
                proc.WaitForExit();
                aux_logger.Close(proc.ExitCode);
                path = "scoop-search.exe";
            }

            using Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = query,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };
            p.StartInfo = CoreTools.UpdateEnvironmentVariables(p.StartInfo);
            IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.FindPackages, p);

            p.Start();

            string? line;
            IManagerSource source = Properties.DefaultSource;
            while ((line = p.StandardOutput.ReadLine()) is not null)
            {
                logger.AddToStdOut(line);
                if (line.StartsWith("'"))
                {
                    string sourceName = line.Split(" ")[0].Replace("'", "");
                    source = SourcesHelper.Factory.GetSourceOrDefault(sourceName);
                }
                else if (line.Trim() != "")
                {
                    string[] elements = line.Trim().Split(" ");
                    if (elements.Length < 2)
                    {
                        continue;
                    }

                    for (int i = 0; i < elements.Length; i++)
                    {
                        elements[i] = elements[i].Trim();
                    }

                    if (FALSE_PACKAGE_IDS.Contains(elements[0])
                        || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                    {
                        continue;
                    }

                    Packages.Add(new Package(
                        CoreTools.FormatAsName(elements[0]),
                        elements[0],
                        elements[1].Replace("(", "").Replace(")", ""),
                        source,
                        this));
                }
            }
            logger.AddToStdErr(p.StandardError.ReadToEnd());
            p.WaitForExit();
            logger.Close(p.ExitCode);
            return Packages;
        }

        protected override IReadOnlyList<Package> GetAvailableUpdates_UnSafe()
        {
            Dictionary<string, IPackage> InstalledPackages = [];
            foreach (IPackage InstalledPackage in GetInstalledPackages())
            {
                if (!InstalledPackages.ContainsKey(InstalledPackage.Id + "." + InstalledPackage.VersionString))
                {
                    InstalledPackages.Add(InstalledPackage.Id + "." + InstalledPackage.VersionString, InstalledPackage);
                }
            }

            List<Package> Packages = [];

            using Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Status.ExecutableCallArgs + " status",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };
            IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListUpdates, p);

            p.Start();

            string? line;
            bool DashesPassed = false;
            while ((line = p.StandardOutput.ReadLine()) is not null)
            {
                logger.AddToStdOut(line);
                if (!DashesPassed)
                {
                    if (line.Contains("---"))
                    {
                        DashesPassed = true;
                    }
                }
                else if (line.Trim() != "")
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Trim().Split(" ");
                    if (elements.Length < 3)
                    {
                        continue;
                    }

                    for (int i = 0; i < elements.Length; i++)
                    {
                        elements[i] = elements[i].Trim();
                    }

                    if (FALSE_PACKAGE_IDS.Contains(elements[0])
                        || FALSE_PACKAGE_VERSIONS.Contains(elements[1])
                        || FALSE_PACKAGE_VERSIONS.Contains(elements[2]))
                    {
                        continue;
                    }

                    if (InstalledPackages.TryGetValue(elements[0] + "." + elements[1], out IPackage? InstalledPackage))
                    {
                        OverridenInstallationOptions options = new(InstalledPackage.OverridenOptions.Scope);
                        Packages.Add(new Package(
                            CoreTools.FormatAsName(elements[0]),
                            elements[0],
                            elements[1],
                            elements[2],
                            InstalledPackage.Source,
                            this,
                            options));
                    }
                    else
                    {
                        Logger.Warn("Upgradable scoop package not listed on installed packages - id=" + elements[0]);
                    }
                }
            }
            logger.AddToStdErr(p.StandardError.ReadToEnd());
            p.WaitForExit();
            logger.Close(p.ExitCode);
            return Packages;
        }

        protected override IReadOnlyList<Package> GetInstalledPackages_UnSafe()
            => TaskRecycler<IReadOnlyList<Package>>.RunOrAttach(_getInstalledPackages_UnSafe, 15);
        private IReadOnlyList<Package> _getInstalledPackages_UnSafe()
        {
            List<Package> Packages = [];

            using Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Status.ExecutableCallArgs + " list",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };
            IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListInstalledPackages, p);
            p.Start();

            string? line;
            bool DashesPassed = false;
            while ((line = p.StandardOutput.ReadLine()) is not null)
            {
                logger.AddToStdOut(line);
                if (!DashesPassed)
                {
                    if (line.Contains("---"))
                    {
                        DashesPassed = true;
                    }
                }
                else if (line.Trim() != "")
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Trim().Split(" ");
                    if (elements.Length < 3)
                        continue;

                    if (elements[2].Contains(":\\"))
                    {
                        var path = Regex.Match(line, "[A-Za-z]:(?:[\\\\\\/][^\\\\\\/\\n]+)+(?:.json|…)");
                        elements[2] = path.Value;
                    }

                    for (int i = 0; i < elements.Length; i++)
                    {
                        elements[i] = elements[i].Trim();
                    }

                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                    {
                        continue;
                    }

                    OverridenInstallationOptions options = new(
                        line.Contains("Global install") ? PackageScope.Global : PackageScope.User
                    );

                    Packages.Add(new Package(
                        CoreTools.FormatAsName(elements[0]),
                        elements[0],
                        elements[1],
                        SourcesHelper.Factory.GetSourceOrDefault(elements[2]),
                        this,
                        options));
                }
            }
            logger.AddToStdErr(p.StandardError.ReadToEnd());
            p.WaitForExit();
            logger.Close(p.ExitCode);
            return Packages;
        }

        public override void RefreshPackageIndexes()
        {
            if (new TimeSpan(DateTime.Now.Ticks - LastScoopSourceUpdateTime).TotalMinutes < 10)
            {
                Logger.Info("Scoop buckets have been already refreshed in the last ten minutes, skipping.");
                return;
            }
            LastScoopSourceUpdateTime = DateTime.Now.Ticks;
            using Process p = new();
            ProcessStartInfo StartInfo = new()
            {
                FileName = Status.ExecutablePath,
                Arguments = Status.ExecutableCallArgs + " update",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            p.StartInfo = StartInfo;
            IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.RefreshIndexes, p);
            p.Start();
            logger.AddToStdOut(p.StandardOutput.ReadToEnd());
            logger.AddToStdErr(p.StandardError.ReadToEnd());
            p.WaitForExit();
            logger.Close(p.ExitCode);
        }

        public override IReadOnlyList<string> FindCandidateExecutableFiles()
        {
            return CoreTools.WhichMultiple("scoop.ps1");
        }


        protected override ManagerStatus LoadManager()
        {
            string path = CoreData.PowerShell5;
            var pwsh7 = CoreTools.Which("pwsh.exe");

            if (pwsh7.Item1)
            {
                Logger.Info("Scoop found PowerShell7, PowerShell7 will be used...");
                path = pwsh7.Item2;
            }

            var (found, executable) = GetExecutableFile();
            ManagerStatus status = new()
            {
                ExecutablePath = path,
                ExecutableCallArgs = $"-NoProfile -ExecutionPolicy Bypass -Command \"{executable.Replace(" ", "` ")}\" ",
                Found = found,
            };

            if (!status.Found)
            {
                return status;
            }

            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = status.ExecutablePath,
                    Arguments = status.ExecutableCallArgs + "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };
            process.Start();
            status.Version = process.StandardOutput.ReadToEnd().Trim();

            Status = status; // Wee need this for the RunCleanup method to get the executable path
            if (status.Found && IsEnabled() && Settings.Get(Settings.K.EnableScoopCleanup))
            {
                RunCleanup();
            }

            return status;
        }

        private async void RunCleanup()
        {
            Logger.Info("Starting scoop cleanup...");
            foreach (string command in new[] { " cache rm *", " cleanup --all --cache", " cleanup --all --global --cache" })
            {
                using Process p = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Status.ExecutablePath,
                        Arguments = Status.ExecutableCallArgs + " " + command,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8
                    }
                };
                p.Start();
                await p.WaitForExitAsync();
            }

            Logger.Info("Scoop cleanup finished!");
        }
    }
}
