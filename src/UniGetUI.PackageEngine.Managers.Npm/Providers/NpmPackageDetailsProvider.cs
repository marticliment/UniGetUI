using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Nodes;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.NpmManager
{
    internal sealed class NpmPackageDetailsProvider : BasePackageDetailsProvider<PackageManager>
    {
        public NpmPackageDetailsProvider(Npm manager) : base(manager) { }

        protected override void GetDetails_UnSafe(IPackageDetails details)
        {
            try
            {
                details.InstallerType = "Tarball";
                details.ManifestUrl = new Uri($"https://www.npmjs.com/package/{details.Package.Id}");
                details.ReleaseNotesUrl = new Uri($"https://www.npmjs.com/package/{details.Package.Id}?activeTab=versions");

                using Process p = new();
                p.StartInfo = new ProcessStartInfo
                {
                    FileName = Manager.Status.ExecutablePath,
                    Arguments = Manager.Properties.ExecutableCallArgs + " show " + details.Package.Id + " --json",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                IProcessTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.LoadPackageDetails, p);
                p.Start();

                string strContents = p.StandardOutput.ReadToEnd();
                logger.AddToStdOut(strContents);
                JsonObject? contents = JsonNode.Parse(strContents) as JsonObject;

                details.License = contents?["license"]?.ToString();
                details.Description = contents?["description"]?.ToString();

                if (Uri.TryCreate(contents?["homepage"]?.ToString() ?? "", UriKind.RelativeOrAbsolute, out var homepageUrl))
                    details.HomepageUrl = homepageUrl;

                details.Publisher = (contents?["maintainers"] as JsonArray)?[0]?.ToString();
                details.Author = contents?["author"]?.ToString();
                details.UpdateDate = contents?["time"]?[contents?["dist-tags"]?["latest"]?.ToString() ?? details.Package.Version]?.ToString();

                if (Uri.TryCreate(contents?["dist"]?["tarball"]?.ToString() ?? "", UriKind.RelativeOrAbsolute, out var installerUrl))
                    details.InstallerUrl = installerUrl;

                if(int.TryParse(contents?["dist"]?["unpackedSize"]?.ToString() ?? "", NumberStyles.Any, CultureInfo.InvariantCulture, out int installerSize))
                    details.InstallerSize = installerSize / 1048576d;

                details.InstallerHash = contents?["dist"]?["integrity"]?.ToString();

                logger.AddToStdErr(p.StandardError.ReadToEnd());
                p.WaitForExit();
                logger.Close(p.ExitCode);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            return;
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
            if (package.OverridenOptions.Scope is PackageScope.Local)
                return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "node_modules", package.Id);
            else
                return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Roaming", "npm",
                    "node_modules", package.Id);
        }

        protected override IEnumerable<string> GetInstallableVersions_UnSafe(IPackage package)
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Manager.Status.ExecutablePath,
                    Arguments =
                        Manager.Properties.ExecutableCallArgs + " show " + package.Id + " versions --json",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            IProcessTaskLogger logger = Manager.TaskLogger.CreateNew(LoggableTaskType.LoadPackageVersions, p);
            p.Start();

            string strContents = p.StandardOutput.ReadToEnd();
            logger.AddToStdOut(strContents);
            JsonArray? rawVersions = JsonNode.Parse(strContents) as JsonArray;

            List<string> versions = new();
            foreach(JsonNode? raw_ver in rawVersions ?? [])
                if(raw_ver is not null)
                    versions.Add(raw_ver.ToString());

            logger.AddToStdErr(p.StandardError.ReadToEnd());
            p.WaitForExit();
            logger.Close(p.ExitCode);

            return versions;
        }
    }
}
