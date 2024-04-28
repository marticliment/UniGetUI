using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.Generic.NuGet
{
    public static class NuGetIconLoader
    {
        public static async Task<Uri?> GetIconFromManifest(Package package)
        {
            Uri PackageManifest = new Uri($"{package.Source.Url}/Packages(Id='{package.Name}',Version='{package.Version}')");

            string PackageManifestContent = "";
            try
            {
                HttpClientHandler handler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.All
                };

                using (HttpClient client = new HttpClient(handler))
                {
                    var response = await client.GetAsync(PackageManifest);
                    if (!response.IsSuccessStatusCode && package.Version.EndsWith(".0"))
                        response = await client.GetAsync(new Uri(PackageManifest.ToString().Replace(".0')", "')")));

                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.Warn($"Failed to download the {package.Manager.Name} manifest at Url={PackageManifest.ToString()} with status code {response.StatusCode}");
                        return null;
                    }

                    PackageManifestContent = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception e)
            {
                Logger.Warn($"Failed to download the {package.Manager.Name} manifest at Url={PackageManifest.ToString()}");
                Logger.Warn(e);
                return null;
            }

            var possibleIconUrl = Regex.Match(PackageManifestContent, "<(?:d\\:)?IconUrl>(.*)<(?:\\/d:)?IconUrl>");

            Logger.Error(PackageManifestContent);
            Logger.Error(possibleIconUrl.Groups.ToString() ?? "");

            if (!possibleIconUrl.Success)
            {
                Logger.Warn($"No Icon URL could be parsed on the manifest Url={PackageManifest.ToString()}");
                return null;
            }

            Logger.Debug($"A native icon with Url={possibleIconUrl.Groups[1].Value} was found");
            return new Uri(possibleIconUrl.Groups[1].Value);

        }
    }
}
