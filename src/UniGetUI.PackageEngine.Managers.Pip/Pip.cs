using System.Diagnostics;
using System.Text.RegularExpressions;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.Classes;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.PipManager
{
    public class Pip : PackageManager
    {
        public static new string[] FALSE_PACKAGE_IDS = ["", "WARNING:", "[notice]", "Package", "DEPRECATION:"];
        public static new string[] FALSE_PACKAGE_VERSIONS = ["", "Ignoring", "invalid"];

        public Pip()
        {
            Dependencies = [
                // parse_pip_search is required for pip package finding to work
                new ManagerDependency(
                    "parse-pip-search",
                    Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe"),
                    "-ExecutionPolicy Bypass -NoLogo -NoProfile -Command \"& {python.exe "
                        + "-m pip install parse_pip_search; if($error.count -ne 0){pause}}\"",
                    "python -m pip install parse_pip_search",
                    async () =>
                    {
                        bool found = (await CoreTools.WhichAsync("parse_pip_search.exe")).Item1;
                        if (found) return true;
                        else if (Status.ExecutablePath.Contains("WindowsApps\\python.exe"))
                        {
                            Logger.Warn("parse_pip_search could was not found but the user will not be prompted to install it.");
                            Logger.Warn("NOTE: Microsoft Store python is not fully supported on UniGetUI");
                            return true;
                        }
                        else return false;
                    }
                )
            ];

            Capabilities = new ManagerCapabilities
            {
                CanRunAsAdmin = true,
                SupportsCustomVersions = true,
                SupportsCustomScopes = true,
                SupportsPreRelease = true,
            };

            Properties = new ManagerProperties
            {
                Name = "Pip",
                Description = CoreTools.Translate("Python's library manager. Full of python libraries and other python-related utilities<br>Contains: <b>Python libraries and related utilities</b>"),
                IconId = IconType.Python,
                ColorIconId = "pip_color",
                ExecutableFriendlyName = "pip",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "install --upgrade",
                ExecutableCallArgs = " -m pip",
                DefaultSource = new ManagerSource(this, "pip", new Uri("https://pypi.org/")),
                KnownSources = [new ManagerSource(this, "pip", new Uri("https://pypi.org/"))],

            };

            DetailsHelper = new PipPkgDetailsHelper(this);
            OperationHelper = new PipPkgOperationHelper(this);
        }

        protected override IEnumerable<Package> FindPackages_UnSafe(string query)
        {
            List<Package> Packages = [];

            var (found, path) = CoreTools.Which("parse_pip_search.exe");
            if (!found)
            {
                Process proc = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Status.ExecutablePath,
                        Arguments = Properties.ExecutableCallArgs + " install parse_pip_search",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                    }
                };
                IProcessTaskLogger aux_logger = TaskLogger.CreateNew(LoggableTaskType.InstallManagerDependency, proc);
                proc.Start();

                aux_logger.AddToStdOut(proc.StandardOutput.ReadToEnd());
                aux_logger.AddToStdErr(proc.StandardError.ReadToEnd());

                proc.WaitForExit();
                aux_logger.Close(proc.ExitCode);
                path = "parse_pip_search.exe";
            }

            Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "\"" + query + "\"",
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
                    string[] elements = line.Split('|');
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

                    Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], DefaultSource, this, new(PackageScope.Global)));
                }
            }
            logger.AddToStdErr(p.StandardError.ReadToEnd());
            p.WaitForExit();
            logger.Close(p.ExitCode);

            return Packages;
        }

        protected override IEnumerable<Package> GetAvailableUpdates_UnSafe()
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " list --outdated",
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
            List<Package> Packages = [];
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
                    if (elements.Length < 3)
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

                    Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], elements[2], DefaultSource, this, new(PackageScope.Global)));
                }
            }
            logger.AddToStdErr(p.StandardError.ReadToEnd());
            p.WaitForExit();
            logger.Close(p.ExitCode);

            return Packages;
        }

        protected override IEnumerable<Package> GetInstalledPackages_UnSafe()
        {

            Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " list",
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
            List<Package> Packages = [];
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

                    Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], DefaultSource, this, new(PackageScope.Global)));
                }
            }
            logger.AddToStdErr(p.StandardError.ReadToEnd());
            p.WaitForExit();
            logger.Close(p.ExitCode);

            return Packages;
        }

        protected override ManagerStatus LoadManager()
        {
            ManagerStatus status = new();

            var (found, path) = CoreTools.Which("python.exe");
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
                    Arguments = Properties.ExecutableCallArgs + " --version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };
            process.Start();
            status.Version = process.StandardOutput.ReadToEnd().Trim();

            if (process.ExitCode == 9009)
            {
                status.Found = false;
                return status;
            }

            return status;
        }
    }
}

