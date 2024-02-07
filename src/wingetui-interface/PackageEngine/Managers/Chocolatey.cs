using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Certificates;
using Windows.Management.Deployment;
using Windows.Graphics.Display;
using System.IO;
using System.Diagnostics;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Collections;
using Windows.UI.WebUI;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;

namespace ModernWindow.PackageEngine.Managers
{
    public class Chocolatey : PackageManagerWithSources
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] {"", "Did", "Features?", "Validation", "-", "being", "It", "Error", "L'accs", "Maximum", "This", "Output is package name ", "operable", "Invalid" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] {"", "Did", "Features?", "Validation", "-", "being", "It", "Error", "L'accs", "Maximum", "This", "packages", "current version", "installed version", "is", "program", "validations", "argument", "no" };
        protected override async Task<Package[]> FindPackages_UnSafe(string query)
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " search \"" + query + "\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            p.Start();
            string line;
            List<Package> Packages = new();
            while((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                if (!line.StartsWith("Chocolatey"))
                {
                    string[] elements = line.Split(' ');
                    for(int i = 0; i<elements.Length; i++) elements[i] = elements[i].Trim();

                    if (elements.Length < 2)
                        continue;

                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;
                    
                    Packages.Add(new Package(bindings.FormatAsName(elements[0]), elements[0], elements[1], MainSource, this));
                }
            }

            await p.WaitForExitAsync();

            return Packages.ToArray();
        }

        protected override async Task<UpgradablePackage[]> GetAvailableUpdates_UnSafe()
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " outdated",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            p.Start();
            string line;
            List<UpgradablePackage> Packages = new();
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                if (!line.StartsWith("Chocolatey"))
                {
                    string[] elements = line.Split('|');
                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                    if (elements.Length <= 2)
                        continue;
                    
                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;

                    Packages.Add(new UpgradablePackage(bindings.FormatAsName(elements[0]), elements[0], elements[1], elements[2], MainSource, this));
                }
            }

            await p.WaitForExitAsync();

            return Packages.ToArray();
        }

        protected override async Task<Package[]> GetInstalledPackages_UnSafe()
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            p.Start();
            string line;
            List<Package> Packages = new();
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                if (!line.StartsWith("Chocolatey"))
                {
                    string[] elements = line.Split(' ');
                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                    if (elements.Length <= 1)
                        continue;

                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;

                    Packages.Add(new Package(bindings.FormatAsName(elements[0]), elements[0], elements[1], MainSource, this));
                }
            }

            await p.WaitForExitAsync();

            return Packages.ToArray();
        }
        public override OperationVeredict GetInstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            var output_string = string.Join("\n", Output);
            
            if(ReturnCode == 1641 || ReturnCode == 0)
                return OperationVeredict.Succeeded;
            else if(ReturnCode == 3010)
                return OperationVeredict.Succeeded; // TODO: Restart required
            else if ((output_string.Contains("Run as administrator") || output_string.Contains("The requested operation requires elevation") || output_string.Contains("ERROR: Exception calling \"CreateDirectory\" with \"1\" argument(s): \"Access to the path")) && !options.RunAsAdministrator)
            {
                options.RunAsAdministrator = true;
                return OperationVeredict.AutoRetry;
            }
            return OperationVeredict.Failed;
        }

        public override OperationVeredict GetUpdateOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return GetInstallOperationVeredict(package, options, ReturnCode, Output);
        }

        public override OperationVeredict GetUninstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            var output_string = string.Join("\n", Output);

            if (ReturnCode == 1641 || ReturnCode == 1614 || ReturnCode == 1605 || ReturnCode == 0)
                return OperationVeredict.Succeeded;
            else if (ReturnCode == 3010)
                return OperationVeredict.Succeeded; // TODO: Restart required
            else if ((output_string.Contains("Run as administrator") || output_string.Contains("The requested operation requires elevation")) && !options.RunAsAdministrator)
            {
                options.RunAsAdministrator = true;
                return OperationVeredict.AutoRetry;
            }
            return OperationVeredict.Failed;
        }
        public override string[] GetInstallParameters(Package package, InstallationOptions options)
        {
            var parameters = GetUninstallParameters(package, options).ToList();
            parameters[0] = Properties.InstallVerb;

            if(options.Architecture == System.Runtime.InteropServices.Architecture.X86)
                parameters.Add("--forcex86");

            if(options.PreRelease)
                parameters.Add("--prerelease");

            if(options.SkipHashCheck)
                parameters.AddRange(new string[] { "--ignore-checksums", "--force"});

            if (options.Version != "")
                parameters.AddRange(new string[] { "--version=" + options.Version, "--allow-downgrade" });

            return parameters.ToArray();
        }
        public override string[] GetUpdateParameters(Package package, InstallationOptions options)
        {
            var parameters = GetInstallParameters(package, options);
            parameters[0] = Properties.UpdateVerb;
            return parameters;
        }

        public override string[] GetUninstallParameters(Package package, InstallationOptions options)
        {
            var parameters = new List<string>() { Properties.UninstallVerb, package.Id, "-y" };

            if (options.CustomParameters != null)
                parameters.AddRange(options.CustomParameters);

            if (!options.InteractiveInstallation)
                parameters.Add("--notsilent");

            return parameters.ToArray();
        }

        public override ManagerSource GetMainSource()
        {
            return new ManagerSource(this, "community", new Uri("https://community.chocolatey.org/api/v2/"));
        }

        public override Task<PackageDetails> GetPackageDetails_UnSafe(Package package)
        {
            throw new NotImplementedException();
        }

        protected override async Task<ManagerSource[]> GetSources_UnSafe()
        {
            List<ManagerSource> sources = new List<ManagerSource>();

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " source list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.StartInfo = startInfo;
            process.Start();

            string line;
            while ((line = await process.StandardOutput.ReadLineAsync()) != null)
            {
                try {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    if(line.Contains(" - ") && line.Contains(" | ")) {
                        string[] parts = line.Trim().Split('|')[0].Trim().Split(" - ");
                        sources.Add(new ManagerSource(this, parts[0].Trim(), new Uri(parts[1].Trim())));
                    }
                } catch (Exception e) {
                    Console.WriteLine(e);
                }
            }
            await process.WaitForExitAsync();
            return sources.ToArray();
        }

#pragma warning disable CS1998
        public override async Task RefreshSources()
        {
            // Chocolatey does not support source refreshing
        }

        protected override ManagerCapabilities GetCapabilities()
        {
            return new ManagerCapabilities()
            {
                CanRunAsAdmin = true,
                CanSkipIntegrityChecks = true,
                CanRunInteractively = true,
                SupportsCustomVersions = true,
                SupportsCustomArchitectures = true,
                SupportedCustomArchitectures = new Architecture[] { Architecture.X86 },
                SupportsPreRelease = true,
                SupportsCustomSources = true,
                Sources = new ManagerSource.Capabilities()
                {
                    KnowsPackageCount = false,
                    KnowsUpdateDate = false,
                }
            };
        }

        protected override ManagerProperties GetProperties()
        {
            ManagerProperties properties = new ManagerProperties()
            {
                Name = "Chocolatey",
                Description = bindings.Translate("The classical package manager for windows. You'll find everything there. <br>Contains: <b>General Software</b>"),
                IconId = "choco",
                ColorIconId = "choco_color",
                ExecutableFriendlyName = "choco.exe",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "upgrade",
                ExecutableCallArgs = "",
                
            };
            return properties;
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            var status = new ManagerStatus();

            if(bindings.GetSettings("UseSystemChocolatey"))
                status.ExecutablePath = await bindings.Which("choco.exe");
            else if(File.Exists(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs\\WingetUI\\choco-cli\\choco.exe")))
                status.ExecutablePath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs\\WingetUI\\choco-cli\\choco.exe");
            else
                status.ExecutablePath = Path.Join(Directory.GetParent(Environment.ProcessPath).FullName, "choco-cli/choco.exe");
            
            status.Found = File.Exists(status.ExecutablePath);

            if(!status.Found)
                return status;

            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = status.ExecutablePath,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            status.Version = (await process.StandardOutput.ReadToEndAsync()).Trim();

            if (status.Found && IsEnabled())
                await RefreshSources();

            return status;
        }

        protected override async Task<string[]> GetPackageVersions_Unsafe(Package package)
        {
            var p = new Process() {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " find -e " + package.Id + " -a",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            Console.WriteLine(p.StartInfo.FileName);
            Console.WriteLine(p.StartInfo.Arguments.ToString());

            p.Start();
            string line;
            List<string> versions = new();
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                if (!line.StartsWith("Chocolatey"))
                {
                    string[] elements = line.Split(' ');
                    Console.WriteLine(line);
                    if (elements.Length < 2 || elements[0].Trim() != package.Id)
                        continue;

                    versions.Add(elements[1].Trim());
                }
            }
            return versions.ToArray();
        }
    }
}
