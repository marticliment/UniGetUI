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
using ModernWindow.Structures;

namespace ModernWindow.PackageEngine.Managers
{
    public class PowerShell : PackageManagerWithSources
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
                Arguments = Properties.ExecutableCallArgs + " Find-Module \"" + query + "\"",
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
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                AppTools.Log(DashesPassed.ToString() + ": " + line);
                if (!DashesPassed)
                {
                    if(line.Contains("-----"))
                        DashesPassed = true;
                }
                else
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                    if (elements.Length < 3)
                        continue;

                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                    if (SourceReference.ContainsKey(elements[2]))
                        Packages.Add(new Package(bindings.FormatAsName(elements[1]), elements[1], elements[0], SourceReference[elements[2]], this));
                    else
                    {
                        AppTools.Log("Unknown PowerShell source!");
                        var s = new ManagerSource(this, elements[2], new Uri("https://www.powershellgallery.com/api/v2"));
                        Packages.Add(new Package(bindings.FormatAsName(elements[1]), elements[1], elements[0], s, this));
                        SourceReference.Add(s.Name, s);
                    }   
                }
            }

            output += await p.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, p, output);

            await p.WaitForExitAsync();

            return Packages.ToArray();
        }

        protected override async Task<UpgradablePackage[]> GetAvailableUpdates_UnSafe()
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = "",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            p.Start();

            var sources = await GetSources();

            string SourceDict = "{";
            foreach (var source in sources)
            {
                SourceDict += "\"" + source.Name + "\" = \"" + source.Url.ToString() + "\";";
            }
            SourceDict += "}";
            await p.StandardInput.WriteLineAsync(@"
                function Test-GalleryModuleUpdate {
                    param (
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $Name,
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [version] $Version,
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $Repository,
                        [switch] $NeedUpdateOnly
                    )
                    process {
                        $URLs = @" + SourceDict + @"
                        $page = Invoke-WebRequest -Uri ($URLs[$Repository] + ""/package/$Name"") -UseBasicParsing -Maximum 0 -ea Ignore
                        [version]$latest = Split-Path -Path ($page.Headers.Location -replace ""$Name."" -replace "".nupkg"") -Leaf
                        $needsupdate = $Latest -gt $Version
                        if ($needsupdate) {
                            Write-Output ($Name + ""|"" + $Version.ToString() + ""|"" + $Latest.ToString() + ""|"" + $Repository)
                        }
                    }
                }
                Get-InstalledModule | Test-GalleryModuleUpdate
                exit
                "); // do NOT remove the trailing endline
            string line;
            List<UpgradablePackage> Packages = new();
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (line.StartsWith(">>"))
                    continue;

                string[] elements = line.Split('|');
                if (elements.Length < 4)
                    continue;

                for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                if (elements[1] + ".0" == elements[2] || elements[1] + ".0.0" == elements[2])
                    continue;

                if (SourceReference.ContainsKey(elements[3]))
                    Packages.Add(new UpgradablePackage(bindings.FormatAsName(elements[0]), elements[0], elements[1], elements[2], SourceReference[elements[3]], this));
                else
                {
                    var s = new ManagerSource(this, elements[3], new Uri("https://www.powershellgallery.com/api/v2"));
                    Packages.Add(new UpgradablePackage(bindings.FormatAsName(elements[0]), elements[0], elements[1], elements[2], s, this));
                    SourceReference.Add(s.Name, s);
                }   
            }

            output += await p.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, p, output);
            await p.WaitForExitAsync();

            return Packages.ToArray();
        }

        protected override async Task<Package[]> GetInstalledPackages_UnSafe()
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " Get-InstalledModule",
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
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!DashesPassed)
                {
                    if(line.Contains("-----"))
                        DashesPassed = true;
                }
                else
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                    if (elements.Length < 3)
                        continue;

                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                    if (SourceReference.ContainsKey(elements[2]))
                        Packages.Add(new Package(bindings.FormatAsName(elements[1]), elements[1], elements[0], SourceReference[elements[2]], this));
                    else
                    {
                        AppTools.Log("Unknown PowerShell source!");
                        var s = new ManagerSource(this, elements[2], new Uri("https://www.powershellgallery.com/api/v2"));
                        Packages.Add(new Package(bindings.FormatAsName(elements[1]), elements[1], elements[0], s, this));
                        SourceReference.Add(s.Name, s);
                    }   
                }
            }

            output += await p.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, p, output);
            await p.WaitForExitAsync();

            return Packages.ToArray();
        }

        public override OperationVeredict GetInstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return GetUninstallOperationVeredict(package, options, ReturnCode, Output);
        }

        public override OperationVeredict GetUpdateOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return GetUninstallOperationVeredict(package, options, ReturnCode, Output);
        }

        public override OperationVeredict GetUninstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            var output_string = string.Join("\n", Output);

            if(output_string.Contains("AdminPrivilegesAreRequired") && !options.RunAsAdministrator)
            {
                options.RunAsAdministrator = true;
                return OperationVeredict.AutoRetry;
            }

            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }
        public override string[] GetInstallParameters(Package package, InstallationOptions options)
        {
            var parameters = GetUpdateParameters(package, options).ToList();
            parameters[0] = Properties.InstallVerb;

            parameters.AddRange(new string[] { "-AllowClobber" });
            if (package.Scope == PackageScope.Global)
                parameters.AddRange(new string[] { "-Scope", "AllUsers" });
            else
                parameters.AddRange(new string[] { "-Scope", "CurrentUser" });

            if(options.Version != "")
                parameters.AddRange(new string[] { "-RequiredVersion", options.Version });

            return parameters.ToArray();
            
        }
        public override string[] GetUpdateParameters(Package package, InstallationOptions options)
        {
            var parameters = GetUninstallParameters(package, options).ToList();
            parameters[0] = Properties.UpdateVerb;

            if(options.PreRelease)
                parameters.Add("-AllowPrerelease");

            if(options.SkipHashCheck)
                parameters.Add("-SkipPublisherCheck");

            return parameters.ToArray();
        }

        public override string[] GetUninstallParameters(Package package, InstallationOptions options)
        {
            var parameters = new List<string>() { Properties.UninstallVerb, "-Name", package.Id, "-Confirm:$false", "-Force" };

            if(options.CustomParameters != null)
                parameters.AddRange(options.CustomParameters);

            return parameters.ToArray();
        }
        public override ManagerSource GetMainSource()
        {
            return new ManagerSource(this, "PSGallery", new Uri("https://www.powershellgallery.com/api/v2"));
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
            string output = "";
            while ((line = await process.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
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
                    AppTools.Log(e);
                }
            }
            output += await process.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, process, output);
            await process.WaitForExitAsync();
            return sources.ToArray();
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
                UninstallVerb = "Uninstall-Module",
                UpdateVerb = "Update-Module",
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
                    RedirectStandardError = true,
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
            var p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " Find-Module -Name " + package.Id + " -AllVersions",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            p.Start();

            string line;
            List<string> versions = new();
            bool DashesPassed = false;
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!DashesPassed)
                {
                    if(line.Contains("-----"))
                        DashesPassed = true;
                }
                else
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                    if (elements.Length < 3)
                        continue;

                    versions.Add(elements[0]);
                }
            }

            return versions.ToArray();
        }
    }

}
