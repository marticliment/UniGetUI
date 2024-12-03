using System.Collections;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Threading.Tasks.Dataflow;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Managers.ScoopManager
{
    internal sealed class ScoopPkgDetailsHelper : BasePkgDetailsHelper
    {
        public ScoopPkgDetailsHelper(Scoop manager) : base(manager) { }

        protected override void GetDetails_UnSafe(IPackageDetails details)
        {
            if (details.Package.Source.Url is not null)
            {
                try
                {
                    details.ManifestUrl = new Uri(details.Package.Source.Url.ToString() + "/blob/master/bucket/" + details.Package.Id + ".json");
                }
                catch (Exception ex)
                {
                    Logger.Error("Cannot load package manifest URL");
                    Logger.Error(ex);
                }
            }

            string packageId;
            if(details.Package.Source.Name.Contains("..."))
                packageId = $"{details.Package.Id}";
            else
                packageId = $"{details.Package.Source.Name}/{details.Package.Id}";

            Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Manager.Status.ExecutablePath,
                    Arguments = Manager.Properties.ExecutableCallArgs + " cat " + packageId,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                }
            };

            IProcessTaskLogger logger = Manager.TaskLogger.CreateNew(Enums.LoggableTaskType.LoadPackageDetails, p);

            p.Start();
            string JsonString = p.StandardOutput.ReadToEnd();
            logger.AddToStdOut(JsonString);
            logger.AddToStdErr(p.StandardError.ReadToEnd());


            if (JsonNode.Parse(JsonString) is not JsonObject contents)
            {
                throw new InvalidOperationException("Deserialized RawInfo was null");
            }

            // Load description
            if (contents["description"] is JsonArray descriptionList)
            {
                details.Description = "";
                foreach (var line in descriptionList)
                    details.Description += line + "\n";
            }
            else
            {
                details.Description = contents["description"]?.ToString();
            }

            // Load installer type
            if (contents["innsetup"]?.ToString() == "true")
                details.InstallerType = "Inno Setup (" + CoreTools.Translate("extracted") + ")";
            else
                details.InstallerType = CoreTools.Translate("Scoop package");

            // Load homepage and author
            if (Uri.TryCreate(contents?["homepage"]?.ToString() ?? "", UriKind.RelativeOrAbsolute, out var homepageUrl))
            {
                details.HomepageUrl = homepageUrl;

                if(homepageUrl.ToString().StartsWith("https://github.com/"))
                    details.Author = homepageUrl.ToString().Replace("https://github.com/", "").Split("/")[0];
                else
                    details.Author = homepageUrl.Host.Split(".")[^2];
            }

            // Load notes
            if (contents?["notes"] is JsonArray notesList)
            {
                details.ReleaseNotes = "";
                foreach (var line in notesList)
                    details.ReleaseNotes += line + "\n";
            }
            else
            {
                details.ReleaseNotes = contents?["notes"]?.ToString();
            }

            if (contents?["license"] is JsonObject licenseDetails)
            {
                details.License = licenseDetails["identifier"]?.ToString();
                if (Uri.TryCreate(licenseDetails["url"]?.ToString(), UriKind.RelativeOrAbsolute, out var licenseUrl))
                    details.LicenseUrl = licenseUrl;
            }
            else
            {
                details.License = contents?["license"]?.ToString();
            }


            // Load installers
            if (contents?["url"] is JsonArray urlList)
            {
                // Only one installer
                if (Uri.TryCreate(urlList[0]?.ToString(), UriKind.RelativeOrAbsolute, out var installerUrl))
                    details.InstallerUrl = installerUrl;

                details.InstallerHash = (contents?["hash"] as JsonArray)?[0]?.ToString();
            }
            else if (contents?["url"] is JsonValue value)
            {
                // Multiple installers
                if (Uri.TryCreate(value.ToString(), UriKind.RelativeOrAbsolute, out var installerUrl))
                    details.InstallerUrl = installerUrl;

                details.InstallerHash = contents?["hash"]?.ToString();
            }
            else if(contents?["architecture"] is JsonObject archNode)
            {
                // Architecture-based installer
                string arch = archNode.ContainsKey("64bit") ? "64bit" : archNode.First().Key;

                if (Uri.TryCreate(archNode[arch]?["url"]?.ToString(), UriKind.RelativeOrAbsolute, out var installerUrl))
                    details.InstallerUrl = installerUrl;

                details.InstallerHash = archNode[arch]?["hash"]?.ToString();
            }

            if (details.InstallerUrl is not null)
                details.InstallerSize = CoreTools.GetFileSize(details.InstallerUrl);

            // Load release notes URL
            if (contents?["checkver"] is JsonObject checkver && Uri.TryCreate(checkver["url"]?.ToString(), UriKind.RelativeOrAbsolute,
                    out var releaseNotesUrl))
                details.ReleaseNotesUrl = releaseNotesUrl;

            logger.Close(0);
        }

        protected override CacheableIcon? GetIcon_UnSafe(IPackage package)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<Uri> GetScreenshots_UnSafe(IPackage package)
        {
            throw new NotImplementedException();
        }

        protected override string? GetInstallLocation_UnSafe(IPackage package)
        {
            return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "apps",
                package.Id, "current");
        }

        protected override IEnumerable<string> GetInstallableVersions_UnSafe(IPackage package)
        {
            throw new InvalidOperationException("Scoop does not support custom package versions");
        }
    }
}
