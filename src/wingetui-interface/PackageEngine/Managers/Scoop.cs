using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using Windows.Graphics.Display;

namespace ModernWindow.PackageEngine.Managers;

public class Scoop : PackageManagerWithSources
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
        return new ManagerSource(this, "main", new Uri("https://github.com/ScoopInstaller/Main"), 0, "");
    }

    public override Task<PackageDetails> GetPackageDetails(Package package)
    {
        throw new NotImplementedException();
    }

    public override async Task<ManagerSource[]> GetSources()
    {
        Console.WriteLine("🔵 Starting " + Name + " source search...");
        using (Process process = new Process())
        {
            process.StartInfo.FileName = Status.ExecutablePath;
            process.StartInfo.Arguments = Properties.ExecutableCallArgs + " bucket list";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false;

            List<string> output = new List<string>();
            List<ManagerSource> sources = new List<ManagerSource>();

            process.Start();

            string line;
            while ((line = await process.StandardOutput.ReadLineAsync()) != null)
            {
                Debug.WriteLine(line);
                try
                {
                    string[] elements = Regex.Replace(line.Trim(), " {2,}", " ").Split(' ');
                    if (elements.Length >= 5)
                        sources.Add(new ManagerSource(this, elements[0].Trim(), new Uri(elements[1].Trim()), int.Parse(elements[4].Trim()), elements[2].Trim() + " " + elements[3].Trim()));
                    else if (elements.Length >= 2)
                        sources.Add(new ManagerSource(this, elements[0].Trim(), new Uri(elements[1].Trim()), 0, "Unknown"));
                    else if (elements.Length >= 1)
                        sources.Add(new ManagerSource(this, elements[0].Trim(), new Uri(""), 0, "Unknown"));
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
            
            await process.WaitForExitAsync();
            Debug.WriteLine("🔵 " + Name + " source search finished.");
            return sources.ToArray();
        }
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
            Arguments = Properties.ExecutableCallArgs + " update"
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
            CanRemoveDataOnUninstall = true,
            SupportsCustomArchitectures = true,
            SupportsCustomScopes = true,
            SupportsCustomSources = true,
            Sources = new ManagerSource.Capabilities()
            {
                KnowsPackageCount = true,
                KnowsUpdateDate = true
            }
        };
    }

    protected override ManagerProperties GetProperties()
    {
        return new ManagerProperties()
        {
            Name = "Scoop",
            Description = bindings.Translate("Great repository of unknown but useful utilities and other interesting packages.<br>Contains: <b>Utilities, Command-line programs, General Software (extras bucket required)</b>"),
            IconId = "scoop",
            ColorIconId = "scoop_color",
            ExecutableCallArgs = "-NoProfile -ExecutionPolicy Bypass -Command scoop",
            ExecutableFriendlyName = "scoop",
            InstallVerb = "install",
            UpdateVerb = "update",
            UninstallVerb = "uninstall"
        };
    }

    protected override async Task<ManagerStatus> LoadManager()
    {
        var status = new ManagerStatus
        {
            ExecutablePath = Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe")
        };
        status.Found = File.Exists(status.ExecutablePath) && File.Exists(bindings.Which("scoop"));

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
        status.Version = await process.StandardOutput.ReadLineAsync();
        

        if (status.Found && IsEnabled())
            await RefreshSources();

        return status;
    }
}