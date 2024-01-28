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
    public class Pip : PackageManager
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "", "WARNING:", "[notice]", "Package" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] { "", "WARNING:", "[notice]", "Package" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "", "Ignoring", "invalid" };
        public override async Task<Package[]> FindPackages_UnSafe(string query)
        {
            var Packages = new List<Package>();

            string path = await bindings.Which("parse_pip_search");
            if(!File.Exists(path))
                {
                    Process proc = new Process() {
                        StartInfo = new ProcessStartInfo()
                        {
                            FileName = path,
                            Arguments = Properties.ExecutableCallArgs + " install parse_pip_search",
                            UseShellExecute = true,
                            CreateNoWindow = true
                        }
                    };
                    proc.Start();
                    await proc.WaitForExitAsync();
                    path = "parse_pip_search.exe";
                }

            Process p = new Process() {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = path,
                    Arguments = query,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            p.Start();

            string line;
            bool DashesPassed = false;
            while((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                if(!DashesPassed)
                {
                    if(line.Contains("----"))
                        DashesPassed = true;
                } else {
                    string[] elements = line.Split('|');
                    if (elements.Length < 2)
                        continue;

                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();
                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;
                    
                    Packages.Add(new Package(bindings.FormatAsName(elements[0]), elements[0], elements[1], MainSource, this));
                }
            }
            return Packages.ToArray();
        }

        public override Task<UpgradablePackage[]> GetAvailableUpdates_UnSafe()
        {
            throw new NotImplementedException();
        }

        public override Task<Package[]> GetInstalledPackages_UnSafe()
        {
            throw new NotImplementedException();
        }

        public override string[] GetInstallParameters(Package package, InstallationOptions options)
        {
            throw new NotImplementedException();
        }

        public override ManagerSource GetMainSource()
        {
            return new ManagerSource(this, "pip", new Uri("https://pypi.org/"));
        }

        public override Task<PackageDetails> GetPackageDetails_UnSafe(Package package)
        {
            throw new NotImplementedException();
        }

        public override string[] GetUninstallParameters(Package package, InstallationOptions options)
        {
            throw new NotImplementedException();
        }

        public override string[] GetUpdateParameters(Package package, InstallationOptions options)
        {
            throw new NotImplementedException();
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async Task RefreshSources()
        {
            // Pip does not support manual source refreshing
        }

        protected override ManagerCapabilities GetCapabilities()
        {
            return new ManagerCapabilities()
            {
                CanRunAsAdmin = true,
                SupportsCustomVersions = true,
                SupportsCustomScopes = true,
                SupportsPreRelease = true,
            };
        }

        protected override ManagerProperties GetProperties()
        {
            ManagerProperties properties = new ManagerProperties()
            {
                Name = "Pip",
                Description = bindings.Translate("Python's library manager. Full of python libraries and other python-related utilities<br>Contains: <b>Python libraries and related utilities</b>"),
                IconId = "python",
                ColorIconId = "pip_color",
                ExecutableFriendlyName = "pip",
                InstallVerb = "install",
                UninstallVerb = "instal --upgrade",
                UpdateVerb = "uninstall",
                ExecutableCallArgs = "-m pip",
                
            };
            return properties;
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            var status = new ManagerStatus();

            status.ExecutablePath = await bindings.Which("python.exe");
            status.Found = File.Exists(status.ExecutablePath);

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
