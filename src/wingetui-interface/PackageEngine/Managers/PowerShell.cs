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
    public class PowerShell : PackageManagerWithSources
    {

        public override async Task<Package[]> FindPackages(string query)
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " Find-Module " + query,
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
                Console.WriteLine(DashesPassed.ToString() + ": " + line);
                if (!DashesPassed)
                {
                    if(line.Contains("-----"))
                        DashesPassed = true;
                }
                else
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                    if(elements.Length >= 3)
                        if(SourceReference.ContainsKey(elements[2]))
                            Packages.Add(new Package(bindings.FormatAsName(elements[1]), elements[1], elements[0], SourceReference[elements[2]], this));
                        else
                        {
                            Console.WriteLine("Unknown PowerShell source!");
                            var s = new ManagerSource(this, elements[2], new Uri("https://www.powershellgallery.com/api/v2"));
                            Packages.Add(new Package(bindings.FormatAsName(elements[1]), elements[1], elements[0], s, this));
                            SourceReference.Add(s.Name, s);
                        }   
                    else
                        Console.WriteLine("NOLENGTH");
                }
            }

            await p.WaitForExitAsync();

            return Packages.ToArray();
        }

        public override Task<UpgradablePackage[]> GetAvailableUpdates()
        {
            throw new NotImplementedException();
        }

        public override Task<Package[]> GetInstalledPackages()
        {
            throw new NotImplementedException();
        }

        public override string[] GetInstallParameters(Package package, InstallationOptions options)
        {
            throw new NotImplementedException();
        }

        public override ManagerSource GetMainSource()
        {
            return new ManagerSource(this, "PSGallery", new Uri("https://www.powershellgallery.com/api/v2"));
        }

        public override Task<PackageDetails> GetPackageDetails(Package package)
        {
            throw new NotImplementedException();
        }

        public override async Task<ManagerSource[]> GetSources()
        {
            List<ManagerSource> sources = new List<ManagerSource>();

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " Get-PSRepository",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.StartInfo = startInfo;
            process.Start();

            bool dashesPassed = false;
            string line;
            while ((line = await process.StandardOutput.ReadLineAsync()) != null)
            {
                try {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    if (!dashesPassed) {
                        if (line.Contains("---"))
                            dashesPassed = true;
                    } else {
                        string[] parts = Regex.Replace(line.Trim(), " {2,}", " ").Split(' ');
                        if(parts.Length >= 3)
                            sources.Add(new ManagerSource(this, parts[0].Trim(), new Uri(parts[2].Trim())));
                    }
                } catch (Exception e) {
                    Console.WriteLine(e);
                }
            }
            await process.WaitForExitAsync();
            return sources.ToArray();
        }

        public override string[] GetUninstallParameters(Package package, InstallationOptions options)
        {
            throw new NotImplementedException();
        }

        public override string[] GetUpdateParameters(Package package, InstallationOptions options)
        {
            throw new NotImplementedException();
        }

#pragma warning disable CS1998
        public override async Task RefreshSources()
        {
            // PowerShell does not allow manual refresh of sources;
        }

        protected override ManagerCapabilities GetCapabilities()
        {
            return new ManagerCapabilities()
            {
                CanRunAsAdmin = true,
                CanSkipIntegrityChecks = true,
                SupportsCustomVersions = true,
                SupportsCustomScopes = true,
                SupportsCustomSources = true,
                SupportsPreRelease = true,
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
                Name = "PowerShell",
                Description = bindings.Translate("PowerShell's package manager. Find libraries and scripts to expand PowerShell capabilities<br>Contains: <b>Modules, Scripts, Cmdlets</b>"),
                IconId = "powershell",
                ColorIconId = "powershell_color",
                ExecutableFriendlyName = "powershell.exe",
                InstallVerb = "Install-Module",
                UninstallVerb = "Update-Module",
                UpdateVerb = "Uninstall-Module",
                ExecutableCallArgs = "-NoProfile -Command",
                
            };
            return properties;
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            var status = new ManagerStatus
            {
                ExecutablePath = Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe")
            };
            status.Found = File.Exists(status.ExecutablePath);

            if(!status.Found)
                return status;

            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " \"echo $PSVersionTable\"",
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
