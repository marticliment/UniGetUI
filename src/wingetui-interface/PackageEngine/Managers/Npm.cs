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

namespace ModernWindow.PackageEngine.Managers
{
    public class Npm : PackageManager
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] { "" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "" };
        public override async Task<Package[]> FindPackages(string query)
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " search " + query + " --parseable",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            p.Start();
            string line;
            List<Package> Packages = new();
            bool HeaderPassed = false;
            while((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                if (!HeaderPassed)
                    if(line.Contains("NAME"))
                        HeaderPassed = true;
                else
                {
                    string[] elements = line.Split('\t');
                    if(elements.Length >= 5)
                        Packages.Add(new Package(bindings.FormatAsName(elements[0]), elements[0], elements[4], MainSource, this));
                }
            }

            await p.WaitForExitAsync();

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
            return new ManagerSource(this, "npm", new Uri("https://www.npmjs.com/"));
        }

        public override Task<PackageDetails> GetPackageDetails(Package package)
        {
            throw new NotImplementedException();
        }

        public override string[] GetUninstallParameters(Package package, InstallationOptions options)
        {
            throw new NotImplementedException();
        }

        public override string[] GetUpdateParameters(Package package, InstallationOptions options)
        {
            throw new NotImplementedException();
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async Task RefreshSources()
        {
            // Npm does not support manual source refreshing
        }

        protected override ManagerCapabilities GetCapabilities()
        {
            return new ManagerCapabilities()
            {
                CanRunAsAdmin = true,
                SupportsCustomVersions = true,
                SupportsCustomScopes = true,
            };
        }

        protected override ManagerProperties GetProperties()
        {
            ManagerProperties properties = new ManagerProperties()
            {
                Name = "Npm",
                Description = bindings.Translate("Node JS's package manager. Full of libraries and other utilities that orbit the javascript world<br>Contains: <b>Node javascript libraries and other related utilities</b>"),
                IconId = "node",
                ColorIconId = "node_color",
                ExecutableFriendlyName = "npm",
                InstallVerb = "install",
                UninstallVerb = "update",
                UpdateVerb = "uninstall",
                ExecutableCallArgs = "-NoProfile -ExecutionPolicy Bypass -Command npm",
                
            };
            return properties;
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            var status = new ManagerStatus();

            status.ExecutablePath = Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe");
            status.Found = File.Exists(await bindings.Which("npm"));

            if(!status.Found)
                return status;

            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " --version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            status.Version = (await process.StandardOutput.ReadToEndAsync()).Trim();
            await process.WaitForExitAsync();

            if (status.Found && IsEnabled())
                await RefreshSources();

            return status;
        }
    }
}
