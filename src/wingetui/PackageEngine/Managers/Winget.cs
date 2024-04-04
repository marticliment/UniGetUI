using CommunityToolkit.Common;
using ModernWindow.Core.Data;
using ModernWindow.PackageEngine.Classes;
using ModernWindow.PackageEngine.Operations;
using ModernWindow.Structures;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Services.TargetedContent;
using Windows.Storage.Streams;
using Deployment = Microsoft.Management.Deployment;

using WindowsPackageManager.Interop;
using Windows.Foundation;
using System.Collections;
using Microsoft.Management.Deployment;
using System.ComponentModel;
using CommunityToolkit.WinUI.Animations;
using Microsoft.UI.Xaml.Controls;


namespace ModernWindow.PackageEngine.Managers
{
    public class Winget : PackageManagerWithSources
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "", "e(s)", "have", "the", "Id" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] { "", "e(s)", "have", "an", "'winget", "pin'", "have", "an", "Version" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "", "have", "an", "'winget", "pin'", "have", "an", "Version" };
        private static LocalPcSource LocalPcSource { get; } = new LocalPcSource();
        private static AndroidSubsystemSource AndroidSubsystemSource { get; } = new AndroidSubsystemSource();
        private static SteamSource SteamSource { get; } = new SteamSource();
        private static UbisoftConnectSource UbisoftConnectSource { get; } = new UbisoftConnectSource();
        private static GOGSource GOGSource { get; } = new GOGSource();
        private static MicrosoftStoreSource MicrosoftStoreSource { get; } = new MicrosoftStoreSource();

        private WindowsPackageManagerFactory WinGetFactory;
        private Deployment.PackageManager WinGetManager;


        protected override async Task<Package[]> FindPackages_UnSafe(string query)
        {
            List<Package> Packages = new();
            var PackageFilters = WinGetFactory.CreateFindPackagesOptions();

            // Name filter
            var FilterName = WinGetFactory.CreatePackageMatchFilter();
            FilterName.Field = Deployment.PackageMatchField.Name;
            FilterName.Value = query;
            FilterName.Option = Deployment.PackageFieldMatchOption.ContainsCaseInsensitive;
            PackageFilters.Filters.Add(FilterName);

            // Id filter
            var FilterId = WinGetFactory.CreatePackageMatchFilter();
            FilterId.Field = Deployment.PackageMatchField.Name;
            FilterId.Value = query;
            FilterId.Option = Deployment.PackageFieldMatchOption.ContainsCaseInsensitive;
            PackageFilters.Filters.Add(FilterId);

            // Load catalogs
            var AvailableCatalogs = WinGetManager.GetPackageCatalogs();
            Dictionary<Deployment.PackageCatalogReference, Task<Deployment.FindPackagesResult>> FindPackageTasks = new();

            // Spawn Tasks to find packages on catalogs
            foreach (var CatalogReference in AvailableCatalogs.ToArray())
            {
                // Connect to catalog
                CatalogReference.AcceptSourceAgreements = true;
                var result = await CatalogReference.ConnectAsync();
                if (result.Status == Deployment.ConnectResultStatus.Ok)
                {
                    try
                    {
                        // Create task and spawn it
                        var task = new Task<Deployment.FindPackagesResult>(() => result.PackageCatalog.FindPackages(PackageFilters));
                        task.Start();

                        // Add task to list
                        FindPackageTasks.Add(
                            CatalogReference,
                            task
                        );
                    }
                    catch (Exception e)
                    {
                        AppTools.Log("WinGet: Catalog " + CatalogReference.Info.Name + " failed to spawn FindPackages task.");
                        AppTools.Log(e);
                    }
                }
                else
                {
                    AppTools.Log("WinGet: Catalog " + CatalogReference.Info.Name + " failed to connect.");
                }
            }

            // Wait for tasks completion
            await Task.WhenAll(FindPackageTasks.Values.ToArray());

            foreach(var CatalogTaskPair in FindPackageTasks)
            {   
                try
                {
                    // Get the source for the catalog
                    ManagerSource source = SourceFactory.GetSourceOrDefault(CatalogTaskPair.Key.Info.Name);

                    var FoundPackages = CatalogTaskPair.Value.Result;
                    foreach(var package in FoundPackages.Matches.ToArray())
                    {
                        // Create the Package item and add it to the list
                        Packages.Add(new Package(
                            package.CatalogPackage.Name,
                            package.CatalogPackage.Id,
                            package.CatalogPackage.DefaultInstallVersion.Version,
                            source,
                            this
                        ));
                    }
                }
                catch (Exception e)
                {
                    AppTools.Log("WinGet: Catalog " + CatalogTaskPair.Key.Info.Name + " failed to get available packages.");
                    AppTools.Log(e);
                }
            }

            return Packages.ToArray();
        }

        protected override async Task<UpgradablePackage[]> GetAvailableUpdates_UnSafe()
        {
            var Packages = new List<UpgradablePackage>();

            Process p = new Process();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            p.Start();

            await p.StandardInput.WriteAsync(@"
                function Print-WinGetPackage {
                    param (
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $Name,
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $Id,
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $InstalledVersion,
                        [Parameter(ValueFromPipelineByPropertyName)] [string[]] $AvailableVersions,
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [bool] $IsUpdateAvailable,
                        [Parameter(ValueFromPipelineByPropertyName)] [string] $Source
                    )
                    process {
                        if($IsUpdateAvailable)
                        {
                            Write-Output(""#"" + $Name + ""`t"" + $Id + ""`t"" + $InstalledVersion + ""`t"" + $AvailableVersions[0] + ""`t"" + $Source)
                        }
                    }
                }

                if(!(Get-Command -Verb Get -Noun WinGetPackage))
                {
                    Install-Module -Name Microsoft.WinGet.Client -Scope CurrentUser -AllowClobber -Confirm:$false -Force
                }
                Get-WinGetPackage | Print-WinGetPackage
                
                exit
                ");

            string line;
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!line.StartsWith("#"))
                    continue; // The PowerShell script appends a '#' to the beginning of each line to identify the output

                string[] elements = line.Split('\t');
                if (elements.Length < 5)
                    continue;

                ManagerSource source = SourceFactory.GetSourceOrDefault(elements[4]);

                Packages.Add(new UpgradablePackage(elements[0][1..], elements[1], elements[2], elements[3], source, this));
            }

            output += await p.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, p, output);
            await p.WaitForExitAsync();

            return Packages.ToArray();
        }

        protected override async Task<Package[]> GetInstalledPackages_UnSafe()
        {
            var Packages = new List<Package>();

            Process p = new Process();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            p.Start();

            await p.StandardInput.WriteAsync(@"
                function Print-WinGetPackage {
                    param (
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $Name,
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $Id,
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $InstalledVersion,
                        [Parameter(ValueFromPipelineByPropertyName)] [string[]] $AvailableVersions,
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [bool] $IsUpdateAvailable,
                        [Parameter(ValueFromPipelineByPropertyName)] [string] $Source
                    )
                    process {
                        Write-Output(""#"" + $Name + ""`t"" + $Id + ""`t"" + $InstalledVersion + ""`t"" + $Source)
                    }
                }

                if(!(Get-Command -Verb Get -Noun WinGetPackage))
                {
                    Install-Module -Name Microsoft.WinGet.Client -Scope CurrentUser -AllowClobber -Confirm:$false -Force
                }
                Get-WinGetPackage | Print-WinGetPackage
                
                exit
                ");

            string line;
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!line.StartsWith("#"))
                    continue; // The PowerShell script appends a '#' to the beginning of each line to identify the output

                string[] elements = line.Split('\t');
                if (elements.Length < 4)
                    continue;

                ManagerSource source;
                if (elements[3] != "")
                    source = SourceFactory.GetSourceOrDefault(elements[3]);
                else
                    source = GetLocalSource(elements[1]);

                Packages.Add(new Package(elements[0][1..], elements[1], elements[2], source, this));
            }

            output += await p.StandardError.ReadToEndAsync();
            AppTools.LogManagerOperation(this, p, output);
            await p.WaitForExitAsync();

            return Packages.ToArray();
        }

        private ManagerSource GetLocalSource(string id)
        {
            try
            {
                // Check if source is android
                bool AndroidValid = true;
                foreach (char c in id)
                    if (!"abcdefghijklmnopqrstuvwxyz.".Contains(c))
                    {
                        AndroidValid = false;
                        break;
                    }
                if (AndroidValid && id.Count(x => x == '.') >= 2)
                    return AndroidSubsystemSource;

                // Check if source is Steam
                if ((id == "Steam" || id.Contains("Steam App ")) && id.Split("Steam App").Count() >= 2 && id.Split("Steam App")[1].Trim().Count(x => !"1234567890".Contains(x)) == 0)
                    return SteamSource;

                // Check if source is Ubisoft Connect
                if (id == "Uplay" || id.Contains("Uplay Install ") && id.Split("Uplay Install").Count() >= 2 && id.Split("Uplay Install")[1].Trim().Count(x => !"1234567890".Contains(x)) == 0)
                    return UbisoftConnectSource;

                // Check if source is GOG
                if (id.EndsWith("_is1") && id.Split("_is1")[0].Count(x => !"1234567890".Contains(x)) == 0)
                    return GOGSource;

                // Check if source is Microsoft Store
                if (id.Count(x => x == '_') == 1 && (id.Split('_')[^1].Length == 14 | id.Split('_')[^1].Length == 13))
                    return MicrosoftStoreSource;

                // Otherwise, Source is localpc
                return LocalPcSource;
            }
            catch (Exception ex)
            {
                AppTools.Log(ex);
                return LocalPcSource;
            }
        }

        public override string[] GetInstallParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = GetUninstallParameters(package, options).ToList();
            parameters[0] = Properties.InstallVerb;

            parameters.Add("--accept-package-agreements");

            if (options.SkipHashCheck)
                parameters.Add("--ignore-security-hash");

            if (options.CustomInstallLocation != "")
            {
                parameters.Add("--location"); parameters.Add("\"" + options.CustomInstallLocation + "\"");
            }

            switch (options.Architecture)
            {
                case (null):
                    break;
                case (Architecture.X86):
                    parameters.Add("--architecture"); parameters.Add("x86");
                    break;
                case (Architecture.X64):
                    parameters.Add("--architecture"); parameters.Add("x64");
                    break;
                case (Architecture.Arm64):
                    parameters.Add("--architecture"); parameters.Add("arm64");
                    break;
            }
            return parameters.ToArray();
        }
        public override string[] GetUpdateParameters(Package package, InstallationOptions options)
        {
            if (package.Name.Contains("64-bit") || package.Id.ToLower().Contains("x64"))
                options.Architecture = Architecture.X64;
            else if (package.Name.Contains("32-bit") || package.Id.ToLower().Contains("x86"))
                options.Architecture = Architecture.X86;

            string[] parameters = GetInstallParameters(package, options);
            parameters[0] = Properties.UpdateVerb;
            List<string> p = parameters.ToList();
            p.Add("--force");
            p.Add("--include-unknown");
            parameters = p.ToArray();
            return parameters;
        }

        public override string[] GetUninstallParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = new() { Properties.UninstallVerb };
            parameters.AddRange(new string[] { "--id", package.Id, "--exact" });

            parameters.Add("--accept-source-agreements");

            switch (options.InstallationScope)
            {
                case (PackageScope.Local):
                    parameters.Add("--scope"); parameters.Add("user");
                    break;
                case (PackageScope.Global):
                    parameters.Add("--scope"); parameters.Add("machine");
                    break;
            }

            if (options.Version != "")
                parameters.AddRange(new string[] { "--version", options.Version, "--force" });

            if (options.InteractiveInstallation)
                parameters.Add("--interactive");
            else
                parameters.AddRange(new string[] { "--silent", "--disable-interactivity" });

            return parameters.ToArray();
        }

        public override OperationVeredict GetInstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            string output_string = string.Join("\n", Output);

            if (ReturnCode == -1978334967) // Use https://www.rapidtables.com/convert/number/hex-to-decimal.html for easy UInt(hex) to Int(dec) conversion
                return OperationVeredict.Succeeded; // TODO: Needs restart
            else if (ReturnCode == -1978335215)
                return OperationVeredict.Failed; // TODO: Needs skip checksum

            if (output_string.Contains("No applicable upgrade found") || output_string.Contains("No newer package versions are available from the configured sources"))
                return OperationVeredict.Succeeded;

            if(output_string.Contains("winget settings --enable InstallerHashOverride"))
            {
                AppTools.Log("Enabling skip hash ckeck for winget...");
                Process p = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = CoreData.GSudoPath,
                        Arguments = Status.ExecutablePath + " " + Properties.ExecutableCallArgs + " settings --enable InstallerHashOverride",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8
                    }
                };
                p.Start();
                p.WaitForExit();
                return OperationVeredict.AutoRetry;
            }

            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override OperationVeredict GetUpdateOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return GetInstallOperationVeredict(package, options, ReturnCode, Output);
        }

        public override OperationVeredict GetUninstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            string output_string = string.Join("\n", Output);

            if (output_string.Contains("1603") || output_string.Contains("0x80070005") || output_string.Contains("Access is denied"))
            {
                options.RunAsAdministrator = true;
                return OperationVeredict.AutoRetry;
            }

            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }


        public override ManagerSource GetMainSource()
        {
            return new ManagerSource(this, "winget", new Uri("https://cdn.winget.microsoft.com/cache"));
        }

        public override async Task<PackageDetails> GetPackageDetails_UnSafe(Package package)
        {
            PackageDetails details = new(package);

            if (package.Source.Name == "winget")
                details.ManifestUrl = new Uri("https://github.com/microsoft/winget-pkgs/tree/master/manifests/"
                    + package.Id[0].ToString().ToLower() + "/"
                    + package.Id.Split('.')[0] + "/"
                    + String.Join("/", (package.Id.Contains('.') ? package.Id.Split('.')[1..] : package.Id.Split('.')))
                );
            else if (package.Source.Name == "msstore")
                details.ManifestUrl = new Uri("https://apps.microsoft.com/detail/" + package.Id);

            // Find the native package for the given Package object
            var Catalog = WinGetManager.GetPackageCatalogByName(package.Source.Name);
            if (Catalog == null)
            {
                AppTools.Log("Failed to get catalog " + package.Source.Name + ". Is the package local?");
                return details;
            }

            // Connect to catalog
            Catalog.AcceptSourceAgreements = true;
            var ConnectResult = await Task.Run(() => Catalog.Connect());
            if(ConnectResult.Status != Deployment.ConnectResultStatus.Ok)
            {
                AppTools.Log("Failed to connect to catalog " + package.Source.Name);
                return details;
            }

            // Match only the exact same Id
            var packageMatchFilter = WinGetFactory.CreateFindPackagesOptions();
            var filters = WinGetFactory.CreatePackageMatchFilter();
            filters.Field = Deployment.PackageMatchField.Id;
            filters.Value = package.Id;
            filters.Option = Deployment.PackageFieldMatchOption.Equals;
            packageMatchFilter.Filters.Add(filters);
            packageMatchFilter.ResultLimit = 1;
            var SearchResult = Task.Run(() => ConnectResult.PackageCatalog.FindPackages(packageMatchFilter));

            if(SearchResult.Result == null || SearchResult.Result.Matches == null || SearchResult.Result.Matches.Count() == 0)
            {
                AppTools.Log("WinGet: Failed to find package " + package.Id + " in catalog " + package.Source.Name);
                return details;
            }

            // Get the Native Package
            var NativePackage = SearchResult.Result.Matches.First().CatalogPackage;

            // Extract data from NativeDetails
            var NativeDetails = NativePackage.DefaultInstallVersion.GetCatalogPackageMetadata(Windows.System.UserProfile.GlobalizationPreferences.Languages[0]);

            if(NativeDetails.Author != "")
                details.Author = NativeDetails.Author;

            if (NativeDetails.Description != "")
                details.Description = NativeDetails.Description;

            if (NativeDetails.PackageUrl != "")
                details.HomepageUrl = new Uri(NativeDetails.PackageUrl);

            if (NativeDetails.License != "")
                details.License = NativeDetails.License;

            if (NativeDetails.LicenseUrl != "")
                details.LicenseUrl = new Uri(NativeDetails.LicenseUrl);

            if (NativeDetails.Publisher != "")
                details.Publisher = NativeDetails.Publisher;

            if (NativeDetails.ReleaseNotes != "")
                details.ReleaseNotes = NativeDetails.ReleaseNotes;

            if (NativeDetails.ReleaseNotesUrl != "")
                details.ReleaseNotesUrl = new Uri(NativeDetails.ReleaseNotesUrl);

            if (NativeDetails.Tags != null)
                details.Tags = NativeDetails.Tags.ToArray();

            Debug.WriteLine("Starting manual fetch");
            
            // There is no way yet to retrieve installer URLs right now so this part will be console-parsed.
            // TODO: Replace this code with native code when available on the COM api
            Process process = new();
            List<string> output = new();
            ProcessStartInfo startInfo = new()
            {
                FileName = Path.Join(CoreData.WingetUIExecutableDirectory, "PackageEngine", "Managers", "winget-cli_x64", "winget.exe"),
                Arguments = Properties.ExecutableCallArgs + " show --id " + package.Id + " --exact --disable-interactivity --accept-source-agreements --source " + package.Source.Name,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            process.StartInfo = startInfo;
            process.Start();

            // Retrieve the output
            string _line;
            while ((_line = await process.StandardOutput.ReadLineAsync()) != null)
                if (_line.Trim() != "")
                    output.Add(_line);

            // Parse the output
            foreach (string __line in output)
            {
                try 
                { 
                    AppTools.Log(__line);
                    string line = __line.Trim();
                    if (line.Contains("Installer SHA256:"))
                        details.InstallerHash = line.Split(":")[1].Trim();

                    else if (line.Contains("Installer Url:"))
                    {
                        details.InstallerUrl = new Uri(line.Replace("Installer Url:", "").Trim());
                        details.InstallerSize = await Tools.GetFileSizeAsync(details.InstallerUrl);
                    }
                    else if (line.Contains("Release Date:"))
                        details.UpdateDate = line.Split(":")[1].Trim();

                    else if (line.Contains("Installer Type:"))
                        details.InstallerType = line.Split(":")[1].Trim();
                }
                catch (Exception e)
                {
                    AppTools.Log("Error occurred while parsing line value=\"" + __line + "\"");
                    AppTools.Log(e.Message);
                }
            }

            // NativeDetails.Icons ICONS
            return details;
            
        }

        protected override async Task<ManagerSource[]> GetSources_UnSafe()
        {
            List<ManagerSource> sources = new();

            foreach(var catalog in await Task.Run(() => WinGetManager.GetPackageCatalogs().ToArray()))
                try {
                    sources.Add(new ManagerSource(this, catalog.Info.Name, new Uri(catalog.Info.Argument), updateDate: catalog.Info.LastUpdateTime.ToString()));
                } catch (Exception e) {
                    AppTools.Log(e);
                }

            return sources.ToArray();
        }


        public override async Task RefreshPackageIndexes()
        {
            await Task.Delay(0);
            // As of WinGet 1.6, WinGet does handle updating package indexes automatically
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
                SupportedCustomArchitectures = new Architecture[] { Architecture.X86, Architecture.X64, Architecture.Arm64 },
                SupportsCustomScopes = true,
                SupportsCustomLocations = true,
                SupportsCustomSources = true,
                Sources = new ManagerSource.Capabilities()
                {
                    KnowsPackageCount = false,
                    KnowsUpdateDate = true,
                    MustBeInstalledAsAdmin = true,
                }
            };
        }

        protected override ManagerProperties GetProperties()
        {
            ManagerProperties properties = new()
            {
                Name = "Winget",
                Description = Tools.Translate("Microsoft's official package manager. Full of well-known and verified packages<br>Contains: <b>General Software, Microsoft Store apps</b>"),
                IconId = "winget",
                ColorIconId = "winget_color",
                ExecutableFriendlyName = "winget.exe",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "update",
                ExecutableCallArgs = "",
            };
            return properties;
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            ManagerStatus status = new();
            
            status.ExecutablePath = await Tools.Which("winget.exe");

            status.Found = File.Exists(status.ExecutablePath);

            if (!status.Found)
                return status;

            var process = new Process()
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
            status.Version = "WinGet CLI Version: " + (await process.StandardOutput.ReadToEndAsync()).Trim();


            // Initialize the WinGet manager (C# Native)
            WinGetFactory = (Tools.IsAdministrator()) 
                ? new WindowsPackageManagerElevatedFactory()
                : new WindowsPackageManagerStandardFactory();
            WinGetManager = await Task.Run(() => WinGetFactory.CreatePackageManager());

            Status = status; // Need to set status before calling RefreshSources, otherwise will crash
            if (status.Found && IsEnabled())
                await RefreshPackageIndexes();

            return status;
        }

        protected override async Task<string[]> GetPackageVersions_Unsafe(Package package)
        {
            // Find the native package for the given Package object
            var Catalog = WinGetManager.GetPackageCatalogByName(package.Source.Name);
            if(Catalog == null)
            {
                AppTools.Log("Failed to get catalog " + package.Source.Name + ". Is the package local?");
                return [];
            }

            // Connect to catalog
            Catalog.AcceptSourceAgreements = true;
            var ConnectResult = await Task.Run(() => Catalog.Connect());
            if (ConnectResult.Status != Deployment.ConnectResultStatus.Ok)
            {
                AppTools.Log("Failed to connect to catalog " + package.Source.Name);
                return [];
            }

            // Match only the exact same Id
            var packageMatchFilter = WinGetFactory.CreateFindPackagesOptions();
            var filters = WinGetFactory.CreatePackageMatchFilter();
            filters.Field = Deployment.PackageMatchField.Id;
            filters.Value = package.Id;
            filters.Option = Deployment.PackageFieldMatchOption.Equals;
            packageMatchFilter.Filters.Add(filters);
            packageMatchFilter.ResultLimit = 1;
            var SearchResult = Task.Run(() => ConnectResult.PackageCatalog.FindPackages(packageMatchFilter));

            if (SearchResult.Result == null || SearchResult.Result.Matches == null || SearchResult.Result.Matches.Count() == 0)
            {
                AppTools.Log("WinGet: Failed to find package " + package.Id + " in catalog " + package.Source.Name);
                return [];
            }

            // Get the Native Package
            var NativePackage = SearchResult.Result.Matches.First().CatalogPackage;
            return NativePackage.AvailableVersions.Select(x => x.Version).ToArray();
        }

        public override ManagerSource[] GetKnownSources()
        {
            return new ManagerSource[]
            {
                new(this, "winget", new Uri("https://cdn.winget.microsoft.com/cache")),
                new(this, "msstore", new Uri("https://storeedgefd.dsx.mp.microsoft.com/v9.0")),
            };
        }

        public override string[] GetAddSourceParameters(ManagerSource source)
        {
            return new string[] { "source", "add", "--name", source.Name, "--arg", source.Url.ToString(), "--accept-source-agreements", "--disable-interactivity" };
        }

        public override string[] GetRemoveSourceParameters(ManagerSource source)
        {
            return new string[] { "source", "remove", "--name", source.Name, "--disable-interactivity" };
        }

        public override OperationVeredict GetAddSourceOperationVeredict(ManagerSource source, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override OperationVeredict GetRemoveSourceOperationVeredict(ManagerSource source, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }
    }

    public class LocalPcSource : ManagerSource
    {
        public override string IconId { get { return "localpc"; } }
        public LocalPcSource() : base(Winget.Tools.App.Winget, Winget.Tools.Translate("Local PC"), new Uri("https://microsoft.com/local-pc-source"))
        { IsVirtualManager = true; }
        public override string ToString()
        {
            return Winget.Tools.Translate("Local PC");
        }
    }

    public class AndroidSubsystemSource : ManagerSource
    {
        public override string IconId { get { return "android"; } }
        public AndroidSubsystemSource() : base(Winget.Tools.App.Winget, Winget.Tools.Translate("Android Subsystem"), new Uri("https://microsoft.com/local-pc-source"))
        { IsVirtualManager = true; }
        public override string ToString()
        {
            return Winget.Tools.Translate("Android Subsystem");
        }
    }

    public class SteamSource : ManagerSource
    {
        public override string IconId { get { return "steam"; } }
        public SteamSource() : base(Winget.Tools.App.Winget, "Steam", new Uri("https://microsoft.com/local-pc-source"))
        { IsVirtualManager = true; }
        public override string ToString()
        {
            return "Steam";
        }
    }

    public class UbisoftConnectSource : ManagerSource
    {
        public override string IconId { get { return "uplay"; } }
        public UbisoftConnectSource() : base(Winget.Tools.App.Winget, "Ubisoft Connect", new Uri("https://microsoft.com/local-pc-source"))
        { IsVirtualManager = true; }
        public override string ToString()
        {
            return "Ubisoft Connect";
        }
    }

    public class GOGSource : ManagerSource
    {
        public override string IconId { get { return "gog"; } }
        public GOGSource() : base(Winget.Tools.App.Winget, "GOG", new Uri("https://microsoft.com/gog-source"))
        { IsVirtualManager = true; }
        public override string ToString()
        {
            return "GOG";
        }
    }

    public class MicrosoftStoreSource : ManagerSource
    {
        public override string IconId { get { return "msstore"; } }
        public MicrosoftStoreSource() : base(Winget.Tools.App.Winget, "Microsoft Store", new Uri("https://microsoft.com/microsoft-store-source"))
        { IsVirtualManager = true; }
        public override string ToString()
        {
            return "Microsoft Store";
        }
    }
}
