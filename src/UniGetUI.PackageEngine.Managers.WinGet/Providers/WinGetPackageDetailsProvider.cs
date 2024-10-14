using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Management.Deployment;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;
using UniGetUIManagers = UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Managers.WingetManager
{
    internal sealed class WinGetPackageDetailsProvider : BasePackageDetailsProvider<UniGetUIManagers.PackageManager>
    {
        private static readonly Dictionary<string, string> __msstore_package_manifests = [];

        private struct MicrosoftStoreProductType
        {
            public string productIds { get; set; }
        }

        public WinGetPackageDetailsProvider(WinGet manager) : base(manager) { }

        protected override IEnumerable<string> GetInstallableVersions_UnSafe(IPackage package)
        {
            return WinGetHelper.Instance.GetInstallableVersions_Unsafe((WinGet)Manager, package);
        }

        protected override void GetDetails_UnSafe(IPackageDetails details)
        {
            WinGetHelper.Instance.GetPackageDetails_UnSafe((WinGet)Manager, details);
        }

        protected override CacheableIcon? GetIcon_UnSafe(IPackage package)
        {
            if (package.Source.IsVirtualManager)
                return null;

            else if (package.Source.Name == "msstore")
                return GetMicrosoftStoreIcon(package);

            else
                return GetWinGetPackageIcon(package);
        }

        protected override IEnumerable<Uri> GetScreenshots_UnSafe(IPackage package)
        {
            if (package.Source.Name != "msstore")
            {
                return [];
            }

            string? ResponseContent = GetMicrosoftStoreManifest(package);
            if (ResponseContent is null)
            {
                return [];
            }

            Match IconArray = Regex.Match(ResponseContent, "(?:\"|')Images(?:\"|'): ?\\[([^\\]]+)\\]");
            if (!IconArray.Success)
            {
                Logger.Warn("Could not parse Images array from Microsoft Store response");
                return [];
            }

            List<Uri> FoundIcons = [];

            foreach (Match ImageEntry in Regex.Matches(IconArray.Groups[1].Value, "{([^}]+)}"))
            {

                if (!ImageEntry.Success)
                {
                    continue;
                }

                Match ImagePurpose = Regex.Match(ImageEntry.Groups[1].Value, "(?:\"|')ImagePurpose(?:\"|'): ?(?:\"|')([^'\"]+)(?:\"|')");
                if (!ImagePurpose.Success || ImagePurpose.Groups[1].Value != "Screenshot")
                {
                    continue;
                }

                Match ImageUrl = Regex.Match(ImageEntry.Groups[1].Value, "(?:\"|')Uri(?:\"|'): ?(?:\"|')([^'\"]+)(?:\"|')");
                if (!ImageUrl.Success)
                {
                    continue;
                }

                FoundIcons.Add(new Uri("https:" + ImageUrl.Groups[1].Value));
            }

            return FoundIcons;
        }

        protected override string? GetInstallLocation_UnSafe(IPackage package)
        {
            foreach (var base_path in new[]
                 {
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                     Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"),
                     Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Packages"),
                     Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "WinGet", "Packages"),
                     Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "WinGet", "Packages"),
                     Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                 })
            {
                var path_with_name = Path.Join(base_path, package.Name);
                if (Directory.Exists(path_with_name)) return path_with_name;

                var path_with_id = Path.Join(base_path, package.Id);
                if (Directory.Exists(path_with_id)) return path_with_id;

                var path_with_source = Path.Join(base_path, $"{package.Id}_{package.Source.Name}");
                if (Directory.Exists(path_with_source)) return path_with_source;
            }

            return null;
        }

        private static string? GetMicrosoftStoreManifest(IPackage package)
        {
            if (__msstore_package_manifests.TryGetValue(package.Id, out var manifest))
            {
                return manifest;
            }

            string CountryCode = CultureInfo.CurrentCulture.Name.Split("-")[^1];
            string Locale = CultureInfo.CurrentCulture.Name;
            string url = $"https://storeedgefd.dsx.mp.microsoft.com/v8.0/sdk/products?market={CountryCode}&locale={Locale}&deviceFamily=Windows.Desktop";

#pragma warning disable SYSLIB0014
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
#pragma warning restore SYSLIB0014

            httpRequest.Method = "POST";
            httpRequest.ContentType = "application/json";

            string data = "{\"productIds\": \"" + package.Id.ToLower() + "\"}";

            using (StreamWriter streamWriter = new(httpRequest.GetRequestStream()))
            {
                streamWriter.Write(data);
            }

            HttpWebResponse? httpResponse = httpRequest.GetResponse() as HttpWebResponse;
            if (httpResponse is null)
            {
                Logger.Warn($"Null MS Store response for uri={url} and data={data}");
                return null;
            }

            string result;
            using (StreamReader streamReader = new(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
            }

            Logger.Debug("Microsoft Store API call status code: " + httpResponse.StatusCode);

            if (result != "" && httpResponse.StatusCode == HttpStatusCode.OK)
            {
                __msstore_package_manifests[package.Id] = result;
            }

            return result;
        }

        private static CacheableIcon? GetMicrosoftStoreIcon(IPackage package)
        {
            string? ResponseContent = GetMicrosoftStoreManifest(package);
            if (ResponseContent is null)
            {
                return null;
            }

            Match IconArray = Regex.Match(ResponseContent, "(?:\"|')Images(?:\"|'): ?\\[([^\\]]+)\\]");
            if (!IconArray.Success)
            {
                Logger.Warn("Could not parse Images array from Microsoft Store response");
                return null;
            }

            Dictionary<int, string> FoundIcons = [];

            foreach (Match ImageEntry in Regex.Matches(IconArray.Groups[1].Value, "{([^}]+)}"))
            {
                string CurrentImage = ImageEntry.Groups[1].Value;

                if (!ImageEntry.Success)
                {
                    continue;
                }

                Match ImagePurpose = Regex.Match(CurrentImage, "(?:\"|')ImagePurpose(?:\"|'): ?(?:\"|')([^'\"]+)(?:\"|')");
                if (!ImagePurpose.Success || ImagePurpose.Groups[1].Value != "Tile")
                {
                    continue;
                }

                Match ImageUrl = Regex.Match(CurrentImage, "(?:\"|')Uri(?:\"|'): ?(?:\"|')([^'\"]+)(?:\"|')");
                Match ImageSize = Regex.Match(CurrentImage, "(?:\"|')Height(?:\"|'): ?([^,]+)");

                if (!ImageUrl.Success || !ImageSize.Success)
                {
                    continue;
                }

                FoundIcons[int.Parse(ImageSize.Groups[1].Value)] = ImageUrl.Groups[1].Value;
            }

            if (FoundIcons.Count == 0)
            {
                Logger.Warn($"No Logo image found for package {package.Id} in Microsoft Store response");
                return null;
            }

            Logger.Debug("Choosing icon with size " + FoundIcons.Keys.Max() + " for package " + package.Id + " from Microsoft Store");

            string uri = "https:" + FoundIcons[FoundIcons.Keys.Max()];

            return new CacheableIcon(new Uri(uri));
        }

        private static CacheableIcon? GetWinGetPackageIcon(IPackage package)
        {
            if (WinGetHelper.Instance is not NativeWinGetHelper)
                return null;

            PackageManager WinGetManager = ((NativeWinGetHelper)WinGetHelper.Instance).WinGetManager;
            WindowsPackageManager.Interop.WindowsPackageManagerFactory Factory = ((NativeWinGetHelper)WinGetHelper.Instance).Factory;

            // Find the native package for the given Package object
            PackageCatalogReference Catalog = WinGetManager.GetPackageCatalogByName(package.Source.Name);
            if (Catalog is null)
            {
                Logger.Error("[WINGET COM] Failed to get catalog " + package.Source.Name + ". Is the package local?");
                return null;
            }

            // Connect to catalog
            Catalog.AcceptSourceAgreements = true;
            ConnectResult ConnectResult = Catalog.Connect();
            if (ConnectResult.Status != ConnectResultStatus.Ok)
            {
                Logger.Error("[WINGET COM] Failed to connect to catalog " + package.Source.Name);
                return null;
            }

            // Match only the exact same Id
            FindPackagesOptions packageMatchFilter = Factory.CreateFindPackagesOptions();
            PackageMatchFilter filters = Factory.CreatePackageMatchFilter();
            filters.Field = PackageMatchField.Id;
            filters.Value = package.Id;
            filters.Option = PackageFieldMatchOption.Equals;
            packageMatchFilter.Filters.Add(filters);
            packageMatchFilter.ResultLimit = 1;
            FindPackagesResult SearchResult = ConnectResult.PackageCatalog.FindPackages(packageMatchFilter);

            if (SearchResult.Matches is null || SearchResult.Matches.Count == 0)
            {
                Logger.Error($"[WINGET COM] Package with Id={package.Id} was NOT found in catalog id=" + package.Source.Name);
                return null;
            }

            // Get the Native Package
            CatalogPackage NativePackage = SearchResult.Matches.First().CatalogPackage;

            // Extract data from NativeDetails
            CatalogPackageMetadata NativeDetails = NativePackage.DefaultInstallVersion.GetCatalogPackageMetadata();

            // Get the actual icon and return it
            foreach (Icon? icon in NativeDetails.Icons.ToArray())
            {
                if (icon is not null && icon.Url is not null)
                {
                    Logger.Debug($"Found WinGet native icon for {package.Id} with URL={icon.Url}");
                    return new CacheableIcon(new Uri(icon.Url), icon.Sha256);
                }
            }

            Logger.Debug($"Native WinGet icon for Package={package.Id} on catalog={package.Source.Name} was not found :(");
            return null;
        }
    }
}
