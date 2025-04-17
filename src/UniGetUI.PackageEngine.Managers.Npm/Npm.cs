using System.Diagnostics;
using System.Text.Json.Nodes;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Structs;

namespace UniGetUI.PackageEngine.Managers.NpmManager
{
    public class Npm : PackageManager
    {
        public Npm()
        {
            Capabilities = new ManagerCapabilities
            {
                CanRunAsAdmin = true,
                SupportsCustomVersions = true,
                CanDownloadInstaller = true,
                SupportsCustomScopes = true,
                SupportsPreRelease = true,
                SupportsProxy = ProxySupport.No,
                SupportsProxyAuth = false
            };

            Properties = new ManagerProperties
            {
                Name = "Npm",
                Description = CoreTools.Translate("Node JS's package manager. Full of libraries and other utilities that orbit the javascript world<br>Contains: <b>Node javascript libraries and other related utilities</b>"),
                IconId = IconType.Node,
                ColorIconId = "node_color",
                ExecutableFriendlyName = "npm",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "install",
                ExecutableCallArgs = " -NoProfile -ExecutionPolicy Bypass -Command npm",
                DefaultSource = new ManagerSource(this, "npm", new Uri("https://www.npmjs.com/")),
                KnownSources = [new ManagerSource(this, "npm", new Uri("https://www.npmjs.com/"))],

            };

            DetailsHelper = new NpmPkgDetailsHelper(this);
            OperationHelper = new NpmPkgOperationHelper(this);
        }

        protected override IReadOnlyList<Package> FindPackages_UnSafe(string query)
        {
            using Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " search \"" + query + "\" --json",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.FindPackages, p);
            p.Start();

            string? line;
            List<Package> Packages = [];
            while ((line = p.StandardOutput.ReadLine()) is not null)
            {
                logger.AddToStdOut(line);
                if (line.StartsWith("{"))
                {
                    JsonNode? node = JsonNode.Parse(line);
                    string? id = node?["name"]?.ToString();
                    string? version = node?["version"]?.ToString();
                    if (id is not null && version is not null)
                    {
                        Packages.Add(new Package(CoreTools.FormatAsName(id), id, version, DefaultSource, this));
                    }
                    else
                    {
                        logger.AddToStdErr("Line could not be parsed: " + line);
                    }
                }
            }

            logger.AddToStdErr(p.StandardError.ReadToEnd());
            p.WaitForExit();
            logger.Close(p.ExitCode);

            return Packages;
        }

        protected override IReadOnlyList<Package> GetAvailableUpdates_UnSafe()
        {
            List<Package> Packages = [];
            foreach (var options in new OverridenInstallationOptions[] { new(PackageScope.Local), new(PackageScope.Global) })
            {
                using Process p = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Status.ExecutablePath,
                        Arguments = Properties.ExecutableCallArgs + " outdated --json" + (options.Scope == PackageScope.Global ? " --global" : ""),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        StandardOutputEncoding = System.Text.Encoding.UTF8
                    }
                };

                IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListUpdates, p);
                p.Start();

                string strContents = p.StandardOutput.ReadToEnd();
                logger.AddToStdOut(strContents);
                JsonObject? contents = null;
                if (strContents.Any()) contents = JsonNode.Parse(strContents) as JsonObject;
                foreach (var (packageId, packageData) in contents?.ToDictionary() ?? [])
                {
                    string? version = packageData?["current"]?.ToString();
                    string? newVersion = packageData?["latest"]?.ToString();
                    if (version is not null && newVersion is not null)
                    {
                        Packages.Add(new Package(CoreTools.FormatAsName(packageId), packageId, version, newVersion,
                            DefaultSource, this, options));
                    }
                }

                logger.AddToStdErr(p.StandardError.ReadToEnd());
                p.WaitForExit();
                logger.Close(p.ExitCode);
            }
            return Packages;
        }

        protected override IReadOnlyList<Package> GetInstalledPackages_UnSafe()
        {
            List<Package> Packages = [];
            foreach (var options in new OverridenInstallationOptions[] { new(PackageScope.Local), new(PackageScope.Global) })
            {
                using Process p = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Status.ExecutablePath,
                        Arguments = Properties.ExecutableCallArgs + " list --json" + (options.Scope == PackageScope.Global ? " --global" : ""),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        StandardOutputEncoding = System.Text.Encoding.UTF8
                    }
                };

                IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListInstalledPackages, p);
                p.Start();

                string strContents = p.StandardOutput.ReadToEnd();
                logger.AddToStdOut(strContents);
                JsonObject? contents = null;
                if (strContents.Any()) contents = (JsonNode.Parse(strContents) as JsonObject)?["dependencies"] as JsonObject;
                foreach (var (packageId, packageData) in contents?.ToDictionary() ?? [])
                {
                    string? version = packageData?["version"]?.ToString();
                    if (version is not null)
                    {
                        Packages.Add(new Package(CoreTools.FormatAsName(packageId), packageId, version, DefaultSource, this, options));
                    }
                }

                logger.AddToStdErr(p.StandardError.ReadToEnd());
                p.WaitForExit();
                logger.Close(p.ExitCode);
            }

            return Packages;
        }

        protected override ManagerStatus LoadManager()
        {
            ManagerStatus status = new()
            {
                ExecutablePath = Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe"),
                Found = CoreTools.Which("npm").Item1
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
                    Arguments = Properties.ExecutableCallArgs + " --version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };
            process.Start();
            status.Version = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return status;
        }
    }
}
