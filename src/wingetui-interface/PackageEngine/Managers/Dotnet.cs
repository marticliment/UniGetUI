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
using System.Runtime.InteropServices;

namespace ModernWindow.PackageEngine.Managers
{
    public class Dotnet : PackageManager
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
            bool DashesPassed = false;
            while((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                if (!DashesPassed)
                {
                    if(line.Contains("-----"))
                        DashesPassed = true;
                }
                else
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                    if(elements.Length >= 2)
                        Packages.Add(new Package(bindings.FormatAsName(elements[0]), elements[0], elements[1], MainSource, this, PackageScope.Global)); 
                        // Dotnet tool packages are installed globally by default, hence the Global flag
                }
            }

            await p.WaitForExitAsync();

            return Packages.ToArray();
        }

        protected override async Task<UpgradablePackage[]> GetAvailableUpdates_UnSafe()
        {
            string path = await bindings.Which("dotnet-tools-outdated.exe");
            if(!File.Exists(path))
            {
                Process proc = new Process() {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = path,
                        Arguments = Properties.ExecutableCallArgs + " install dotnet-tools-outdated.exe",
                        UseShellExecute = true,
                        CreateNoWindow = true
                    }
                };
                proc.Start();
                await proc.WaitForExitAsync();
                path = "dotnet-tools-outdated.exe";
            }

            Process p = new Process() {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = path,
                    Arguments = "",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            p.Start();

            string line;
            bool DashesPassed = false;
            var Packages = new List<UpgradablePackage>();
            while((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                if(!DashesPassed)
                {
                    if(line.Contains("----"))
                        DashesPassed = true;
                } else {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                    if (elements.Length < 3)
                        continue;

                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();
                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;
                    
                    Packages.Add(new UpgradablePackage(bindings.FormatAsName(elements[0]), elements[0], elements[1], elements[2], MainSource, this, PackageScope.Global));
                }
            }
            return Packages.ToArray();
        }

        protected override async Task<Package[]> GetInstalledPackages_UnSafe()
        {
            Process p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " list",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            p.Start();

            string line;
            bool DashesPassed = false;
            var Packages = new List<Package>();
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
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

                    Packages.Add(new Package(bindings.FormatAsName(elements[0]), elements[0], elements[1], MainSource, this, PackageScope.User));
                }
            }

            p = new Process() {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " list --global",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            p.Start();

            DashesPassed = false;
            while((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                if(!DashesPassed)
                {
                    if(line.Contains("----"))
                        DashesPassed = true;
                } else {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                    if (elements.Length < 2)
                        continue;

                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();
                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;
                    
                    Packages.Add(new Package(bindings.FormatAsName(elements[0]), elements[0], elements[1], MainSource, this, PackageScope.Global));
                }
            }
            return Packages.ToArray();
        }
        

        public override OperationVeredict GetInstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override OperationVeredict GetUpdateOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override OperationVeredict GetUninstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }
        public override string[] GetInstallParameters(Package package, InstallationOptions options)
        {
            var parameters = GetUpdateParameters(package, options);
            parameters[0] = Properties.InstallVerb;
            return parameters;
        }
        public override string[] GetUpdateParameters(Package package, InstallationOptions options)
        {
            var parameters = GetUninstallParameters(package, options).ToList();
            parameters[0] = Properties.UpdateVerb;

            if (options.Architecture == Architecture.X86)
                parameters.AddRange(new string[] { "--arch", "x86" });
            else if (options.Architecture == Architecture.X64)
                parameters.AddRange(new string[] { "--arch", "x64" });
            else if (options.Architecture == Architecture.Arm)
                parameters.AddRange(new string[] { "--arch", "arm32" });
            else if (options.Architecture == Architecture.Arm64)
                parameters.AddRange(new string[] { "--arch", "arm64" });

            return parameters.ToArray();
        }

        public override string[] GetUninstallParameters(Package package, InstallationOptions options)
        {
            var parameters = new List<string>() { Properties.UninstallVerb, package.Id };

            if(options.CustomParameters != null)
                parameters.AddRange(options.CustomParameters);

            if (options.CustomInstallLocation != "")
                parameters.Add("--tool-path" + options.CustomInstallLocation);
            else if (package.Scope == PackageScope.Global)
                parameters.Add("--global");
            
            return parameters.ToArray();
        }

        public override ManagerSource GetMainSource()
        {
            return new ManagerSource(this, "nuget.org", new Uri("https://www.nuget.org/"));
        }

        public override Task<PackageDetails> GetPackageDetails_UnSafe(Package package)
        {
            throw new NotImplementedException();
        }


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async Task RefreshSources()
        {
            // .NET Tool does not support manual source refreshing
        }

        protected override ManagerCapabilities GetCapabilities()
        {
            return new ManagerCapabilities()
            {
                CanRunAsAdmin = true,
                SupportsCustomVersions = true,
                SupportsCustomScopes = true,
                SupportsCustomArchitectures = true,
                SupportedCustomArchitectures = new Architecture[] { Architecture.X86, Architecture.X64, Architecture.Arm64, Architecture.Arm },
                SupportsPreRelease = true,
                SupportsCustomLocations = true,
            };
        }

        protected override ManagerProperties GetProperties()
        {
            ManagerProperties properties = new ManagerProperties()
            {
                Name = ".NET Tool",
                Description = bindings.Translate("A repository full of tools and executables designed with Microsoft's .NET ecosystem in mind.<br>Contains: <b>.NET related tools and scripts</b>"),
                IconId = "dotnet",
                ColorIconId = "dotnet_color",
                ExecutableFriendlyName = "dotnet tool",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "update",
                ExecutableCallArgs = "tool",
                
            };
            return properties;
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            var status = new ManagerStatus();

            status.ExecutablePath = await bindings.Which("dotnet.exe");
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
    }
}
