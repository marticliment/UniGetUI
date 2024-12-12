using System.Collections.Concurrent;
using Microsoft.Management.Deployment;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Managers.WingetManager;

public static class NativePackageHandler
{
    private static readonly ConcurrentDictionary<long, CatalogPackage> __nativePackages = new();
    private static readonly ConcurrentDictionary<long, CatalogPackageMetadata> __nativeDetails = new();
    private static readonly ConcurrentDictionary<long, PackageInstallerInfo> __nativeInstallers_Install = new();
    private static ConcurrentDictionary<long, PackageInstallerInfo> __nativeInstallers_Uninstall = new();

    /// <summary>
    /// Get (cache or load) the native package for the given package, if any;
    /// </summary>
    /// <returns></returns>
    public static CatalogPackage? GetPackage(IPackage package)
    {
        if (NativeWinGetHelper.ExternalFactory is null || NativeWinGetHelper.ExternalWinGetManager is null)
            return null;

        __nativePackages.TryGetValue(package.GetHash(), out CatalogPackage? catalogPackage);
        if (catalogPackage is not null)
            return catalogPackage;

        // Rarely a package will not be available in cache (native WinGet helper should always call AddPackage)
        // so it makes no sense to add TaskRecycler.RunOrAttach() here. (Only imported packages may arrive at this point)
        catalogPackage = _findPackageOnCatalog(package);
        if(catalogPackage is not null) AddPackage(package, catalogPackage);

        return catalogPackage;
    }

    /// <summary>
    /// Adds an external CatalogPackage to the internal database
    /// </summary>
    public static void AddPackage(IPackage package, CatalogPackage catalogPackage)
    {
        __nativePackages[package.GetHash()] = catalogPackage;
    }

    /// <summary>
    /// Get (cached or load) the native package details for the given package, if any;
    /// </summary>
    public static CatalogPackageMetadata? GetDetails(IPackage package)
    //    => TaskRecycler<CatalogPackageMetadata?>.RunOrAttach(_getDetails, package);
    //
    //private static CatalogPackageMetadata? _getDetails(IPackage package)
    {
        if (__nativeDetails.TryGetValue(package.GetHash(), out CatalogPackageMetadata? metadata))
            return metadata;

        CatalogPackage? catalogPackage = GetPackage(package);
        metadata = catalogPackage?.DefaultInstallVersion?.GetCatalogPackageMetadata();
        metadata ??= catalogPackage?.InstalledVersion?.GetCatalogPackageMetadata();

        if (metadata is not null)
            __nativeDetails[package.GetHash()] = metadata;

        return metadata;
    }

    /// <summary>
    /// Get (cached or load) the native installer for the given package, if any. The operation type determines wether
    /// </summary>
    public static PackageInstallerInfo? GetInstallationOptions(IPackage package, OperationType operation)
    //    =>  TaskRecycler<PackageInstallerInfo?>.RunOrAttach(_getInstallationOptions, package, operation);
    //
    //private static PackageInstallerInfo? _getInstallationOptions(IPackage package, OperationType operation)
    {
        if (NativeWinGetHelper.ExternalFactory is null)
            return null;

        PackageInstallerInfo? installerInfo;
        if (operation is OperationType.Uninstall)
            installerInfo = _getInstallationOptionsOnDict(package, ref __nativeInstallers_Uninstall, true);
        else
            installerInfo = _getInstallationOptionsOnDict(package, ref __nativeInstallers_Uninstall, false);

        return installerInfo;
    }

    public static void Clear()
    {
        __nativePackages.Clear();;
        __nativeDetails.Clear();;
        __nativeInstallers_Install.Clear();;
        __nativeInstallers_Uninstall.Clear();
    }

    private static PackageInstallerInfo? _getInstallationOptionsOnDict(IPackage package, ref ConcurrentDictionary<long, PackageInstallerInfo> source, bool installed)
    {
        if (source.TryGetValue(package.GetHash(), out PackageInstallerInfo? installerInfo))
            return installerInfo;

        PackageVersionInfo? catalogPackage;
        if (installed) catalogPackage = GetPackage(package)?.InstalledVersion;
        else catalogPackage = GetPackage(package)?.DefaultInstallVersion;

        InstallOptions? options = NativeWinGetHelper.ExternalFactory?.CreateInstallOptions();
        installerInfo = catalogPackage?.GetApplicableInstaller(options);

        if (installerInfo is not null)
            source[package.GetHash()] = installerInfo;

        return installerInfo;
    }

    private static CatalogPackage? _findPackageOnCatalog(IPackage package)
    {
        if (NativeWinGetHelper.ExternalWinGetManager is null || NativeWinGetHelper.ExternalFactory is null)
            return null;

        PackageCatalogReference Catalog = NativeWinGetHelper.ExternalWinGetManager.GetPackageCatalogByName(package.Source.Name);
        if (Catalog is null)
        {
            Logger.Error("Failed to get catalog " + package.Source.Name + ". Is the package local?");
            return null;
        }

        // Connect to catalog
        Catalog.AcceptSourceAgreements = true;
        ConnectResult ConnectResult = Catalog.Connect();
        if (ConnectResult.Status != ConnectResultStatus.Ok)
        {
            Logger.Error("Failed to connect to catalog " + package.Source.Name);
            return null;
        }

        // Match only the exact same Id
        FindPackagesOptions packageMatchFilter = NativeWinGetHelper.ExternalFactory.CreateFindPackagesOptions();
        PackageMatchFilter filters = NativeWinGetHelper.ExternalFactory.CreatePackageMatchFilter();
        filters.Field = PackageMatchField.Id;
        filters.Value = package.Id;
        filters.Option = PackageFieldMatchOption.Equals;
        packageMatchFilter.Filters.Add(filters);
        packageMatchFilter.ResultLimit = 1;
        var SearchResult = Task.Run(() => ConnectResult.PackageCatalog.FindPackages(packageMatchFilter));

        if (SearchResult?.Result?.Matches is null ||
            SearchResult.Result.Matches.Count == 0)
        {
            Logger.Error("Failed to find package " + package.Id + " in catalog " + package.Source.Name);
            return null;
        }

        // Return the Native Package
        return SearchResult.Result.Matches.First().CatalogPackage;
    }
}
