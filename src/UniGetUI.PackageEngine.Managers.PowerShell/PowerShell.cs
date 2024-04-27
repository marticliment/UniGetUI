using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UniGetUI.Core;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;

namespace UniGetUI.PackageEngine.Managers.PowerShellManager
{
    public class PowerShell : PackageManager
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] { "" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "" };

        public PowerShell() : base()
        {
            Capabilities = new ManagerCapabilities()
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

            Properties = new ManagerProperties()
            {
                Name = "PowerShell",
                Description = CoreTools.Translate("PowerShell's package manager. Find libraries and scripts to expand PowerShell capabilities<br>Contains: <b>Modules, Scripts, Cmdlets</b>"),
                IconId = "powershell",
                ColorIconId = "powershell_color",
                ExecutableFriendlyName = "powershell.exe",
                InstallVerb = "Install-Module",
                UninstallVerb = "Uninstall-Module",
                UpdateVerb = "Update-Module",
                ExecutableCallArgs = " -NoProfile -Command",
                KnownSources = [new(this, "PSGallery", new Uri("https://www.powershellgallery.com/api/v2")),
                                new(this, "PoshTestGallery", new Uri("https://www.poshtestgallery.com/api/v2"))],
                DefaultSource = new ManagerSource(this, "PSGallery", new Uri("https://www.powershellgallery.com/api/v2")),
            };

            SourceProvider = new PowerShellSourceProvider(this);
        }

        protected override async Task<Package[]> FindPackages_UnSafe(string query)
        {
            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " Find-Module \"" + query + "\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            p.Start();
            await p.StandardInput.WriteLineAsync("");
            string line;
            List<Package> Packages = new();
            bool DashesPassed = false;
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!DashesPassed)
                {
                    if (line.Contains("-----"))
                        DashesPassed = true;
                }
                else
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                    if (elements.Length < 3)
                        continue;

                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                    Packages.Add(new Package(Core.Tools.CoreTools.FormatAsName(elements[1]), elements[1], elements[0], GetSourceOrDefault(elements[2]), this));
                }
            }

            output += await p.StandardError.ReadToEndAsync();
            LogOperation(p, output);

            await p.WaitForExitAsync();

            return Packages.ToArray();
        }

        protected override async Task<UpgradablePackage[]> GetAvailableUpdates_UnSafe()
        {
            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = "",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            p.Start();

            await p.StandardInput.WriteLineAsync(@"
                function Test-GalleryModuleUpdate {
                    param (
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $Name,
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [version] $Version,
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $Repository,
                        [switch] $NeedUpdateOnly
                    )
                    process {
                        $URLs = @{}
                        @(Get-PSRepository).ForEach({$URLs[$_.Name] = $_.SourceLocation})
                        $page = Invoke-WebRequest -Uri ($URLs[$Repository] + ""/package/$Name"") -UseBasicParsing -Maximum 0 -ea Ignore
                        [version]$latest = Split-Path -Path ($page.Headers.Location -replace ""$Name."" -replace "".nupkg"") -Leaf
                        $needsupdate = $Latest -gt $Version
                        if ($needsupdate) {
                                Write-Output($Name + ""|"" + $Version.ToString() + ""|"" + $Latest.ToString() + ""|"" + $Repository)
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

                Packages.Add(new UpgradablePackage(Core.Tools.CoreTools.FormatAsName(elements[0]), elements[0], elements[1], elements[2], GetSourceOrDefault(elements[3]), this));
            }

            output += await p.StandardError.ReadToEndAsync();
            LogOperation(p, output);
            await p.WaitForExitAsync();

            return Packages.ToArray();
        }

        protected override async Task<Package[]> GetInstalledPackages_UnSafe()
        {
            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " Get-InstalledModule",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
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
                    if (line.Contains("-----"))
                        DashesPassed = true;
                }
                else
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                    if (elements.Length < 3)
                        continue;

                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                    Packages.Add(new Package(CoreTools.FormatAsName(elements[1]), elements[1], elements[0], GetSourceOrDefault(elements[2]), this));
                }
            }

            output += await p.StandardError.ReadToEndAsync();
            LogOperation(p, output);
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
            string output_string = string.Join("\n", Output);

            if (output_string.Contains("AdminPrivilegesAreRequired") && !options.RunAsAdministrator)
            {
                options.RunAsAdministrator = true;
                return OperationVeredict.AutoRetry;
            }

            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }
        public override string[] GetInstallParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = GetUpdateParameters(package, options).ToList();
            parameters[0] = Properties.InstallVerb;

            parameters.AddRange(new string[] { "-AllowClobber" });
            if (package.Scope == PackageScope.Global)
                parameters.AddRange(new string[] { "-Scope", "AllUsers" });
            else
                parameters.AddRange(new string[] { "-Scope", "CurrentUser" });

            if (options.Version != "")
                parameters.AddRange(new string[] { "-RequiredVersion", options.Version });

            return parameters.ToArray();

        }
        public override string[] GetUpdateParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = GetUninstallParameters(package, options).ToList();
            parameters[0] = Properties.UpdateVerb;

            if (options.PreRelease)
                parameters.Add("-AllowPrerelease");

            if (options.SkipHashCheck)
                parameters.Add("-SkipPublisherCheck");

            return parameters.ToArray();
        }

        public override string[] GetUninstallParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = new() { Properties.UninstallVerb, "-Name", package.Id, "-Confirm:$false", "-Force" };

            if (options.CustomParameters != null)
                parameters.AddRange(options.CustomParameters);

            return parameters.ToArray();
        }

        public override async Task<PackageDetails> GetPackageDetails_UnSafe(Package package)
        {
            PackageDetails details = new(package);

            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " Find-Module -Name " + package.Id + " | Get-Member -MemberType NoteProperty | Select-Object Definition",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            p.Start();

            string line;
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                if (line.Count(c => c == ' ') < 1)
                    continue;

                try
                {
                    string entry = line.Split('=')[0].Split(' ')[^1];
                    string PossibleContent = line.Split('=')[1].Trim();
                    if (PossibleContent == "null")
                        continue;

                    if (entry == "Author")
                        details.Author = PossibleContent;

                    else if (entry == ("CompanyName"))
                        details.Publisher = PossibleContent;

                    else if (entry == ("Copyright"))
                        details.License = PossibleContent;

                    else if (entry == ("LicenseUri"))
                        details.LicenseUrl = new Uri(PossibleContent);

                    else if (entry == ("Description"))
                        details.Description = PossibleContent;

                    else if (entry == ("Type"))
                        details.InstallerType = PossibleContent;

                    else if (entry == ("ProjectUri"))
                        details.HomepageUrl = new Uri(PossibleContent);

                    else if (entry == ("PublishedDate"))
                        details.UpdateDate = PossibleContent;

                    else if (entry == ("UpdatedDate"))
                        details.UpdateDate = PossibleContent;

                    else if (entry == ("ReleaseNotes"))
                        details.ReleaseNotes = PossibleContent;

                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }

            }

            return details;
        }

        

#pragma warning disable CS1998
        public override async Task RefreshPackageIndexes()
        {
            // PowerShell does not allow manual refresh of sources;
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            ManagerStatus status = new()
            {
                ExecutablePath = Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe")
            };
            status.Found = File.Exists(status.ExecutablePath);

            if (!status.Found)
                return status;

            Process process = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " \"echo $PSVersionTable\"",
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
        protected override async Task<string[]> GetPackageVersions_Unsafe(Package package)
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " Find-Module -Name " + package.Id + " -AllVersions",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
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
                    if (line.Contains("-----"))
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
