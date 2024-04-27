using System.Diagnostics;
using System.Runtime.InteropServices;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.ChocolateyManager
{
    public class Chocolatey : PackageManager
    {
        new public static string[] FALSE_PACKAGE_NAMES = new string[] { "" };
        new public static string[] FALSE_PACKAGE_IDS = new string[] { "Directory", "", "Did", "Features?", "Validation", "-", "being", "It", "Error", "L'accs", "Maximum", "This", "Output is package name ", "operable", "Invalid" };
        new public static string[] FALSE_PACKAGE_VERSIONS = new string[] { "", "Did", "Features?", "Validation", "-", "being", "It", "Error", "L'accs", "Maximum", "This", "packages", "current version", "installed version", "is", "program", "validations", "argument", "no" };
        
        public Chocolatey()
        {
            SourceProvider = new ChocolateySourceProvider(this);
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
            string output = "";
            List<Package> Packages = new();
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!line.StartsWith("Chocolatey"))
                {
                    string[] elements = line.Split(' ');
                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                    if (elements.Length < 2)
                        continue;

                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;

                    Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], DefaultSource, this));
                }
            }

            output += await p.StandardError.ReadToEndAsync();
            // AppTools.LogManagerOperation(this, p, output);

            await p.WaitForExitAsync();

            return Packages.ToArray();
        }

        protected override async Task<UpgradablePackage[]> GetAvailableUpdates_UnSafe()
        {
            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " outdated",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            p.Start();
            string line;
            string output = "";
            List<UpgradablePackage> Packages = new();
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!line.StartsWith("Chocolatey"))
                {
                    string[] elements = line.Split('|');
                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                    if (elements.Length <= 2)
                        continue;

                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;

                    Packages.Add(new UpgradablePackage(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], elements[2], DefaultSource, this));
                }
            }

            output += await p.StandardError.ReadToEndAsync();
            // AppTools.LogManagerOperation(this, p, output);

            await p.WaitForExitAsync();

            return Packages.ToArray();
        }

        protected override async Task<Package[]> GetInstalledPackages_UnSafe()
        {
            Process p = new();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            p.Start();
            string line;
            string output = "";
            List<Package> Packages = new();
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                output += line + "\n";
                if (!line.StartsWith("Chocolatey"))
                {
                    string[] elements = line.Split(' ');
                    for (int i = 0; i < elements.Length; i++) elements[i] = elements[i].Trim();

                    if (elements.Length <= 1)
                        continue;

                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                        continue;

                    Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], DefaultSource, this));
                }
            }

            output += await p.StandardError.ReadToEndAsync();
            // AppTools.LogManagerOperation(this, p, output);

            await p.WaitForExitAsync();

            return Packages.ToArray();
        }
        public override OperationVeredict GetInstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            string output_string = string.Join("\n", Output);

            if (ReturnCode == 1641 || ReturnCode == 0)
                return OperationVeredict.Succeeded;
            else if (ReturnCode == 3010)
                return OperationVeredict.Succeeded; // TODO: Restart required
            else if ((output_string.Contains("Run as administrator") || output_string.Contains("The requested operation requires elevation") || output_string.Contains("ERROR: Exception calling \"CreateDirectory\" with \"1\" argument(s): \"Access to the path")) && !options.RunAsAdministrator)
            {
                options.RunAsAdministrator = true;
                return OperationVeredict.AutoRetry;
            }
            return OperationVeredict.Failed;
        }

        public override OperationVeredict GetUpdateOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return GetInstallOperationVeredict(package, options, ReturnCode, Output);
        }

        public override OperationVeredict GetUninstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            string output_string = string.Join("\n", Output);

            if (ReturnCode == 1641 || ReturnCode == 1614 || ReturnCode == 1605 || ReturnCode == 0)
                return OperationVeredict.Succeeded;
            else if (ReturnCode == 3010)
                return OperationVeredict.Succeeded; // TODO: Restart required
            else if ((output_string.Contains("Run as administrator") || output_string.Contains("The requested operation requires elevation")) && !options.RunAsAdministrator)
            {
                options.RunAsAdministrator = true;
                return OperationVeredict.AutoRetry;
            }
            return OperationVeredict.Failed;
        }
        public override string[] GetInstallParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = GetUninstallParameters(package, options).ToList();
            parameters[0] = Properties.InstallVerb;
            parameters.Add("--no-progress");

            if (options.Architecture == System.Runtime.InteropServices.Architecture.X86)
                parameters.Add("--forcex86");

            if (options.PreRelease)
                parameters.Add("--prerelease");

            if (options.SkipHashCheck)
                parameters.AddRange(new string[] { "--ignore-checksums", "--force" });

            if (options.Version != "")
                parameters.AddRange(new string[] { "--version=" + options.Version, "--allow-downgrade" });

            return parameters.ToArray();
        }
        public override string[] GetUpdateParameters(Package package, InstallationOptions options)
        {
            string[] parameters = GetInstallParameters(package, options);
            parameters[0] = Properties.UpdateVerb;
            return parameters;
        }

        public override string[] GetUninstallParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = new() { Properties.UninstallVerb, package.Id, "-y" };

            if (options.CustomParameters != null)
                parameters.AddRange(options.CustomParameters);

            if (options.InteractiveInstallation)
                parameters.Add("--notsilent");

            return parameters.ToArray();
        }


        public override async Task<PackageDetails> GetPackageDetails_UnSafe(Package package)
        {
            PackageDetails details = new(package);

            if (package.Source.Name == "community")
                details.ManifestUrl = new Uri("https://community.chocolatey.org/packages/" + package.Id);
            else if (package.Source.Url != null && package.Source.Url.ToString().Trim()[^1].ToString() == "/")
                details.ManifestUrl = new Uri((package.Source.Url.ToString().Trim() + "package/" + package.Id).Replace("//", "/").Replace(":/", "://"));



            if (package.Source.Name == "community")
            {
                try
                {
                    details.InstallerType = CoreTools.Translate("NuPkg (zipped manifest)");
                    details.InstallerUrl = new Uri("https://packages.chocolatey.org/" + package.Id + "." + package.Version + ".nupkg");
                    details.InstallerSize = await CoreTools.GetFileSizeAsync(details.InstallerUrl);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }

            Process process = new();
            List<string> output = new();
            ProcessStartInfo startInfo = new()
            {
                FileName = Status.ExecutablePath,
                Arguments = Properties.ExecutableCallArgs + " info " + package.Id,
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
                    Logger.Log(_line);
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
                    Logger.Log("Error occurred while parsing line value=\"" + _line + "\"");
                    Logger.Log(e.Message);
                }
            }

            return details;
        }

#pragma warning disable CS1998
        public override async Task RefreshPackageIndexes()
        {
            // Chocolatey does not support source refreshing
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
                SupportedCustomArchitectures = new Architecture[] { Architecture.X86 },
                SupportsPreRelease = true,
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
            ManagerProperties properties = new()
            {
                Name = "Chocolatey",
                Description = CoreTools.Translate("The classical package manager for windows. You'll find everything there. <br>Contains: <b>General Software</b>"),
                IconId = "choco",
                ColorIconId = "choco_color",
                ExecutableFriendlyName = "choco.exe",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "upgrade",
                ExecutableCallArgs = "",
                KnownSources = [new ManagerSource(this, "chocolatey", new Uri("https://community.chocolatey.org/api/v2/"))],
                DefaultSource = new ManagerSource(this, "chocolatey", new Uri("https://community.chocolatey.org/api/v2/")),

            };
            return properties;
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            ManagerStatus status = new();

            string old_choco_path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs\\WingetUI\\choco-cli");
            string new_choco_path = Path.Join(CoreData.UniGetUIDataDirectory, "Chocolatey");

            if (Directory.Exists(old_choco_path))
                try
                {
                    Logger.Log("Moving Bundled Chocolatey from old path to new path...");

                    if (!Directory.Exists(new_choco_path))
                        Directory.CreateDirectory(new_choco_path);

                    foreach (string old_subdir in Directory.GetDirectories(old_choco_path, "*", SearchOption.AllDirectories))
                    {
                        string new_subdir = old_subdir.Replace(old_choco_path, new_choco_path);
                        if (!Directory.Exists(new_subdir))
                        {
                            Debug.WriteLine("New directory: " + new_subdir);
                            await Task.Run(() => Directory.CreateDirectory(new_subdir));
                        }
                        else
                            Debug.WriteLine("Directory " + new_subdir + " already exists");
                    }

                    foreach (string old_file in Directory.GetFiles(old_choco_path, "*", SearchOption.AllDirectories))
                    {
                        string new_file = old_file.Replace(old_choco_path, new_choco_path);
                        if (!File.Exists(new_file))
                        {
                            Debug.WriteLine("Copying " + old_file);
                            await Task.Run(() => File.Move(old_file, new_file));
                        }
                        else
                        {
                            Debug.WriteLine("File " + new_file + " already exists.");
                            File.Delete(old_file);
                        }
                    }

                    foreach (string old_subdir in Directory.GetDirectories(old_choco_path, "*", SearchOption.AllDirectories).Reverse())
                    {
                        if (!Directory.EnumerateFiles(old_subdir).Any() && !Directory.EnumerateDirectories(old_subdir).Any())
                        {
                            Debug.WriteLine("Deleting old empty subdirectory " + old_subdir);
                            Directory.Delete(old_subdir);
                        }
                    }

                    if (!Directory.EnumerateFiles(old_choco_path).Any() && !Directory.EnumerateDirectories(old_choco_path).Any())
                    {
                        Debug.WriteLine("Deleting old Chocolatey directory " + old_choco_path);
                        Directory.Delete(old_choco_path);
                    }


                }
                catch (Exception e)
                {
                    Logger.Log(e);
                }

            if (Settings.Get("UseSystemChocolatey"))
                status.ExecutablePath = (await CoreTools.Which("choco.exe")).Item2;
            else if (File.Exists(Path.Join(new_choco_path, "choco.exe")))
                status.ExecutablePath = Path.Join(new_choco_path, "choco.exe");
            else
                status.ExecutablePath = Path.Join(Directory.GetParent(Environment.ProcessPath).FullName, "choco-cli\\choco.exe");

            status.Found = File.Exists(status.ExecutablePath);

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

            // If the user is running bundled chocolatey and chocolatey is not in path, add chocolatey to path
            if (/*Settings.Get("ShownWelcomeWizard") && */!Settings.Get("UseSystemChocolatey") && !File.Exists(@"C:\ProgramData\Chocolatey\bin\choco.exe"))
            {
                string path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
                if (!path.Contains(status.ExecutablePath.Replace("\\choco.exe", "\\bin")))
                {
                    Logger.Log("Adding chocolatey to path since it was not on path.");
                    Environment.SetEnvironmentVariable("PATH", $"{status.ExecutablePath.Replace("\\choco.exe", "\\bin")};{path}", EnvironmentVariableTarget.User);
                    Environment.SetEnvironmentVariable("chocolateyinstall", Path.GetDirectoryName(status.ExecutablePath), EnvironmentVariableTarget.User);
                }
            }
            Environment.SetEnvironmentVariable("chocolateyinstall", Path.GetDirectoryName(status.ExecutablePath), EnvironmentVariableTarget.Process);


            if (status.Found && IsEnabled())
                await RefreshPackageIndexes();

            return status;
        }

        protected override async Task<string[]> GetPackageVersions_Unsafe(Package package)
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " find -e " + package.Id + " -a",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            Logger.Log(p.StartInfo.FileName);
            Logger.Log(p.StartInfo.Arguments.ToString());

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
                    Logger.Log(line);
                    if (elements.Length < 2 || elements[0].Trim() != package.Id)
                        continue;

                    versions.Add(elements[1].Trim());
                }
            }
            output += await p.StandardError.ReadToEndAsync();
            // AppTools.LogManagerOperation(this, p, output);

            return versions.ToArray();
        }
    }
}
