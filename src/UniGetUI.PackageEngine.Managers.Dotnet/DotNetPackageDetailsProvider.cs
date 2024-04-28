using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Managers.Generic.NuGet;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.DotNetManager
{
    internal class DotNetPackageDetailsProvider : BasePackageDetailsProvider<PackageManager>
    {
        public DotNetPackageDetailsProvider(DotNet manager) : base(manager) { }

        protected override async Task<PackageDetails> GetPackageDetails_Unsafe(Package package)
        {
            PackageDetails details = new(package);

            try
            {
                details.ManifestUrl = new Uri("https://www.nuget.org/packages/" + package.Id);
                string url = $"http://www.nuget.org/api/v2/Packages(Id='{package.Id}',Version='')";

                using (HttpClient client = new())
                {
                    string apiContents = await client.GetStringAsync(url);

                    details.InstallerUrl = new Uri($"https://globalcdn.nuget.org/packages/{package.Id}.{package.Version}.nupkg");
                    details.InstallerType = CoreTools.Translate("NuPkg (zipped manifest)");
                    details.InstallerSize = await CoreTools.GetFileSizeAsync(details.InstallerUrl);


                    foreach (Match match in Regex.Matches(apiContents, @"<name>[^<>]+<\/name>"))
                    {
                        details.Author = match.Value.Replace("<name>", "").Replace("</name>", "");
                        details.Publisher = match.Value.Replace("<name>", "").Replace("</name>", "");
                        break;
                    }

                    foreach (Match match in Regex.Matches(apiContents, @"<d:Description>[^<>]+<\/d:Description>"))
                    {
                        details.Description = match.Value.Replace("<d:Description>", "").Replace("</d:Description>", "");
                        break;
                    }

                    foreach (Match match in Regex.Matches(apiContents, @"<updated>[^<>]+<\/updated>"))
                    {
                        details.UpdateDate = match.Value.Replace("<updated>", "").Replace("</updated>", "");
                        break;
                    }

                    foreach (Match match in Regex.Matches(apiContents, @"<d:ProjectUrl>[^<>]+<\/d:ProjectUrl>"))
                    {
                        details.HomepageUrl = new Uri(match.Value.Replace("<d:ProjectUrl>", "").Replace("</d:ProjectUrl>", ""));
                        break;
                    }

                    foreach (Match match in Regex.Matches(apiContents, @"<d:LicenseUrl>[^<>]+<\/d:LicenseUrl>"))
                    {
                        details.LicenseUrl = new Uri(match.Value.Replace("<d:LicenseUrl>", "").Replace("</d:LicenseUrl>", ""));
                        break;
                    }

                    foreach (Match match in Regex.Matches(apiContents, @"<d:PackageHash>[^<>]+<\/d:PackageHash>"))
                    {
                        details.InstallerHash = match.Value.Replace("<d:PackageHash>", "").Replace("</d:PackageHash>", "");
                        break;
                    }

                    foreach (Match match in Regex.Matches(apiContents, @"<d:ReleaseNotes>[^<>]+<\/d:ReleaseNotes>"))
                    {
                        details.ReleaseNotes = match.Value.Replace("<d:ReleaseNotes>", "").Replace("</d:ReleaseNotes>", "");
                        break;
                    }

                    foreach (Match match in Regex.Matches(apiContents, @"<d:LicenseNames>[^<>]+<\/d:LicenseNames>"))
                    {
                        details.License = match.Value.Replace("<d:LicenseNames>", "").Replace("</d:LicenseNames>", "");
                        break;
                    }
                }
                return details;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return details;
            }
        }


        protected override async Task<Uri?> GetPackageIcon_Unsafe(Package package)
        {
            return await NuGetIconLoader.GetIconFromManifest(package);
        }

        protected override Task<Uri[]> GetPackageScreenshots_Unsafe(Package package)
        {
            throw new NotImplementedException();
        }

        protected override async Task<string[]> GetPackageVersions_Unsafe(Package package)
        {
            throw new Exception("DotNet does not support custom package versions");
        }
    }
}
