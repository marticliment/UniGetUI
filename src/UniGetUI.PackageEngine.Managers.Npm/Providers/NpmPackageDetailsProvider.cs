using System.Diagnostics;
using System.Text.Json.Nodes;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

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
                    Arguments = Manager.Properties.ExecutableCallArgs + " info " + details.Package.Id,
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

                string? outLine;
                int lineNo = 0;
                bool ReadingMaintainer = false;
                while ((outLine = p.StandardOutput.ReadLine()) is not null)
                {
                    try
                    {
                        lineNo++;
                        if (lineNo == 2)
                        {
                            details.License = outLine.Split("|")[1];
                        }
                        else if (lineNo == 3)
                        {
                            details.Description = outLine.Trim();
                        }
                        else if (lineNo == 4)
                        {
                            details.HomepageUrl = new Uri(outLine.Trim());
                        }
                        else if (outLine.StartsWith(".tarball"))
                        {
                            details.InstallerUrl = new Uri(outLine.Replace(".tarball: ", "").Trim());
                            details.InstallerSize = CoreTools.GetFileSize(details.InstallerUrl);
                        }
                        else if (outLine.StartsWith(".integrity"))
                        {
                            details.InstallerHash = outLine.Replace(".integrity: sha512-", "").Replace("==", "").Trim();
                        }
                        else if (outLine.StartsWith("maintainers:"))
                        {
                            ReadingMaintainer = true;
                        }
                        else if (ReadingMaintainer)
                        {
                            ReadingMaintainer = false;
                            details.Author = outLine.Replace("-", "").Split('<')[0].Trim();
                        }
                        else if (outLine.StartsWith("published"))
                        {
                            details.Publisher = outLine.Split("by").Last().Split('<')[0].Trim();
                            details.UpdateDate = outLine.Replace("published", "").Split("by")[0].Trim();
                        }
                    }
                    catch (Exception e)
                    {
                        logger.AddToStdErr(e.ToString());
                    }
                }

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
