using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Managers.Generic.NuGet.Internal
{
    internal static class NuGetManifestLoader
    {
        private static readonly Dictionary<string, string> __manifest_cache = [];

        /// <summary>
        /// Returns the URL to the manifest of a NuGet-based package
        /// </summary>
        /// <param name="package">A valid Package object</param>
        /// <returns>A Uri object</returns>
        public static Uri GetManifestUrl(IPackage package)
        {
            return new Uri($"{package.Source.Url}/Packages(Id='{package.Id}',Version='{package.Version}')");
        }

        /// <summary>
        /// Returns the URL to the NuPkg file
        /// </summary>
        /// <param name="package">A valid Package object</param>
        /// <returns>A Uri object</returns>
        public static Uri GetNuPkgUrl(IPackage package)
        {
            return new Uri($"{package.Source.Url}/package/{package.Id}/{package.Version}");
        }

        /// <summary>
        /// Returns the contents of the manifest of a NuGet-based package
        /// </summary>
        /// <param name="package">The package for which to obtain the manifest</param>
        /// <returns>A string containing the contents of the manifest</returns>
        public static string? GetManifestContent(IPackage package)
        {
            string? PackageManifestContent = "";
            string PackageManifestUrl = GetManifestUrl(package).ToString();
            if (__manifest_cache.TryGetValue(PackageManifestUrl, out var content))
            {
                Logger.Debug($"Loading cached NuGet manifest for package {package.Id} on manager {package.Manager.Name}");
                return content;
            }

            try
            {
                using (HttpClient client = new(CoreData.GenericHttpClientParameters))
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(CoreData.UserAgentString);
                    HttpResponseMessage response = client.GetAsync(PackageManifestUrl).GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode && package.Version.EndsWith(".0"))
                    {
                        response = client.GetAsync(new Uri(PackageManifestUrl.ToString().Replace(".0')", "')"))).GetAwaiter().GetResult();
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.Warn($"Failed to download the {package.Manager.Name} manifest at Url={PackageManifestUrl.ToString()} with status code {response.StatusCode}");
                        return null;
                    }

                    PackageManifestContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
                __manifest_cache[PackageManifestUrl] = PackageManifestContent;
                return PackageManifestContent;
            }
            catch (Exception e)
            {
                Logger.Warn($"Failed to download the {package.Manager.Name} manifest at Url={PackageManifestUrl.ToString()}");
                Logger.Warn(e);
                return null;
            }
        }
    }
}
