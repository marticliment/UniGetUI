using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using ABI.System.Collections.Generic;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Windows.Graphics.Display;

namespace ModernWindow.PackageEngine.Managers;

public class Scoop : PackageManagerWithSources
{
    new public static string[] FALSE_PACKAGE_NAMES = new string[] { "" };
    new public static string[] FALSE_PACKAGE_IDS = new string[] { "No" };
    new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "Matches" };
    public override async Task<Package[]> FindPackages(string query)
    {
        var Packages = new List<Package>();

        string path = await bindings.Which("scoop-search");
        if(!File.Exists(path))
            {
                Process proc = new Process() {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = path,
                        Arguments = Properties.ExecutableCallArgs + " install main/scoop-search",
                        UseShellExecute = true,
                        CreateNoWindow = true
                    }
                };
                proc.Start();
                await proc.WaitForExitAsync();
                path = "scoop-search.exe";
            }

        Process p = new Process() {
            StartInfo = new ProcessStartInfo()
            {
                FileName = path,
                Arguments = query,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        p.Start();

        string line;
        ManagerSource source = GetMainSource();
        while((line = await p.StandardOutput.ReadLineAsync()) != null)
        {
            if(line.StartsWith("'"))
            {
                var sourceName = line.Split(" ")[0].Replace("'", "");
                if(SourceReference.ContainsKey(sourceName))
                    source = SourceReference[sourceName];
                else
                {
                    Console.WriteLine("Unknown source!");
                    source = new ManagerSource(this, sourceName, new Uri("https://scoop.sh/"), 0, "Unknown");
                    SourceReference.Add(sourceName, source);
                }
            }
            else if (line.Trim() != "")
            {
                var elements = line.Trim().Split(" ");
                if(elements.Length < 2)
                    continue;

                for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                    continue;

                Packages.Add(new Package(bindings.FormatAsName(elements[0]), elements[0], elements[1].Replace("(", "").Replace(")", ""), source, this));
            }
        }
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
        status.Found = process.ExitCode == 0;
        

        if (status.Found && IsEnabled())
            _ = RefreshSources();

        return status;
    }
}