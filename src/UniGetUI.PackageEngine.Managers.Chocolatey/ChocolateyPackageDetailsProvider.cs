using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Managers.ChocolateyManager;
using UniGetUI.PackageEngine.Managers.Generic.NuGet;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.ChocolateyManager
{
    internal class ChocolateyPackageDetailsProvider : BasePackageDetailsProvider<PackageManager>
    {
        public ChocolateyPackageDetailsProvider(Chocolatey manager) : base(manager) { }

        protected override async Task<PackageDetails> GetPackageDetails_Unsafe(Package package)
        {
            PackageDetails details = new(package);

            if (package.Source == Manager.Properties.DefaultSource)
                details.ManifestUrl = new Uri("https://community.chocolatey.org/packages/" + package.Id);
            else if (package.Source.Url != null && package.Source.Url.ToString().Trim()[^1].ToString() == "/")
                details.ManifestUrl = new Uri((package.Source.Url.ToString().Trim() + "package/" + package.Id).Replace("//", "/").Replace(":/", "://"));


            if (package.Source == Manager.Properties.DefaultSource)
            {
                try
                {
                    details.InstallerType = CoreTools.Translate("NuPkg (zipped manifest)");
                    details.InstallerUrl = new Uri("https://packages.chocolatey.org/" + package.Id + "." + package.Version + ".nupkg");
                    details.InstallerSize = await CoreTools.GetFileSizeAsync(details.InstallerUrl);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            Process process = new();
            List<string> output = new();
            ProcessStartInfo startInfo = new()
            {
                FileName = Manager.Status.ExecutablePath,
                Arguments = Manager.Properties.ExecutableCallArgs + " info " + package.Id,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            process.StartInfo = startInfo;
            process.Start();

            string _line;
            while ((_line = await process.StandardOutput.ReadLineAsync()) != null)
                if (_line.Trim() != "")
                {
                    output.Add(_line);
                }

            // Parse the output
            bool IsLoadingDescription = false;
            bool IsLoadingReleaseNotes = false;
            bool IsLoadingTags = false;
            foreach (string __line in output)
            {
                try
                {
                    string line = __line.TrimEnd();
                    if (line == "")
                        continue;

                    // Check if a multiline field is being loaded
                    if (line.StartsWith("  ") && IsLoadingDescription)
                        details.Description += "\n" + line.Trim();
                    else if (line.StartsWith("  ") && IsLoadingReleaseNotes)
                        details.ReleaseNotes += "\n" + line.Trim();

                    // Stop loading multiline fields
                    else if (IsLoadingDescription)
                        IsLoadingDescription = false;
                    else if (IsLoadingReleaseNotes)
                        IsLoadingReleaseNotes = false;
                    else if (IsLoadingTags)
                        IsLoadingTags = false;

                    // Check for singleline fields
                    if (line.StartsWith(" ") && line.Contains("Title:"))
                        details.UpdateDate = line.Split("|")[1].Trim().Replace("Published:", "");

                    else if (line.StartsWith(" ") && line.Contains("Author:"))
                        details.Author = line.Split(":")[1].Trim();

                    else if (line.StartsWith(" ") && line.Contains("Software Site:"))
                        details.HomepageUrl = new Uri(line.Replace("Software Site:", "").Trim());

                    else if (line.StartsWith(" ") && line.Contains("Software License:"))
                        details.LicenseUrl = new Uri(line.Replace("Software License:", "").Trim());

                    else if (line.StartsWith(" ") && line.Contains("Package Checksum:"))
                        details.InstallerHash = line.Split(":")[1].Trim().Replace("'", "");

                    else if (line.StartsWith(" ") && line.Contains("Description:"))
                    {
                        details.Description = line.Split(":")[1].Trim();
                        IsLoadingDescription = true;
                    }
                    else if (line.StartsWith(" ") && line.Contains("Release Notes:"))
                    {
                        details.ReleaseNotesUrl = new Uri(line.Replace("Release Notes:", "").Trim());
                        details.ReleaseNotes = "";
                        IsLoadingReleaseNotes = true;
                    }
                    else if (line.StartsWith(" ") && line.Contains("Tags"))
                    {
                        List<string> tags = new();
                        foreach (string tag in line.Replace("Tags:", "").Trim().Split(' '))
                        {
                            if (tag.Trim() != "")
                                tags.Add(tag.Trim());
                        }
                        details.Tags = tags.ToArray();
                    }
                }
                catch (Exception e)
                {
                    Logger.Warn("Error occurred while parsing line value=\"" + _line + "\"");
                    Logger.Warn(e.Message);
                }
            }

            return details;
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
            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Manager.Status.ExecutablePath,
                    Arguments = Manager.Properties.ExecutableCallArgs + " find -e " + package.Id + " -a",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            p.Start();
            string line;
            string output = "";
            List<string> versions = new();
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!line.StartsWith("Chocolatey"))
                {
                    string[] elements = line.Split(' ');
                    if (elements.Length < 2 || elements[0].Trim() != package.Id)
                        continue;

                    versions.Add(elements[1].Trim());
                }
            }
            output += await p.StandardError.ReadToEndAsync();
            Manager.LogOperation(p, output);

            return versions.ToArray();
        }
    }
}
