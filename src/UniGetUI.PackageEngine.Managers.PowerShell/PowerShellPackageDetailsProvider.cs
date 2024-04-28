using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.Classes.Manager.BaseProviders;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Managers.Generic.NuGet;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.PowerShellManager
{
    internal class PowerShellPackageDetailsProvider : BasePackageDetailsProvider<PackageManager>
    {
        public PowerShellPackageDetailsProvider(PowerShell manager) : base(manager) { }

        protected override async Task<PackageDetails> GetPackageDetails_Unsafe(Package package)
        {
            PackageDetails details = new(package);

            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Manager.Status.ExecutablePath,
                Arguments = Manager.Properties.ExecutableCallArgs + " Find-Module -Name " + package.Id + " | Get-Member -MemberType NoteProperty | Select-Object Definition",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            p.Start();

            string line;
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                if (line.Count(c => c == ' ') < 1)
                    continue;

                try
                {
                    string entry = line.Split('=')[0].Split(' ')[^1];
                    string PossibleContent = line.Split('=')[1].Trim();
                    if (PossibleContent == "null")
                        continue;

                    if (entry == "Author")
                        details.Author = PossibleContent;

                    else if (entry == ("CompanyName"))
                        details.Publisher = PossibleContent;

                    else if (entry == ("Copyright"))
                        details.License = PossibleContent;

                    else if (entry == ("LicenseUri"))
                        details.LicenseUrl = new Uri(PossibleContent);

                    else if (entry == ("Description"))
                        details.Description = PossibleContent;

                    else if (entry == ("Type"))
                        details.InstallerType = PossibleContent;

                    else if (entry == ("ProjectUri"))
                        details.HomepageUrl = new Uri(PossibleContent);

                    else if (entry == ("PublishedDate"))
                        details.UpdateDate = PossibleContent;

                    else if (entry == ("UpdatedDate"))
                        details.UpdateDate = PossibleContent;

                    else if (entry == ("ReleaseNotes"))
                        details.ReleaseNotes = PossibleContent;

                }
                catch (Exception ex)
                {
                    Logger.Warn(ex);
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
                    Arguments = Manager.Properties.ExecutableCallArgs + " Find-Module -Name " + package.Id + " -AllVersions",
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
            List<string> versions = new();
            bool DashesPassed = false;
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!DashesPassed)
                {
                    if (line.Contains("-----"))
                        DashesPassed = true;
                }
                else
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                    if (elements.Length < 3)
                        continue;

                    versions.Add(elements[0]);
                }
            }

            return versions.ToArray();
        }
    }
}
