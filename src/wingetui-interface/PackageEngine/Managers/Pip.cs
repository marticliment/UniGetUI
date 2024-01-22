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
    public class Pip : PackageManager
    {
        public override Task<Package[]> FindPackages(string query)
        {
            throw new NotImplementedException();
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
            return new ManagerSource(this, "pip", new Uri("https://pypi.org/"));
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
            // Pip does not support manual source refreshing
        }

        protected override ManagerCapabilities GetCapabilities()
        {
            return new ManagerCapabilities()
            {
                CanRunAsAdmin = true,
                SupportsCustomVersions = true,
                SupportsCustomScopes = true,
                SupportsPreRelease = true,
            };
        }

        protected override ManagerProperties GetProperties()
        {
            ManagerProperties properties = new ManagerProperties()
            {
                Name = "Pip",
                Description = bindings.Translate("Python's library manager. Full of python libraries and other python-related utilities<br>Contains: <b>Python libraries and related utilities</b>"),
                IconId = "python",
                ColorIconId = "pip_color",
                ExecutableFriendlyName = "pip",
                InstallVerb = "install",
                UninstallVerb = "instal --upgrade",
                UpdateVerb = "uninstall",
                ExecutableCallArgs = "-m pip",
                
            };
            return properties;
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            var status = new ManagerStatus();

            status.ExecutablePath = await bindings.Which("python.exe");
            status.Found = File.Exists(status.ExecutablePath);

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
