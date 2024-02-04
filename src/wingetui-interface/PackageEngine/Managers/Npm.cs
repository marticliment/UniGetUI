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

namespace ModernWindow.PackageEngine.Managers
{
    public class Npm : PackageManager
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] { "" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "" };
        protected override async Task<Package[]> FindPackages_UnSafe(string query)
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " search " + query + " --parseable",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };

            p.Start();
            string line;
            List<Package> Packages = new();
            bool HeaderPassed = false;
            while((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                if (!HeaderPassed)
                    if(line.Contains("NAME"))
                        HeaderPassed = true;
                else
                {
                    string[] elements = line.Split('\t');
                    if(elements.Length >= 5)
                        Packages.Add(new Package(bindings.FormatAsName(elements[0]), elements[0], elements[4], MainSource, this));
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
                Arguments = Properties.ExecutableCallArgs + " outdated --parseable",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };

            p.Start();
            string line;
            List<UpgradablePackage> Packages = new();
            while((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                string[] elements = line.Split(':');
                if(elements.Length >= 4)
                {
                    Packages.Add(new UpgradablePackage(bindings.FormatAsName(elements[2].Split('@')[0]), elements[2].Split('@')[0], elements[3].Split('@')[^1], elements[2].Split('@')[^1], MainSource, this));
                }
            }
            
            p = new Process();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " outdated --global --parseable",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };

            p.Start();
            while((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                string[] elements = line.Split(':');
                if(elements.Length >= 4)
                    Packages.Add(new UpgradablePackage(bindings.FormatAsName(elements[2].Split('@')[0]), elements[2].Split('@')[0], elements[3].Split('@')[^1], elements[2].Split('@')[^1], MainSource, this, PackageScope.Global));
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
                CreateNoWindow = true,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };

            p.Start();
            string line;
            List<Package> Packages = new();
            while((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                if(line.Contains("--") || line.Contains("├─") || line.Contains("└─"))
                {
                    string[] elements = line[4..].Split('@');
                    Packages.Add(new Package(bindings.FormatAsName(elements[0]), elements[0], elements[1], MainSource, this));
                }
            }
            
            p = new Process();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " list --global",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };

            p.Start();
            while((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                if(line.Contains("--") || line.Contains("├─") || line.Contains("└─"))
                {
                    string[] elements = line[4..].Split('@');
                    Packages.Add(new Package(bindings.FormatAsName(elements[0]), elements[0], elements[1], MainSource, this, PackageScope.Global));
                }
            }

            await p.WaitForExitAsync();

            return Packages.ToArray();
        }

        public override OperationVeredict GetInstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override OperationVeredict GetUpdateOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override OperationVeredict GetUninstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }
        public override string[] GetInstallParameters(Package package, InstallationOptions options)
        {
            var parameters = GetUninstallParameters(package, options);
            parameters[0] = Properties.InstallVerb;

            if(options.Version != "")
                parameters[1] = package.Id + "@" + package.Version;
            else
                parameters[1] = package.Id + "@latest";
            
            return parameters;
        }
        public override string[] GetUpdateParameters(Package package, InstallationOptions options)
        {
            var parameters = GetUninstallParameters(package, options);
            parameters[0] = Properties.UpdateVerb;
            parameters[1] = package.Id + "@" + package.NewVersion;
            return parameters;
        }
        public override string[] GetUninstallParameters(Package package, InstallationOptions options)
        {
            var parameters = new List<string>() { Properties.UninstallVerb, package.Id };

            if (options.CustomParameters != null)
                parameters.AddRange(options.CustomParameters);

            if (package.Scope == PackageScope.Global)
                parameters.Add("--global");

            return parameters.ToArray();

        }
        public override ManagerSource GetMainSource()
        {
            return new ManagerSource(this, "npm", new Uri("https://www.npmjs.com/"));
        }

        public override Task<PackageDetails> GetPackageDetails_UnSafe(Package package)
        {
            throw new NotImplementedException();
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async Task RefreshSources()
        {
            // Npm does not support manual source refreshing
        }

        protected override ManagerCapabilities GetCapabilities()
        {
            return new ManagerCapabilities()
            {
                CanRunAsAdmin = true,
                SupportsCustomVersions = true,
                SupportsCustomScopes = true,
            };
        }

        protected override ManagerProperties GetProperties()
        {
            ManagerProperties properties = new ManagerProperties()
            {
                Name = "Npm",
                Description = bindings.Translate("Node JS's package manager. Full of libraries and other utilities that orbit the javascript world<br>Contains: <b>Node javascript libraries and other related utilities</b>"),
                IconId = "node",
                ColorIconId = "node_color",
                ExecutableFriendlyName = "npm",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "install",
                ExecutableCallArgs = "-NoProfile -ExecutionPolicy Bypass -Command npm",
                
            };
            return properties;
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            var status = new ManagerStatus();

            status.ExecutablePath = Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe");
            status.Found = File.Exists(await bindings.Which("npm"));

            if(!status.Found)
                return status;

            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " --version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                }
            };
            process.Start();
            status.Version = (await process.StandardOutput.ReadToEndAsync()).Trim();
            await process.WaitForExitAsync();

            if (status.Found && IsEnabled())
                await RefreshSources();

            return status;
        }
    }
}
