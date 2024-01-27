using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Certificates;
using Windows.Management.Deployment;
using Windows.Graphics.Display;
using System.IO;
using System.Diagnostics;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;

namespace ModernWindow.PackageEngine.Managers
{
    public class Winget : PackageManagerWithSources
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "", "have", "the", "Id" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] { "", "have", "an", "'winget", "pin'", "have", "an", "Version" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "", "have", "an", "'winget", "pin'", "have", "an", "Version" };
        public override async Task<Package[]> FindPackages(string query)
        {
            var Packages = new List<Package>();
            Process p = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " search " + query,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Console.WriteLine(Status.ExecutablePath);
            p.StartInfo = startInfo;
            p.Start();

            string OldLine = "";
            int IdIndex = -1;
            int VersionIndex =-1;
            int SourceIndex = -1;
            bool DashesPassed = false;
            string line;
            while((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                if(!DashesPassed && line.Contains("---"))
                {
                    var HeaderPrefix = OldLine.Contains("SearchId")? "Search": "";
                    IdIndex = OldLine.IndexOf(HeaderPrefix+"Id");
                    VersionIndex = OldLine.IndexOf(HeaderPrefix+"Version");
                    SourceIndex = OldLine.IndexOf(HeaderPrefix+"Source");
                    DashesPassed = true;
                }
                else if (DashesPassed && IdIndex > 0 && VersionIndex > 0 && IdIndex < line.Length && VersionIndex < line.Length)
                {
                    string name = line[..IdIndex].Trim();
                    string id = line[IdIndex..].Trim().Split(' ')[0];
                    string version = line[VersionIndex..].Trim().Split(' ')[0];
                    ManagerSource source;
                    if (SourceIndex == -1 || SourceIndex >= line.Length)
                        source = MainSource;
                    else
                    {
                        string sourceName = line[SourceIndex..].Trim().Split(' ')[0];
                        if (SourceReference.ContainsKey(sourceName))
                            source = SourceReference[sourceName];
                        else
                        {
                            source = new ManagerSource(this, sourceName, new Uri("https://microsoft.com/winget"));
                            SourceReference.Add(source.Name, source);
                        }    
                    }
                    Packages.Add(new Package(name, id, version, source, this));
                }
                OldLine = line;
            }

            await Task.Run(p.WaitForExit);

            Console.WriteLine("Winget finished with package count " + Packages.Count());
            return Packages.ToArray();

        }

        public override Task<UpgradablePackage[]> GetAvailableUpdates()
        {
            throw new NotImplementedException();
        }

        public override Task<Package[]> GetInstalledPackages()
        {
            throw new NotImplementedException();
        }

        public override string[] GetInstallParameters(Package package, InstallationOptions options)
        {
            throw new NotImplementedException();
        }

        public override ManagerSource GetMainSource()
        {
            return new ManagerSource(this, "winget", new Uri("https://cdn.winget.microsoft.com/cache"));
        }

        public override Task<PackageDetails> GetPackageDetails(Package package)
        {
            throw new NotImplementedException();
        }

        public override async Task<ManagerSource[]> GetSources()
        {
            List<ManagerSource> sources = new List<ManagerSource>();

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " source list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.StartInfo = startInfo;
            process.Start();

            bool dashesPassed = false;
            string line;
            while ((line = await process.StandardOutput.ReadLineAsync()) != null)
            {
                try {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    if (!dashesPassed) {
                        if (line.Contains("---"))
                            dashesPassed = true;
                    } else {
                        string[] parts = Regex.Replace(line.Trim(), " {2,}", " ").Split(' ');
                        sources.Add(new ManagerSource(this, parts[0].Trim(), new Uri(parts[1].Trim())));
                    }
                } catch (Exception e) {
                    Console.WriteLine(e);
                }
            }
            await process.WaitForExitAsync();
            return sources.ToArray();
        }

        public override string[] GetUninstallParameters(Package package, InstallationOptions options)
        {
            throw new NotImplementedException();
        }

        public override string[] GetUpdateParameters(Package package, InstallationOptions options)
        {
            throw new NotImplementedException();
        }

        public override async Task RefreshSources()
        {
            Process process = new Process();
            ProcessStartInfo StartInfo = new ProcessStartInfo()
            {
                FileName = Properties.ExecutableFriendlyName,
                Arguments = Properties.ExecutableCallArgs + " source update",
                RedirectStandardOutput = true,
            };
            process.StartInfo = StartInfo;
            process.Start();
            await process.WaitForExitAsync();
        }

        protected override ManagerCapabilities GetCapabilities()
        {
            return new ManagerCapabilities()
            {
                CanRunAsAdmin = true,
                CanSkipIntegrityChecks = true,
                CanRunInteractively = true,
                SupportsCustomVersions = true,
                SupportsCustomArchitectures = true,
                SupportsCustomScopes = true,
                SupportsCustomLocations = true,
                SupportsCustomSources = true,
                Sources = new ManagerSource.Capabilities()
                {
                    KnowsPackageCount = false,
                    KnowsUpdateDate = false,
                }
            };
        }

        protected override ManagerProperties GetProperties()
        {
            ManagerProperties properties = new ManagerProperties()
            {
                Name = "Winget",
                Description = bindings.Translate("Microsoft's official package manager. Full of well-known and verified packages<br>Contains: <b>General Software, Microsoft Store apps</b>"),
                IconId = "winget",
                ColorIconId = "winget_color",
                ExecutableFriendlyName = "winget.exe",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "update",
                ExecutableCallArgs = "",
                
            };
            return properties;
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            var status = new ManagerStatus();
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = "/C echo %PROCESSOR_ARCHITECTURE%",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();

            if(bindings.GetSettings("UseSystemWinget"))
                status.ExecutablePath = await bindings.Which("winget.exe");
            else if(output.Contains("ARM64") | bindings.GetSettings("EnableArmWinget"))
                status.ExecutablePath = Path.Join(Directory.GetParent(Environment.ProcessPath).FullName, "wingetui/PackageEngine/Managers/winget-cli_arm64/winget.exe");
            else
                status.ExecutablePath = Path.Join(Directory.GetParent(Environment.ProcessPath).FullName, "wingetui/PackageEngine/Managers/winget-cli_x64/winget.exe");
            
            status.Found = File.Exists(status.ExecutablePath);

            if(!status.Found)
                return status;

            process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = status.ExecutablePath,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            status.Version = (await process.StandardOutput.ReadToEndAsync()).Trim();

            if (status.Found && IsEnabled())
                await RefreshSources();

            return status;
        }
    }
}
