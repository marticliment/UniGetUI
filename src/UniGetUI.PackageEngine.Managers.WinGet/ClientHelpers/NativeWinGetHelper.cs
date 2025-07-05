using System.Diagnostics;
using Microsoft.Management.Deployment;
using UniGetUI.Core.Classes;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUI.PackageEngine.Structs;
using WindowsPackageManager.Interop;

namespace UniGetUI.PackageEngine.Managers.WingetManager;

internal sealed class NativeWinGetHelper : IWinGetManagerHelper
{
    public WindowsPackageManagerFactory Factory;
    public static WindowsPackageManagerFactory? ExternalFactory;
    public PackageManager WinGetManager;
    public static PackageManager? ExternalWinGetManager;
    private readonly WinGet Manager;

    public NativeWinGetHelper(WinGet manager)
    {
        Manager = manager;
        if (CoreTools.IsAdministrator())
        {
            Logger.Info("Running elevated, WinGet class registration is likely to fail unless using lower trust class registration is allowed in settings");
        }

        try
        {
            Factory = new WindowsPackageManagerStandardFactory();
            WinGetManager = Factory.CreatePackageManager();
            ExternalFactory = Factory;
            ExternalWinGetManager = WinGetManager;
        }
        catch
        {
            Logger.Warn("Couldn't connect to WinGet API, attempting to connect with lower trust... (Are you running as administrator?)");
            Factory = new WindowsPackageManagerStandardFactory(allowLowerTrustRegistration: true);
            WinGetManager = Factory.CreatePackageManager();
            ExternalFactory = Factory;
            ExternalWinGetManager = WinGetManager;
        }
    }

    public IReadOnlyList<Package> FindPackages_UnSafe(string query)
    {
        List<Package> packages = [];
        INativeTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.FindPackages);
        Dictionary<(PackageCatalogReference, PackageMatchField), Task<FindPackagesResult>> FindPackageTasks = [];

        // Load catalogs
        logger.Log("Loading available catalogs...");
        IReadOnlyList<PackageCatalogReference> AvailableCatalogs = WinGetManager.GetPackageCatalogs();

        // Spawn Tasks to find packages on catalogs
        logger.Log("Spawning catalog fetching tasks...");
        foreach (PackageCatalogReference CatalogReference in AvailableCatalogs.ToArray())
        {
            logger.Log($"Begin search on catalog {CatalogReference.Info.Name}");
            // Connect to catalog
            CatalogReference.AcceptSourceAgreements = true;
            ConnectResult result = CatalogReference.Connect();
            if (result.Status == ConnectResultStatus.Ok)
            {
                foreach (var filter_type in new[] { PackageMatchField.Name, PackageMatchField.Id, PackageMatchField.Moniker })
                {
                    FindPackagesOptions PackageFilters = Factory.CreateFindPackagesOptions();

                    logger.Log("Generating filters...");
                    // Name filter
                    PackageMatchFilter FilterName = Factory.CreatePackageMatchFilter();
                    FilterName.Field = filter_type;
                    FilterName.Value = query;
                    FilterName.Option = PackageFieldMatchOption.ContainsCaseInsensitive;
                    PackageFilters.Filters.Add(FilterName);

                    try
                    {
                        // Create task and spawn it
                        Task<FindPackagesResult> task = new(() => result.PackageCatalog.FindPackages(PackageFilters));
                        task.Start();

                        // Add task to list
                        FindPackageTasks.Add(
                            (CatalogReference, filter_type),
                            task
                        );
                    }
                    catch (Exception e)
                    {
                        logger.Error("WinGet: Catalog " + CatalogReference.Info.Name +
                                        " failed to spawn FindPackages task.");
                        logger.Error(e);
                    }
                }
            }
            else
            {
                logger.Error("WinGet: Catalog " + CatalogReference.Info.Name + " failed to connect.");
            }
        }

        // Wait for tasks completion
        Task.WhenAll(FindPackageTasks.Values.ToArray()).GetAwaiter().GetResult();
        logger.Log($"All catalogs fetched. Fetching results for query piece {query}");

        foreach (var CatalogTaskPair in FindPackageTasks)
        {
            try
            {
                // Get the source for the catalog
                IManagerSource source = Manager.SourcesHelper.Factory.GetSourceOrDefault(CatalogTaskPair.Key.Item1.Info.Name);

                FindPackagesResult FoundPackages = CatalogTaskPair.Value.Result;
                foreach (MatchResult matchResult in FoundPackages.Matches.ToArray())
                {
                    CatalogPackage nativePackage = matchResult.CatalogPackage;
                    // Create the Package item and add it to the list
                    logger.Log(
                        $"Found package: {nativePackage.Name}|{nativePackage.Id}|{nativePackage.DefaultInstallVersion.Version} on catalog {source.Name}");

                    var overriden_options = new OverridenInstallationOptions();

                    var UniGetUIPackage = new Package(
                        nativePackage.Name,
                        nativePackage.Id,
                        nativePackage.DefaultInstallVersion.Version,
                        source,
                        Manager,
                        overriden_options);
                    NativePackageHandler.AddPackage(UniGetUIPackage, nativePackage);
                    packages.Add(UniGetUIPackage);
                }
            }
            catch (Exception e)
            {
                logger.Error("WinGet: Catalog " + CatalogTaskPair.Key.Item1.Info.Name +
                                " failed to get available packages.");
                logger.Error(e);
            }
        }

        logger.Close(0);
        return packages;
    }

    public IReadOnlyList<Package> GetAvailableUpdates_UnSafe()
    {
        var logger = Manager.TaskLogger.CreateNew(LoggableTaskType.ListUpdates);
        List<Package> packages = [];
        foreach (var nativePackage in TaskRecycler<IReadOnlyList<CatalogPackage>>.RunOrAttach(GetLocalWinGetPackages))
        {
            if (nativePackage.IsUpdateAvailable)
            {
                IManagerSource source;
                source = Manager.SourcesHelper.Factory.GetSourceOrDefault(nativePackage.DefaultInstallVersion
                    .PackageCatalog.Info.Name);

                var UniGetUIPackage = new Package(
                    nativePackage.Name,
                    nativePackage.Id,
                    nativePackage.InstalledVersion.Version,
                    nativePackage.DefaultInstallVersion.Version,
                    source,
                    Manager);

                if (!WinGetPkgOperationHelper.UpdateAlreadyInstalled(UniGetUIPackage))
                {
                    NativePackageHandler.AddPackage(UniGetUIPackage, nativePackage);
                    packages.Add(UniGetUIPackage);
                    logger.Log(
                        $"Found package {nativePackage.Name} {nativePackage.Id} on source {source.Name}, from version {nativePackage.InstalledVersion.Version} to version {nativePackage.DefaultInstallVersion.Version}");
                }
                else
                {
                    Logger.Warn($"WinGet package {nativePackage.Id} not being shown as an updated as this version has already been marked as installed");
                }
            }
        }

        logger.Close(0);
        return packages;

    }

    public IReadOnlyList<Package> GetInstalledPackages_UnSafe()
    {
        var logger = Manager.TaskLogger.CreateNew(LoggableTaskType.ListInstalledPackages);
        List<Package> packages = [];
        foreach (var nativePackage in TaskRecycler<IReadOnlyList<CatalogPackage>>.RunOrAttach(GetLocalWinGetPackages, 15))
        {
            IManagerSource source;
            var availableVersions = nativePackage.AvailableVersions?.ToArray() ?? [];
            if (availableVersions.Length > 0)
            {
                var installPackage = nativePackage.GetPackageVersionInfo(availableVersions[0]);
                source = Manager.SourcesHelper.Factory.GetSourceOrDefault(installPackage.PackageCatalog.Info.Name);
            }
            else
            {
                source = Manager.GetLocalSource(nativePackage.Id);
            }
            logger.Log($"Found package {nativePackage.Name} {nativePackage.Id} on source {source.Name}");
            var UniGetUIPackage = new Package(
                nativePackage.Name,
                nativePackage.Id,
                nativePackage.InstalledVersion.Version,
                source,
                Manager);
            NativePackageHandler.AddPackage(UniGetUIPackage, nativePackage);
            packages.Add(UniGetUIPackage);
        }
        logger.Close(0);
        return packages;
    }

    private IReadOnlyList<CatalogPackage> GetLocalWinGetPackages()
    {
        var logger = Manager.TaskLogger.CreateNew(LoggableTaskType.OtherTask);
        logger.Log("OtherTask: GetWinGetLocalPackages");
        PackageCatalogReference installedSearchCatalogRef;
        CreateCompositePackageCatalogOptions createCompositePackageCatalogOptions = Factory.CreateCreateCompositePackageCatalogOptions();
        foreach (var catalogRef in WinGetManager.GetPackageCatalogs().ToArray())
        {
            logger.Log($"Adding catalog {catalogRef.Info.Name} to composite catalog");
            createCompositePackageCatalogOptions.Catalogs.Add(catalogRef);
        }
        createCompositePackageCatalogOptions.CompositeSearchBehavior = CompositeSearchBehavior.LocalCatalogs;
        installedSearchCatalogRef = WinGetManager.CreateCompositePackageCatalog(createCompositePackageCatalogOptions);

        var ConnectResult = installedSearchCatalogRef.Connect();
        if (ConnectResult.Status != ConnectResultStatus.Ok)
        {
            logger.Error("Failed to connect to installedSearchCatalogRef. Aborting.");
            logger.Close(1);
            throw new InvalidOperationException("WinGet: Failed to connect to composite catalog.");
        }

        FindPackagesOptions findPackagesOptions = Factory.CreateFindPackagesOptions();
        PackageMatchFilter filter = Factory.CreatePackageMatchFilter();
        filter.Field = PackageMatchField.Id;
        filter.Option = PackageFieldMatchOption.StartsWithCaseInsensitive;
        filter.Value = "";
        findPackagesOptions.Filters.Add(filter);

        var TaskResult = ConnectResult.PackageCatalog.FindPackages(findPackagesOptions);
        List<CatalogPackage> foundPackages = [];
        foreach (var match in TaskResult.Matches.ToArray())
        {
            foundPackages.Add(match.CatalogPackage);
        }

        logger.Close(0);
        return foundPackages;
    }

    public IReadOnlyList<IManagerSource> GetSources_UnSafe()
    {
        List<ManagerSource> sources = [];
        INativeTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.ListSources);

        foreach (PackageCatalogReference catalog in WinGetManager.GetPackageCatalogs().ToList())
        {
            try
            {
                logger.Log($"Found source {catalog.Info.Name} with argument {catalog.Info.Argument}");
                sources.Add(new ManagerSource(
                    Manager,
                    catalog.Info.Name,
                    new Uri(catalog.Info.Argument),
                    updateDate: (catalog.Info.LastUpdateTime.Second != 0 ? catalog.Info.LastUpdateTime : DateTime.Now).ToString()));
            }
            catch (Exception e)
            {
                logger.Error(e);
            }
        }

        logger.Close(0);
        return sources;
    }

    public IReadOnlyList<string> GetInstallableVersions_Unsafe(IPackage package)
    {
        INativeTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.LoadPackageVersions);

        var nativePackage = NativePackageHandler.GetPackage(package);
        if (nativePackage is null) return [];

        string[] versions = nativePackage.AvailableVersions.Select(x => x.Version).ToArray();
        foreach (string? version in versions)
        {
            logger.Log(version);
        }

        logger.Close(0);
        return versions ?? [];
    }

    public void GetPackageDetails_UnSafe(IPackageDetails details)
    {
        INativeTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.LoadPackageDetails);

        if (details.Package.Source.Name == "winget")
        {
            details.ManifestUrl = new Uri("https://github.com/microsoft/winget-pkgs/tree/master/manifests/"
                                          + details.Package.Id[0].ToString().ToLower() + "/"
                                          + details.Package.Id.Split('.')[0] + "/"
                                          + string.Join("/",
                                              details.Package.Id.Contains('.')
                                                  ? details.Package.Id.Split('.')[1..]
                                                  : details.Package.Id.Split('.'))
            );
        }
        else if (details.Package.Source.Name == "msstore")
        {
            details.ManifestUrl = new Uri("https://apps.microsoft.com/detail/" + details.Package.Id);
        }

        CatalogPackageMetadata? NativeDetails = NativePackageHandler.GetDetails(details.Package);
        if (NativeDetails is null) return;

        // Extract data from NativeDetails
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

        if (NativeDetails.Tags is not null)
            details.Tags = NativeDetails.Tags.ToArray();

        // There is no way yet to retrieve installer URLs right now so this part will be console-parsed.
        // TODO: Replace this code with native code when available on the COM api
        Process process = new();
        List<string> output = [];
        ProcessStartInfo startInfo = new()
        {
            FileName = Manager.BundledWinGetPath,
            Arguments = Manager.Status.ExecutableCallArgs + " show " + WinGetPkgOperationHelper.GetIdNamePiece(details.Package) +
                        " --disable-interactivity --accept-source-agreements --source " +
                        details.Package.Source.Name,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8
        };
        process.StartInfo = startInfo;
        if (CoreTools.IsAdministrator())
        {
            string WinGetTemp = Path.Join(Path.GetTempPath(), "UniGetUI", "ElevatedWinGetTemp");
            Logger.Warn($"[WARN] Redirecting %TEMP% folder to {WinGetTemp}, since UniGetUI was run as admin");
            process.StartInfo.Environment["TEMP"] = WinGetTemp;
            process.StartInfo.Environment["TMP"] = WinGetTemp;
        }
        process.Start();

        logger.Log("Begin loading installers:");
        logger.Log(" Executable: " + startInfo.FileName);
        logger.Log(" Arguments: " + startInfo.Arguments);

        // Retrieve the output
        string? _line;
        while ((_line = process.StandardOutput.ReadLine()) is not null)
        {
            if (_line.Trim() != "")
            {
                logger.Log(_line);
                output.Add(_line);
            }
        }

        logger.Error(process.StandardError.ReadToEnd());

        // Parse the output
        foreach (string __line in output)
        {
            try
            {
                string line = __line.Trim();
                if (line.Contains("Installer SHA256:"))
                {
                    details.InstallerHash = line.Split(":")[1].Trim();
                }
                else if (line.Contains("Installer Url:"))
                {
                    details.InstallerUrl = new Uri(line.Replace("Installer Url:", "").Trim());
                    details.InstallerSize = CoreTools.GetFileSize(details.InstallerUrl);
                }
                else if (line.Contains("Release Date:"))
                {
                    details.UpdateDate = line.Split(":")[1].Trim();
                }
                else if (line.Contains("Installer Type:"))
                {
                    details.InstallerType = line.Split(":")[1].Trim();
                }
            }
            catch (Exception e)
            {
                Logger.Warn("Error occurred while parsing line value=\"" + __line + "\"");
                Logger.Warn(e.Message);
            }
        }

        logger.Close(0);
    }
}
