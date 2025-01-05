using System.Diagnostics;
using System.Text;
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

namespace UniGetUI.PackageEngine.Managers.PowerShell7Manager
{
    public class PowerShell7 : BaseNuGet
    {
        public PowerShell7()
        {
            Capabilities = new ManagerCapabilities
            {
                CanRunAsAdmin = true,
                SupportsCustomVersions = true,
                SupportsCustomScopes = true,
                SupportsCustomSources = true,
                SupportsPreRelease = true,
                SupportsCustomPackageIcons = true,
                Sources = new SourceCapabilities
                {
                    KnowsPackageCount = false,
                    KnowsUpdateDate = false,
                }
            };

            Properties = new ManagerProperties
            {
                Name = "PowerShell7",
                DisplayName = "PowerShell 7.x",
                Description = CoreTools.Translate("PowerShell's package manager. Find libraries and scripts to expand PowerShell capabilities<br>Contains: <b>Modules, Scripts, Cmdlets</b>"),
                IconId = IconType.PowerShell,
                ColorIconId = "powershell_color",
                ExecutableFriendlyName = "pwsh.exe",
                InstallVerb = "Install-PSResource",
                UninstallVerb = "Uninstall-PSResource",
                UpdateVerb = "Update-PSResource",
                ExecutableCallArgs = " -NoProfile -Command",
                KnownSources = [new ManagerSource(this, "PSGallery", new Uri("https://www.powershellgallery.com/api/v2")),
                                new ManagerSource(this, "PoshTestGallery", new Uri("https://www.poshtestgallery.com/api/v2"))],
                DefaultSource = new ManagerSource(this, "PSGallery", new Uri("https://www.powershellgallery.com/api/v2")),
            };

            DetailsHelper = new PowerShell7DetailsHelper(this);
            SourcesHelper = new PowerShell7SourceHelper(this);
            OperationHelper = new PowerShell7PkgOperationHelper(this);
        }
        protected override IEnumerable<Package> GetAvailableUpdates_UnSafe()
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Status.ExecutablePath,
                    Arguments = "",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardInputEncoding = new UTF8Encoding(false),
                }
            };

            IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListUpdates, p);

            p.Start();

            string command = """
                function Get-MaxVersion {
                    param (
                        [Parameter(Mandatory, ValueFromPipeline)] [PSCustomObject] $InputObject
                    )

                    begin {
                        $maxVersions = @{}
                        $moduleObjects = @{}
                    }

                    process {
                        if (-not $maxVersions.ContainsKey($InputObject.Name)) {
                            $maxVersions[$InputObject.Name] = $InputObject.Version
                            $moduleObjects[$InputObject.Name] = $InputObject
                        } elseif ($InputObject.Version -gt $maxVersions[$InputObject.Name]) {
                            $maxVersions[$InputObject.Name] = $InputObject.Version
                            $moduleObjects[$InputObject.Name] = $InputObject
                        }
                    }

                    end {
                        $moduleObjects.GetEnumerator() | ForEach-Object {
                            $_.Value
                        }
                    }
                }

                function Test-GalleryModuleUpdate_Legacy {
                    param (
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $Name,
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [version] $Version,
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $Repository,
                        [switch] $NeedUpdateOnly
                    )
                    process {
                        $URLs = @{}
                        @(Get-PSRepository).ForEach({$URLs[$_.Name] = If ($_.Uri) {$_.Uri.AbsoluteUri} Else {$_.SourceLocation}})
                        $page = Invoke-WebRequest -Uri ($URLs[$Repository] + "/package/$Name") -UseBasicParsing -ea Ignore
                        [version]$latest = Split-Path -Path ($page.BaseResponse.RequestMessage.RequestUri -replace "$Name." -replace ".nupkg") -Leaf
                        $needsupdate = $Latest -gt $Version
                        if ($needsupdate) {
                                Write-Output($Name + "|" + $Version.ToString() + "|" + $Latest.ToString() + "|" + $Repository)
                        }
                    }
                }

                function Test-GalleryModuleUpdate {
                    param (
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $Name,
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [version] $Version,
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $Repository,
                        [switch] $NeedUpdateOnly
                    )
                    process {
                        $URLs = @{}
                        @(Get-PSRepository).ForEach({$URLs[$_.Name] = If ($_.Uri) {$_.Uri.AbsoluteUri} Else {$_.SourceLocation}})

                        $packageUrl = "$($URLs[$Repository])/FindPackagesById()?id='$Name'&`$filter=IsLatestVersion"
                        $page = Invoke-WebRequest -Uri $packageUrl -UseBasicParsing -ea Ignore
                        $latestVersionMatch = [regex]::Match($page.Content, '<d:Version>(.*?)</d:Version>')
                        if ($latestVersionMatch.Success) {
                            [version]$latest = $latestVersionMatch.Groups[1].Value
                            $needsupdate = $latest -gt $Version
                            if ($needsupdate) {
                                    Write-Output($Name + "|" + $Version.ToString() + "|" + $latest.ToString() + "|" + $Repository)
                            }
                        } else {
                            Write-Warning("Could not parse version for package " + $Name)
                            Test-GalleryModuleUpdate_Legacy -Name $Name -Version $Version -Repository $Repository -NeedUpdateOnly:$NeedUpdateOnly
                        }
                    }
                }

                Get-PSResource | Get-MaxVersion | Test-GalleryModuleUpdate



                exit
                """;
            p.StandardInput.WriteLine(command);
            logger.AddToStdIn(command);
            p.StandardInput.Close();

            string? line;
            List<Package> Packages = [];
            while ((line = p.StandardOutput.ReadLine()) is not null)
            {
                logger.AddToStdOut(line);
                if (line.StartsWith(">>"))
                {
                    continue;
                }

                string[] elements = line.Split('|');
                if (elements.Length < 4)
                {
                    continue;
                }

                for (int i = 0; i < elements.Length; i++)
                {
                    elements[i] = elements[i].Trim();
                }

                if (elements[1] + ".0" == elements[2] || elements[1] + ".0.0" == elements[2])
                {
                    continue;
                }

                Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1],
                    elements[2], SourcesHelper.Factory.GetSourceOrDefault(elements[3]), this));
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
                    Arguments = Properties.ExecutableCallArgs + " \"Get-InstalledPSResource | Format-Table -Property Name,Version,Repository\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                }
            };

            IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListInstalledPackages, p);

            p.Start();
            string? line;
            List<Package> Packages = [];
            bool DashesPassed = false;
            while ((line = p.StandardOutput.ReadLine()) is not null)
            {
                logger.AddToStdOut(line);
                if (!DashesPassed)
                {
                    if (line.Contains("-----"))
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

                    Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1],
                        SourcesHelper.Factory.GetSourceOrDefault(elements[2]), this));
                }
            }

            logger.AddToStdErr(p.StandardError.ReadToEnd());
            p.WaitForExit();
            logger.Close(p.ExitCode);

            return Packages;
        }
        protected override ManagerStatus LoadManager()
        {
            var (found, path) = CoreTools.Which("pwsh.exe");

            ManagerStatus status = new()
            {
                ExecutablePath = path,
                Found = found,
            };

            if (!status.Found)
            {
                return status;
            }

            Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = status.ExecutablePath,
                    Arguments = " -Version",
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
