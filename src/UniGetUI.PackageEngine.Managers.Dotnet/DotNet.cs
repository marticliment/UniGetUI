using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UniGetUI.Core;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Managers;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.DotNetManager
{
    public class DotNet : PackageManager
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] { "" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "" };

        public DotNet() : base()
        {
            Capabilities = new ManagerCapabilities()
            {
                CanRunAsAdmin = true,
                SupportsCustomScopes = true,
                SupportsCustomArchitectures = true,
                SupportedCustomArchitectures = new Architecture[] { Architecture.X86, Architecture.X64, Architecture.Arm64, Architecture.Arm },
                SupportsPreRelease = true,
                SupportsCustomLocations = true
            };

            Properties = new ManagerProperties()
            {
                Name = ".NET Tool",
                Description = CoreTools.Translate("A repository full of tools and executables designed with Microsoft's .NET ecosystem in mind.<br>Contains: <b>.NET related tools and scripts</b>"),
                IconId = "dotnet",
                ColorIconId = "dotnet_color",
                ExecutableFriendlyName = "dotnet tool",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "update",
                ExecutableCallArgs = "tool",
                DefaultSource = new ManagerSource(this, "nuget.org", new Uri("https://www.nuget.org/")),
                KnownSources = [new ManagerSource(this, "nuget.org", new Uri("https://www.nuget.org/"))],
            };
        }



        protected override async Task<Package[]> FindPackages_UnSafe(string query)
        {

            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " search \"" + query + "\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            p.Start();
            string line;
            List<Package> Packages = new();
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
                    if (elements.Length >= 2)
                        Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], DefaultSource, this, PackageScope.Global));
                    // Dotnet tool packages are installed globally by default, hence the Global flag
                }
            }

            output += await p.StandardError.ReadToEndAsync();
            LogOperation(p, output);

            await p.WaitForExitAsync();

            return Packages.ToArray();
        }

        protected override async Task<UpgradablePackage[]> GetAvailableUpdates_UnSafe()
        {
            var which_res = await CoreTools.Which("dotnet-tools-outdated.exe");
            string path = which_res.Item2;
            if (!which_res.Item1)
            {
                Process proc = new()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = path,
                        Arguments = Properties.ExecutableCallArgs + " install --global dotnet-tools-outdated",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                    }
                };
                proc.Start();
                await proc.WaitForExitAsync();
                path = "dotnet-tools-outdated.exe";
            }

            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = path,
                    Arguments = "",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            p.Start();

            string line;
            bool DashesPassed = false;
            List<UpgradablePackage> Packages = new();
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!DashesPassed)
                {
                    if (line.Contains("----"))
                        DashesPassed = true;
                }
                else
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                    if (elements.Length < 3)
                        continue;

                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();
                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;

                    Packages.Add(new UpgradablePackage(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], elements[2], DefaultSource, this, PackageScope.Global));
                }
            }
            output += await p.StandardError.ReadToEndAsync();
            LogOperation(p, output);

            return Packages.ToArray();
        }

        protected override async Task<Package[]> GetInstalledPackages_UnSafe()
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " list",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            p.Start();

            string line;
            bool DashesPassed = false;
            List<Package> Packages = new();
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                if (!DashesPassed)
                {
                    if (line.Contains("----"))
                        DashesPassed = true;
                }
                else
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                    if (elements.Length < 2)
                        continue;

                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();
                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;

                    Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], DefaultSource, this, PackageScope.User));
                }
            }

            p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " list --global",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            p.Start();

            DashesPassed = false;
            string output = "";
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!DashesPassed)
                {
                    if (line.Contains("----"))
                        DashesPassed = true;
                }
                else
                {
                    string[] elements = Regex.Replace(line, " {2,}", " ").Split(' ');
                    if (elements.Length < 2)
                        continue;

                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();
                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;

                    Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], DefaultSource, this, PackageScope.Global));
                }
            }
            output += await p.StandardError.ReadToEndAsync();
            LogOperation(p, output);

            return Packages.ToArray();
        }


        public override OperationVeredict GetInstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override OperationVeredict GetUpdateOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override OperationVeredict GetUninstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }
        public override string[] GetInstallParameters(Package package, InstallationOptions options)
        {
            string[] parameters = GetUpdateParameters(package, options);
            parameters[0] = Properties.InstallVerb;
            return parameters;
        }
        public override string[] GetUpdateParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = GetUninstallParameters(package, options).ToList();
            parameters[0] = Properties.UpdateVerb;

            if (options.Architecture == Architecture.X86)
                parameters.AddRange(new string[] { "--arch", "x86" });
            else if (options.Architecture == Architecture.X64)
                parameters.AddRange(new string[] { "--arch", "x64" });
            else if (options.Architecture == Architecture.Arm)
                parameters.AddRange(new string[] { "--arch", "arm32" });
            else if (options.Architecture == Architecture.Arm64)
                parameters.AddRange(new string[] { "--arch", "arm64" });

            return parameters.ToArray();
        }

        public override string[] GetUninstallParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = new() { Properties.UninstallVerb, package.Id };

            if (options.CustomParameters != null)
                parameters.AddRange(options.CustomParameters);

            if (options.CustomInstallLocation != "")
                parameters.AddRange(new string[] { "--tool-path", "\"" + options.CustomInstallLocation + "\"" });
            else if (package.Scope == PackageScope.Global)
                parameters.Add("--global");

            return parameters.ToArray();
        }

        public override async Task<PackageDetails> GetPackageDetails_UnSafe(Package package)
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
                Logger.Log(e);
                return details;
            }
        }


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async Task RefreshPackageIndexes()
        {
            // .NET Tool does not support manual source refreshing
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            ManagerStatus status = new();

            var which_res = await CoreTools.Which("dotnet.exe");
            status.ExecutablePath = which_res.Item2;
            status.Found = which_res.Item1;

            if (!status.Found)
                return status;

            Process process = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = status.ExecutablePath,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };
            process.Start();
            status.Version = (await process.StandardOutput.ReadToEndAsync()).Trim();

            if (status.Found && IsEnabled())
                await RefreshPackageIndexes();

            return status;
        }

        protected override async Task<string[]> GetPackageVersions_Unsafe(Package package)
        {
            await Task.Delay(0);
            Logger.Log("Manager " + Name + " does not support version retrieving, this function should have never been called");
            return new string[0];
        }
    }
}
