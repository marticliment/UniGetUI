using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Managers.Generic.NuGet.Internal;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.Generic.NuGet
{
    public static class NuGetPackageDetailsLoader
    {
        public static async Task<PackageDetails> GetPackageDetails(Package package)
        {
            PackageDetails details = new(package);
            try
            {
                details.ManifestUrl = PackageManifestLoader.GetPackageManifestUrl(package);
                string? PackageManifestContents = await PackageManifestLoader.GetPackageManifestContent(package);
                if(PackageManifestContents == null)
                {
                    Logger.Warn($"No manifest content could be loaded for package {package.Id} on manager {package.Manager.Name}, returning empty PackageDetails");
                    return details;
                }

                // details.InstallerUrl = new Uri($"https://globalcdn.nuget.org/packages/{package.Id}.{package.Version}.nupkg");
                details.InstallerUrl = PackageManifestLoader.GetPackageNuGetPackageUrl(package);
                details.InstallerType = CoreTools.Translate("NuPkg (zipped manifest)");
                details.InstallerSize = await CoreTools.GetFileSizeAsync(details.InstallerUrl);

                foreach (Match match in Regex.Matches(PackageManifestContents, @"<name>[^<>]+<\/name>"))
                {
                    details.Author = match.Value.Replace("<name>", "").Replace("</name>", "");
                    details.Publisher = match.Value.Replace("<name>", "").Replace("</name>", "");
                    break;
                }

                foreach (Match match in Regex.Matches(PackageManifestContents, @"<d:Description>[^<>]+<\/d:Description>"))
                {
                    details.Description = match.Value.Replace("<d:Description>", "").Replace("</d:Description>", "");
                    break;
                }

                foreach (Match match in Regex.Matches(PackageManifestContents, @"<updated>[^<>]+<\/updated>"))
                {
                    details.UpdateDate = match.Value.Replace("<updated>", "").Replace("</updated>", "");
                    break;
                }

                foreach (Match match in Regex.Matches(PackageManifestContents, @"<d:ProjectUrl>[^<>]+<\/d:ProjectUrl>"))
                {
                    details.HomepageUrl = new Uri(match.Value.Replace("<d:ProjectUrl>", "").Replace("</d:ProjectUrl>", ""));
                    break;
                }

                foreach (Match match in Regex.Matches(PackageManifestContents, @"<d:LicenseUrl>[^<>]+<\/d:LicenseUrl>"))
                {
                    details.LicenseUrl = new Uri(match.Value.Replace("<d:LicenseUrl>", "").Replace("</d:LicenseUrl>", ""));
                    break;
                }

                foreach (Match match in Regex.Matches(PackageManifestContents, @"<d:PackageHash>[^<>]+<\/d:PackageHash>"))
                {
                    details.InstallerHash = match.Value.Replace("<d:PackageHash>", "").Replace("</d:PackageHash>", "");
                    break;
                }

                foreach (Match match in Regex.Matches(PackageManifestContents, @"<d:ReleaseNotes>[^<>]+<\/d:ReleaseNotes>"))
                {
                    details.ReleaseNotes = match.Value.Replace("<d:ReleaseNotes>", "").Replace("</d:ReleaseNotes>", "");
                    break;
                }

                foreach (Match match in Regex.Matches(PackageManifestContents, @"<d:LicenseNames>[^<>]+<\/d:LicenseNames>"))
                {
                    details.License = match.Value.Replace("<d:LicenseNames>", "").Replace("</d:LicenseNames>", "");
                    break;
                }

                return details;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return details;
            }
        }
    }
}
