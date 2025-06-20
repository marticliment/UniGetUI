using System.Diagnostics;
using System.Text.RegularExpressions;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Managers.Chocolatey;
using UniGetUI.PackageEngine.Managers.PowerShellManager;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Structs;
using Architecture = UniGetUI.PackageEngine.Enums.Architecture;

namespace UniGetUI.PackageEngine.Managers.DotNetManager
{
    public class DotNet : BaseNuGet
    {
        public static new string[] FALSE_PACKAGE_IDS = [""];
        public static new string[] FALSE_PACKAGE_VERSIONS = [""];

        public DotNet()
        {
            Capabilities = new ManagerCapabilities
            {
                CanRunAsAdmin = true,
                CanDownloadInstaller = true,
                SupportsCustomScopes = true,
                SupportsCustomArchitectures = true,
                SupportedCustomArchitectures = [Architecture.x86, Architecture.x64, Architecture.arm64, Architecture.arm32],
                SupportsPreRelease = true,
                SupportsCustomLocations = true,
                SupportsCustomPackageIcons = true,
                SupportsCustomVersions = true,
                SupportsProxy = ProxySupport.Partially,
                SupportsProxyAuth = true
            };

            Properties = new ManagerProperties
            {
                Name = ".NET Tool",
                Description = CoreTools.Translate("A repository full of tools and executables designed with Microsoft's .NET ecosystem in mind.<br>Contains: <b>.NET related tools and scripts</b>"),
                IconId = IconType.DotNet,
                ColorIconId = "dotnet_color",
                ExecutableFriendlyName = "dotnet tool",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "update",
                ExecutableCallArgs = "tool",
                DefaultSource = new ManagerSource(this, "nuget.org", new Uri("https://www.nuget.org/api/v2")),
                KnownSources = [new ManagerSource(this, "nuget.org", new Uri("https://www.nuget.org/api/v2"))],
            };

            DetailsHelper = new DotNetDetailsHelper(this);
            OperationHelper = new DotNetPkgOperationHelper(this);
        }

        protected override IReadOnlyList<Package> _getInstalledPackages_UnSafe()
        {
            List<Package> Packages = [];
            foreach (var options in new OverridenInstallationOptions[] { new(PackageScope.Local), new(PackageScope.Global) })
            {
                using Process p = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Status.ExecutablePath,
                        Arguments = Properties.ExecutableCallArgs + " list" + (options.Scope == PackageScope.Global ? " --global" : ""),
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
                        if (line.Contains("----"))
                        {
                            DashesPassed = true;
                        }
                    }
                    else
                    {
                        string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                        if (elements.Length < 2)
                        {
                            continue;
                        }

                        for (int i = 0; i < elements.Length; i++)
                        {
                            elements[i] = elements[i].Trim();
                        }

                        if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        {
                            continue;
                        }

                        Packages.Add(new Package(
                            CoreTools.FormatAsName(elements[0]),
                            elements[0],
                            elements[1],
                            DefaultSource,
                            this,
                            options
                        ));
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
            ManagerStatus status = new();

            var (found, path) = CoreTools.Which("dotnet.exe");
            status.ExecutablePath = path;
            status.Found = found;

            if (!status.Found)
            {
                return status;
            }

            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = status.ExecutablePath,
                    Arguments = "tool -h",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };
            process.Start();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                status.Found = false;
                return status;
            }

            process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = status.ExecutablePath,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            process.Start();
            status.Version = process.StandardOutput.ReadToEnd().Trim();

            return status;
        }
    }
}
