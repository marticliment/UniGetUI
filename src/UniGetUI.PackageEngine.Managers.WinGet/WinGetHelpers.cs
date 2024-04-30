using Microsoft.Management.Deployment;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UniGetUI.Core;
using UniGetUI.Core.Data;
using UniGetUI.PackageEngine.Classes;
using WindowsPackageManager.Interop;
using UniGetUI.Core.Logging;
using Deployment = Microsoft.Management.Deployment;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;

namespace UniGetUI.PackageEngine.Managers.WingetManager
{
    internal static class WinGetHelper
    {
        private static IWinGetPackageHelper? __helper;
        public static IWinGetPackageHelper Instance
        {
            get
            {
                if (__helper == null)
                {
                    __helper = new BundledWinGetHelper();
                }
                return __helper;
            }

            set
            {
                __helper = value;
            }
        }
    }

    internal interface IWinGetPackageHelper
    {

        public Task<Package[]> FindPackages_UnSafe(WinGet ManagerInstance, string query);
        public Task<ManagerSource[]> GetSources_UnSafe(WinGet ManagerInstance);
        public Task<string[]> GetPackageVersions_Unsafe(WinGet ManagerInstance, Package package);
        public Task<PackageDetails> GetPackageDetails_UnSafe(WinGet ManagerInstance, Package package);

    }

    internal class NativeWinGetHelper : IWinGetPackageHelper
    {
        public WindowsPackageManagerStandardFactory Factory;
        public Deployment.PackageManager WinGetManager;

        public NativeWinGetHelper()
        {
            if (CoreTools.IsAdministrator())
                Logger.Info("Running elevated, WinGet class registration is likely to fail");
            Factory = new WindowsPackageManagerStandardFactory();
            WinGetManager = Factory.CreatePackageManager();
        }


        public async Task<Package[]> FindPackages_UnSafe(WinGet ManagerInstance, string query)
        {
            List<Package> Packages = new();
            FindPackagesOptions PackageFilters = Factory.CreateFindPackagesOptions();

            // Name filter
            PackageMatchFilter FilterName = Factory.CreatePackageMatchFilter();
            FilterName.Field = Deployment.PackageMatchField.Name;
            FilterName.Value = query;
            FilterName.Option = Deployment.PackageFieldMatchOption.ContainsCaseInsensitive;
            PackageFilters.Filters.Add(FilterName);

            // Id filter
            PackageMatchFilter FilterId = Factory.CreatePackageMatchFilter();
            FilterId.Field = Deployment.PackageMatchField.Name;
            FilterId.Value = query;
            FilterId.Option = Deployment.PackageFieldMatchOption.ContainsCaseInsensitive;
            PackageFilters.Filters.Add(FilterId);

            // Load catalogs
            IReadOnlyList<PackageCatalogReference> AvailableCatalogs = WinGetManager.GetPackageCatalogs();
            Dictionary<Deployment.PackageCatalogReference, Task<Deployment.FindPackagesResult>> FindPackageTasks = new();

            // Spawn Tasks to find packages on catalogs
            foreach (PackageCatalogReference CatalogReference in AvailableCatalogs.ToArray())
            {
                // Connect to catalog
                CatalogReference.AcceptSourceAgreements = true;
                ConnectResult result = await CatalogReference.ConnectAsync();
                if (result.Status == Deployment.ConnectResultStatus.Ok)
                {
                    try
                    {
                        // Create task and spawn it
                        Task<FindPackagesResult> task = new(() => result.PackageCatalog.FindPackages(PackageFilters));
                        task.Start();

                        // Add task to list
                        FindPackageTasks.Add(
                            CatalogReference,
                            task
                        );
                    }
                    catch (Exception e)
                    {
                        Logger.Error("WinGet: Catalog " + CatalogReference.Info.Name + " failed to spawn FindPackages task.");
                        Logger.Error(e);
                    }
                }
                else
                {
                    Logger.Error("WinGet: Catalog " + CatalogReference.Info.Name + " failed to connect.");
                }
            }

            // Wait for tasks completion
            await Task.WhenAll(FindPackageTasks.Values.ToArray());

            foreach (KeyValuePair<PackageCatalogReference, Task<FindPackagesResult>> CatalogTaskPair in FindPackageTasks)
            {
                try
                {
                    // Get the source for the catalog
                    ManagerSource source = ManagerInstance.GetSourceOrDefault(CatalogTaskPair.Key.Info.Name);

                    FindPackagesResult FoundPackages = CatalogTaskPair.Value.Result;
                    foreach (MatchResult package in FoundPackages.Matches.ToArray())
                    {
                        // Create the Package item and add it to the list
                        Packages.Add(new Package(
                            package.CatalogPackage.Name,
                            package.CatalogPackage.Id,
                            package.CatalogPackage.DefaultInstallVersion.Version,
                            source,
                            ManagerInstance
                        ));
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("WinGet: Catalog " + CatalogTaskPair.Key.Info.Name + " failed to get available packages.");
                    Logger.Error(e);
                }
            }

            return Packages.ToArray();
        }

        public async Task<ManagerSource[]> GetSources_UnSafe(WinGet ManagerInstance)
        {
            List<ManagerSource> sources = new();

            foreach (PackageCatalogReference catalog in await Task.Run(() => WinGetManager.GetPackageCatalogs().ToArray()))
                try
                {
                    sources.Add(new ManagerSource(ManagerInstance, catalog.Info.Name, new Uri(catalog.Info.Argument), updateDate: catalog.Info.LastUpdateTime.ToString()));
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }

            return sources.ToArray();
        }

        public async Task<string[]> GetPackageVersions_Unsafe(WinGet ManagerInstance, Package package)
        {
            // Find the native package for the given Package object
            PackageCatalogReference Catalog = WinGetManager.GetPackageCatalogByName(package.Source.Name);
            if (Catalog == null)
            {
                Logger.Error("Failed to get catalog " + package.Source.Name + ". Is the package local?");
                return [];
            }

            // Connect to catalog
            Catalog.AcceptSourceAgreements = true;
            ConnectResult ConnectResult = await Task.Run(() => Catalog.Connect());
            if (ConnectResult.Status != Deployment.ConnectResultStatus.Ok)
            {
                Logger.Error("Failed to connect to catalog " + package.Source.Name);
                return [];
            }

            // Match only the exact same Id
            FindPackagesOptions packageMatchFilter = Factory.CreateFindPackagesOptions();
            PackageMatchFilter filters = Factory.CreatePackageMatchFilter();
            filters.Field = Deployment.PackageMatchField.Id;
            filters.Value = package.Id;
            filters.Option = Deployment.PackageFieldMatchOption.Equals;
            packageMatchFilter.Filters.Add(filters);
            packageMatchFilter.ResultLimit = 1;
            Task<FindPackagesResult> SearchResult = Task.Run(() => ConnectResult.PackageCatalog.FindPackages(packageMatchFilter));

            if (SearchResult.Result == null || SearchResult.Result.Matches == null || SearchResult.Result.Matches.Count() == 0)
            {
                Logger.Error("WinGet: Failed to find package " + package.Id + " in catalog " + package.Source.Name);
                return [];
            }

            // Get the Native Package
            CatalogPackage NativePackage = SearchResult.Result.Matches.First().CatalogPackage;
            return NativePackage.AvailableVersions.Select(x => x.Version).ToArray();
        }

        public async Task<PackageDetails> GetPackageDetails_UnSafe(WinGet ManagerInstance, Package package)
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
            PackageCatalogReference Catalog = WinGetManager.GetPackageCatalogByName(package.Source.Name);
            if (Catalog == null)
            {
                Logger.Error("Failed to get catalog " + package.Source.Name + ". Is the package local?");
                return details;
            }

            // Connect to catalog
            Catalog.AcceptSourceAgreements = true;
            ConnectResult ConnectResult = await Task.Run(() => Catalog.Connect());
            if (ConnectResult.Status != Deployment.ConnectResultStatus.Ok)
            {
                Logger.Error("Failed to connect to catalog " + package.Source.Name);
                return details;
            }

            // Match only the exact same Id
            FindPackagesOptions packageMatchFilter = Factory.CreateFindPackagesOptions();
            PackageMatchFilter filters = Factory.CreatePackageMatchFilter();
            filters.Field = Deployment.PackageMatchField.Id;
            filters.Value = package.Id;
            filters.Option = Deployment.PackageFieldMatchOption.Equals;
            packageMatchFilter.Filters.Add(filters);
            packageMatchFilter.ResultLimit = 1;
            Task<FindPackagesResult> SearchResult = Task.Run(() => ConnectResult.PackageCatalog.FindPackages(packageMatchFilter));

            if (SearchResult.Result == null || SearchResult.Result.Matches == null || SearchResult.Result.Matches.Count() == 0)
            {
                Logger.Error("WinGet: Failed to find package " + package.Id + " in catalog " + package.Source.Name);
                return details;
            }

            // Get the Native Package
            CatalogPackage NativePackage = SearchResult.Result.Matches.First().CatalogPackage;

            // Extract data from NativeDetails
            CatalogPackageMetadata NativeDetails = NativePackage.DefaultInstallVersion.GetCatalogPackageMetadata(Windows.System.UserProfile.GlobalizationPreferences.Languages[0]);

            if (NativeDetails.Author != "")
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


            // There is no way yet to retrieve installer URLs right now so this part will be console-parsed.
            // TODO: Replace this code with native code when available on the COM api
            Process process = new();
            List<string> output = new();
            ProcessStartInfo startInfo = new()
            {
                FileName = Path.Join(CoreData.UniGetUIExecutableDirectory, "winget-cli_x64", "winget.exe"),
                Arguments = ManagerInstance.Properties.ExecutableCallArgs + " show --id " + package.Id + " --exact --disable-interactivity --accept-source-agreements --source " + package.Source.Name,
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
                    string line = __line.Trim();
                    if (line.Contains("Installer SHA256:"))
                        details.InstallerHash = line.Split(":")[1].Trim();

                    else if (line.Contains("Installer Url:"))
                    {
                        details.InstallerUrl = new Uri(line.Replace("Installer Url:", "").Trim());
                        details.InstallerSize = await CoreTools.GetFileSizeAsync(details.InstallerUrl);
                    }
                    else if (line.Contains("Release Date:"))
                        details.UpdateDate = line.Split(":")[1].Trim();

                    else if (line.Contains("Installer Type:"))
                        details.InstallerType = line.Split(":")[1].Trim();
                }
                catch (Exception e)
                {
                    Logger.Warn("Error occurred while parsing line value=\"" + __line + "\"");
                    Logger.Warn(e.Message);
                }
            }

            return details;
        }
    }


    internal class BundledWinGetHelper : IWinGetPackageHelper
    {

        private string WinGetBundledPath;
        public BundledWinGetHelper()
        {
            WinGetBundledPath = Path.Join(CoreData.UniGetUIExecutableDirectory, "PackageEngine", "Managers", "winget-cli_x64", "winget.exe");
        }

        public async Task<Package[]> FindPackages_UnSafe(WinGet ManagerInstance, string query)
        {
            List<Package> Packages = new();

            Process p = new();
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


            p.Start();

            await p.StandardInput.WriteAsync(@"
                Set-ExecutionPolicy Bypass -Scope Process -Force
                function Print-WinGetPackage {
                    param (
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $Name,
                        [Parameter(Mandatory,ValueFromPipelineByPropertyName)] [string] $Id,
                        [Parameter(ValueFromPipelineByPropertyName)] [string[]] $AvailableVersions,
                        [Parameter(ValueFromPipelineByPropertyName)] [string] $Source
                    )
                    process {
                        Write-Output(""#"" + $Name + ""`t"" + $Id + ""`t"" + $AvailableVersions[0] + ""`t"" + $Source)
                    }
                }

                if(!(Get-Command -Verb Find -Noun WinGetPackage))
                {
                    Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force -Confirm:$false
                    Install-Module -Name Microsoft.WinGet.Client -Scope CurrentUser -AllowClobber -Confirm:$false -Force
                }
                Find-WinGetPackage -Query """ + query + @""" | Print-WinGetPackage
                exit
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

                ManagerSource source = ManagerInstance.GetSourceOrDefault(elements[3]);

                Packages.Add(new Package(elements[0][1..], elements[1], elements[2], source, ManagerInstance));
            }

            output += await p.StandardError.ReadToEndAsync();
            // AppTools.LogManagerOperation(ManagerInstance, p, output);
            await p.WaitForExitAsync();

            return Packages.ToArray();

        }

        public async Task<PackageDetails> GetPackageDetails_UnSafe(WinGet ManagerInstance, Package package)
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

            // Get the output for the best matching locale
            Process process = new();
            string packageIdentifier;
            if (!package.Id.Contains("…"))
                packageIdentifier = "--id " + package.Id + " --exact";
            else if (!package.Name.Contains("…"))
                packageIdentifier = "--name " + package.Id + " --exact";
            else
                packageIdentifier = "--id " + package.Id;

            List<string> output = new();
            bool LocaleFound = true;
            ProcessStartInfo startInfo = new()
            {
                FileName = WinGetBundledPath,
                Arguments = ManagerInstance.Properties.ExecutableCallArgs + " show " + packageIdentifier + " --disable-interactivity --accept-source-agreements --locale " + System.Globalization.CultureInfo.CurrentCulture.ToString(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            process.StartInfo = startInfo;
            process.Start();

            string _line;
            while ((_line = await process.StandardOutput.ReadLineAsync()) != null)
                if (_line.Trim() != "")
                {
                    output.Add(_line);
                    if (_line.Contains("The value provided for the `locale` argument is invalid") || _line.Contains("No applicable installer found; see Logger.Logs for more details."))
                    {
                        LocaleFound = false;
                        break;
                    }
                }

            // Load fallback english locale
            if (!LocaleFound)
            {
                output.Clear();
                Logger.Info("Winget could not found culture data for package Id=" + package.Id + " and Culture=" + System.Globalization.CultureInfo.CurrentCulture.ToString() + ". Trying to get data for en-US");
                process = new Process();
                LocaleFound = true;
                startInfo = new()
                {
                    FileName = WinGetBundledPath,
                    Arguments = ManagerInstance.Properties.ExecutableCallArgs + " show " + packageIdentifier + " --disable-interactivity --accept-source-agreements --locale en-US",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };
                process.StartInfo = startInfo;
                process.Start();

                while ((_line = await process.StandardOutput.ReadLineAsync()) != null)
                    if (_line.Trim() != "")
                    {
                        output.Add(_line);
                        if (_line.Contains("The value provided for the `locale` argument is invalid") || _line.Contains("No applicable installer found; see Logger.Logs for more details."))
                        {
                            LocaleFound = false;
                            break;
                        }
                    }
            }

            // Load default locale
            if (!LocaleFound)
            {
                output.Clear();
                Logger.Info("Winget could not found culture data for package Id=" + package.Id + " and Culture=en-US. Loading default");
                LocaleFound = true;
                process = new Process();
                startInfo = new()
                {
                    FileName = WinGetBundledPath,
                    Arguments = ManagerInstance.Properties.ExecutableCallArgs + " show " + packageIdentifier + " --disable-interactivity --accept-source-agreements",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };
                process.StartInfo = startInfo;
                process.Start();

                while ((_line = await process.StandardOutput.ReadLineAsync()) != null)
                    if (_line.Trim() != "")
                    {
                        output.Add(_line);
                    }
            }

            // Parse the output
            bool IsLoadingDescription = false;
            bool IsLoadingReleaseNotes = false;
            bool IsLoadingTags = false;
            foreach (string __line in output)
            {
                try
                {
                    string line = __line.TrimEnd();
                    if (line == "")
                        continue;

                    // Check if a multiline field is being loaded
                    if (line.StartsWith(" ") && IsLoadingDescription)
                        details.Description += "\n" + line.Trim();
                    else if (line.StartsWith(" ") && IsLoadingReleaseNotes)
                        details.ReleaseNotes += "\n" + line.Trim();
                    else if (line.StartsWith(" ") && IsLoadingTags)
                        details.Tags = details.Tags.Append(line.Trim()).ToArray();

                    // Stop loading multiline fields
                    else if (IsLoadingDescription)
                        IsLoadingDescription = false;
                    else if (IsLoadingReleaseNotes)
                        IsLoadingReleaseNotes = false;
                    else if (IsLoadingTags)
                        IsLoadingTags = false;

                    // Check for singleline fields
                    if (line.Contains("Publisher:"))
                        details.Publisher = line.Split(":")[1].Trim();

                    else if (line.Contains("Author:"))
                        details.Author = line.Split(":")[1].Trim();

                    else if (line.Contains("Homepage:"))
                        details.HomepageUrl = new Uri(line.Replace("Homepage:", "").Trim());

                    else if (line.Contains("License:"))
                        details.License = line.Split(":")[1].Trim();

                    else if (line.Contains("License Url:"))
                        details.LicenseUrl = new Uri(line.Replace("License Url:", "").Trim());

                    else if (line.Contains("Installer SHA256:"))
                        details.InstallerHash = line.Split(":")[1].Trim();

                    else if (line.Contains("Installer Url:"))
                    {
                        details.InstallerUrl = new Uri(line.Replace("Installer Url:", "").Trim());
                        details.InstallerSize = await CoreTools.GetFileSizeAsync(details.InstallerUrl);
                    }
                    else if (line.Contains("Release Date:"))
                        details.UpdateDate = line.Split(":")[1].Trim();

                    else if (line.Contains("Release Notes Url:"))
                        details.ReleaseNotesUrl = new Uri(line.Replace("Release Notes Url:", "").Trim());

                    else if (line.Contains("Installer Type:"))
                        details.InstallerType = line.Split(":")[1].Trim();

                    else if (line.Contains("Description:"))
                    {
                        details.Description = line.Split(":")[1].Trim();
                        IsLoadingDescription = true;
                    }
                    else if (line.Contains("ReleaseNotes"))
                    {
                        details.ReleaseNotes = line.Split(":")[1].Trim();
                        IsLoadingReleaseNotes = true;
                    }
                    else if (line.Contains("Tags"))
                    {
                        details.Tags = new string[0];
                        IsLoadingTags = true;
                    }
                }
                catch (Exception e)
                {
                    Logger.Warn("Error occurred while parsing line value=\"" + _line + "\"");
                    Logger.Warn(e.Message);
                }
            }

            return details;
        }

        public async Task<string[]> GetPackageVersions_Unsafe(WinGet ManagerInstance, Package package)
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = WinGetBundledPath,
                    Arguments = ManagerInstance.Properties.ExecutableCallArgs + " show --id " + package.Id + " --exact --versions --accept-source-agreements",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
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
                    if (line.Contains("---"))
                        DashesPassed = true;
                }
                else
                    versions.Add(line.Trim());
            }
            output += await p.StandardError.ReadToEndAsync();
            // AppTools.LogManagerOperation(ManagerInstance, p, output);
            return versions.ToArray();
        }

        public async Task<ManagerSource[]> GetSources_UnSafe(WinGet ManagerInstance)
        {
            List<ManagerSource> sources = new();

            Process process = new();
            ProcessStartInfo startInfo = new()
            {
                FileName = ManagerInstance.Status.ExecutablePath,
                Arguments = ManagerInstance.Properties.ExecutableCallArgs + " source list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            process.StartInfo = startInfo;
            process.Start();

            bool dashesPassed = false;
            string output = "";
            string line;
            while ((line = await process.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                try
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    if (!dashesPassed)
                    {
                        if (line.Contains("---"))
                            dashesPassed = true;
                    }
                    else
                    {
                        string[] parts = Regex.Replace(line.Trim(), " {2,}", " ").Split(' ');
                        if (parts.Length > 1)
                            sources.Add(new ManagerSource(ManagerInstance, parts[0].Trim(), new Uri(parts[1].Trim())));
                    }
                }
                catch (Exception e)
                {
                    Logger.Warn(e);
                }
            }

            output += await process.StandardError.ReadToEndAsync();
            // AppTools.LogManagerOperation(ManagerInstance, process, output);

            await process.WaitForExitAsync();
            return sources.ToArray();

        }
    }
}


