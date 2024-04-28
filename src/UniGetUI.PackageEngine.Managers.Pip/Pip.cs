using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.PipManager
{
    public class Pip : PackageManager
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "", "WARNING:", "[notice]", "Package", "DEPRECATION:" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] { "", "WARNING:", "[notice]", "Package", "DEPRECATION:" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "", "Ignoring", "invalid" };
        
        public Pip() : base()
        {
            Capabilities = new ManagerCapabilities()
            {
                CanRunAsAdmin = true,
                SupportsCustomVersions = true,
                SupportsCustomScopes = true,
                SupportsPreRelease = true,
            };

            Properties= new ManagerProperties()
            {
                Name = "Pip",
                Description = CoreTools.Translate("Python's library manager. Full of python libraries and other python-related utilities<br>Contains: <b>Python libraries and related utilities</b>"),
                IconId = "python",
                ColorIconId = "pip_color",
                ExecutableFriendlyName = "pip",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "install --upgrade",
                ExecutableCallArgs = " -m pip",
                DefaultSource = new ManagerSource(this, "pip", new Uri("https://pypi.org/")),
                KnownSources = [new ManagerSource(this, "pip", new Uri("https://pypi.org/"))],

            };

            PackageDetailsProvider = new PipPackageDetailsProvider(this);
        }
        
        protected override async Task<Package[]> FindPackages_UnSafe(string query)
        {
            List<Package> Packages = new();

            var which_res = await CoreTools.Which("parse_pip_search");
            string path = which_res.Item2;
            if (!which_res.Item1)
            {
                Process proc = new()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = path,
                        Arguments = Properties.ExecutableCallArgs + " install parse_pip_search",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                    }
                };
                proc.Start();
                await proc.WaitForExitAsync();
                path = "parse_pip_search.exe";
            }

            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
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

            p.Start();

            string line;
            bool DashesPassed = false;
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!DashesPassed)
                {
                    if (line.Contains("----"))
                        DashesPassed = true;
                }
                else
                {
                    string[] elements = line.Split('|');
                    if (elements.Length < 2)
                        continue;

                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();
                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;

                    Packages.Add(new Package(Core.Tools.CoreTools.FormatAsName(elements[0]), elements[0], elements[1], DefaultSource, this, scope: PackageScope.Global));
                }
            }
            output += await p.StandardError.ReadToEndAsync();
            LogOperation(p, output);
            return Packages.ToArray();
        }

        protected override async Task<UpgradablePackage[]> GetAvailableUpdates_UnSafe()
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
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

            p.Start();

            string line;
            bool DashesPassed = false;
            List<UpgradablePackage> Packages = new();
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!DashesPassed)
                {
                    if (line.Contains("----"))
                        DashesPassed = true;
                }
                else
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                    if (elements.Length < 3)
                        continue;

                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();
                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;

                    Packages.Add(new UpgradablePackage(Core.Tools.CoreTools.FormatAsName(elements[0]), elements[0], elements[1], elements[2], DefaultSource, this, scope: PackageScope.Global));
                }
            }
            output += await p.StandardError.ReadToEndAsync();
            LogOperation(p, output);
            return Packages.ToArray();
        }

        protected override async Task<Package[]> GetInstalledPackages_UnSafe()
        {

            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
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

            p.Start();

            string line;
            bool DashesPassed = false;
            List<Package> Packages = new();
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!DashesPassed)
                {
                    if (line.Contains("----"))
                        DashesPassed = true;
                }
                else
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                    if (elements.Length < 2)
                        continue;

                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();
                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;

                    Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], DefaultSource, this, scope: PackageScope.Global));
                }
            }
            output += await p.StandardError.ReadToEndAsync();
            LogOperation(p, output);
            return Packages.ToArray();
        }

        public override OperationVeredict GetInstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            string output_string = string.Join("\n", Output);

            if (ReturnCode == 0)
                return OperationVeredict.Succeeded;
            else if (output_string.Contains("--user") && package.Scope == PackageScope.Global)
            {
                package.Scope = PackageScope.User;
                return OperationVeredict.AutoRetry;
            }
            else
                return OperationVeredict.Failed;
        }

        public override OperationVeredict GetUpdateOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return GetInstallOperationVeredict(package, options, ReturnCode, Output);
        }

        public override OperationVeredict GetUninstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return GetInstallOperationVeredict(package, options, ReturnCode, Output);
        }
        public override string[] GetInstallParameters(Package package, InstallationOptions options)
        {
            string[] parameters = GetUpdateParameters(package, options);
            parameters[0] = Properties.InstallVerb;
            return parameters;
        }
        public override string[] GetUpdateParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = GetUninstallParameters(package, options).ToList();
            parameters[0] = Properties.UpdateVerb;
            parameters.Remove("--yes");

            if (options.PreRelease)
                parameters.Add("--pre");

            if (options.InstallationScope == PackageScope.User)
                parameters.Add("--user");

            if (options.Version != "")
                parameters[1] = package.Id + "==" + options.Version;


            return parameters.ToArray();
        }

        public override string[] GetUninstallParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = new() { Properties.UninstallVerb, package.Id, "--yes", "--no-input", "--no-color", "--no-python-version-warning", "--no-cache" };

            if (options.CustomParameters != null)
                parameters.AddRange(options.CustomParameters);

            return parameters.ToArray();
        }

#pragma warning disable CS1998
        public override async Task RefreshPackageIndexes()
        {
            // Pip does not support manual source refreshing
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            ManagerStatus status = new();

            var which_res = await CoreTools.Which("python.exe");
            status.ExecutablePath = which_res.Item2;
            status.Found = which_res.Item1;

            if (!status.Found)
                return status;

            Process process = new()
            {
                StartInfo = new ProcessStartInfo()
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
            status.Version = (await process.StandardOutput.ReadToEndAsync()).Trim();

            if (status.Found && IsEnabled())
                await RefreshPackageIndexes();

            return status;
        }
    }
}

