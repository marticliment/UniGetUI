using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Managers.Generic.NuGet.Internal;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.Generic.NuGet
{
    public static class NuGetIconLoader
    {
        public static async Task<Uri?> GetIconFromManifest(Package package)
        {
            var PackageManifestContent = await PackageManifestLoader.GetPackageManifestContent(package);
            if(PackageManifestContent == null)
            {
                Logger.Warn($"No manifest content could be loaded for package {package.Id} on manager {package.Manager.Name}");
                return null;
            }

            var possibleIconUrl = Regex.Match(PackageManifestContent, "<(?:d\\:)?IconUrl>(.*)<(?:\\/d:)?IconUrl>");

            Logger.Error(PackageManifestContent);
            Logger.Error(possibleIconUrl.Groups.ToString() ?? "");

            if (!possibleIconUrl.Success)
            {
                Logger.Warn($"No Icon URL could be parsed on the manifest Url={PackageManifestLoader.GetPackageManifestUrl(package).ToString()}");
                return null;
            }

            Logger.Debug($"A native icon with Url={possibleIconUrl.Groups[1].Value} was found");
            return new Uri(possibleIconUrl.Groups[1].Value);

        }
    }
}
