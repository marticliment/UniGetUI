using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Managers.Chocolatey;
using UniGetUI.PackageEngine.Managers.PowerShellManager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.ChocolateyManager
{
    public class Chocolatey : BaseNuGet
    {
        new public static string[] FALSE_PACKAGE_NAMES = [""];
        new public static string[] FALSE_PACKAGE_IDS = ["Directory", "", "Did", "Features?", "Validation", "-", "being", "It", "Error", "L'accs", "Maximum", "This", "Output is package name ", "operable", "Invalid"];
        new public static string[] FALSE_PACKAGE_VERSIONS = ["", "Did", "Features?", "Validation", "-", "being", "It", "Error", "L'accs", "Maximum", "This", "packages", "current version", "installed version", "is", "program", "validations", "argument", "no"];

        public Chocolatey() : base()
        {
            Capabilities = new ManagerCapabilities()
            {
                CanRunAsAdmin = true,
                CanSkipIntegrityChecks = true,
                CanRunInteractively = true,
                SupportsCustomVersions = true,
                SupportsCustomArchitectures = true,
                SupportedCustomArchitectures = new Architecture[] { Architecture.X86 },
                SupportsPreRelease = true,
                SupportsCustomSources = true,
                SupportsCustomPackageIcons = true,
                Sources = new ManagerSource.Capabilities()
                {
                    KnowsPackageCount = false,
                    KnowsUpdateDate = false,
                }
            };

            Properties = new ManagerProperties()
            {
                Name = "Chocolatey",
                Description = CoreTools.Translate("The classic package manager for windows. You'll find everything there. <br>Contains: <b>General Software</b>"),
                IconId = "choco",
                ColorIconId = "choco_color",
                ExecutableFriendlyName = "choco.exe",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "upgrade",
                ExecutableCallArgs = "",
                KnownSources = [new ManagerSource(this, "community", new Uri("https://community.chocolatey.org/api/v2/"))],
                DefaultSource = new ManagerSource(this, "community", new Uri("https://community.chocolatey.org/api/v2/")),

            };

            SourceProvider = new ChocolateySourceProvider(this);
            PackageDetailsProvider = new ChocolateyDetailsProvider(this);
        }

        protected override async Task<Package[]> GetAvailableUpdates_UnSafe()
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " outdated",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            ManagerClasses.Classes.ProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListUpdates, p);
            p.Start();

            string? line;
            List<Package> Packages = [];
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                if (!line.StartsWith("Chocolatey"))
                {
                    string[] elements = line.Split('|');
                    for (int i = 0; i < elements.Length; i++)
                    {
                        elements[i] = elements[i].Trim();
                    }

                    if (elements.Length <= 2)
                    {
                        continue;
                    }

                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]) || elements[1] == elements[2])
                    {
                        continue;
                    }

                    Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], elements[2], DefaultSource, this));
                }
            }

            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);

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
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            ManagerClasses.Classes.ProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.ListPackages, p);
            p.Start();

            string? line;
            List<Package> Packages = [];
            while ((line = await p.StandardOutput.ReadLineAsync()) != null)
            {
                logger.AddToStdOut(line);
                if (!line.StartsWith("Chocolatey"))
                {
                    string[] elements = line.Split(' ');
                    for (int i = 0; i < elements.Length; i++)
                    {
                        elements[i] = elements[i].Trim();
                    }

                    if (elements.Length <= 1)
                    {
                        continue;
                    }

                    if (FALSE_PACKAGE_IDS.Contains(elements[0]) || FALSE_PACKAGE_VERSIONS.Contains(elements[1]))
                    {
                        continue;
                    }

                    Packages.Add(new Package(CoreTools.FormatAsName(elements[0]), elements[0], elements[1], DefaultSource, this));
                }
            }

            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            await p.WaitForExitAsync();
            logger.Close(p.ExitCode);

            return Packages.ToArray();
        }
        public override OperationVeredict GetInstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            string output_string = string.Join("\n", Output);

            if (ReturnCode is 1641 or 0)
            {
                return OperationVeredict.Succeeded;
            }
            else if (ReturnCode == 3010)
            {
                return OperationVeredict.Succeeded; // TODO: Restart required
            }
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

            if (ReturnCode is 1641 or 1614 or 1605 or 0)
            {
                return OperationVeredict.Succeeded;
            }
            else if (ReturnCode == 3010)
            {
                return OperationVeredict.Succeeded; // TODO: Restart required
            }
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

            if (options.Architecture == Architecture.X86)
            {
                parameters.Add("--forcex86");
            }

            if (options.PreRelease)
            {
                parameters.Add("--prerelease");
            }

            if (options.SkipHashCheck)
            {
                parameters.AddRange(["--ignore-checksums", "--force"]);
            }

            if (options.Version != "")
            {
                parameters.AddRange([$"--version={options.Version}", "--allow-downgrade"]);
            }

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
            List<string> parameters = [Properties.UninstallVerb, package.Id, "-y"];

            if (options.CustomParameters is not null)
            {
                parameters.AddRange(options.CustomParameters);
            }

            if (options.InteractiveInstallation)
            {
                parameters.Add("--notsilent");
            }

            return parameters.ToArray();
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            ManagerStatus status = new();

            string old_choco_path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs\\WingetUI\\choco-cli");
            string new_choco_path = Path.Join(CoreData.UniGetUIDataDirectory, "Chocolatey");

            if (Directory.Exists(old_choco_path))
            {
                try
                {
                    Logger.Info("Moving Bundled Chocolatey from old path to new path...");

                    if(Path.GetRelativePath(Environment.GetEnvironmentVariable("chocolateyinstall", EnvironmentVariableTarget.User) ?? "", old_choco_path) == ".")
                    {
                        Logger.ImportantInfo("Migrating ChocolateyInstall environment variable to new location");
                        Environment.SetEnvironmentVariable("chocolateyinstall", new_choco_path, EnvironmentVariableTarget.User);
                    }

                    if (!Directory.Exists(new_choco_path))
                    {
                        Directory.CreateDirectory(new_choco_path);
                    }

                    foreach (string old_subdir in Directory.GetDirectories(old_choco_path, "*", SearchOption.AllDirectories))
                    {
                        string new_subdir = old_subdir.Replace(old_choco_path, new_choco_path);
                        if (!Directory.Exists(new_subdir))
                        {
                            Logger.Debug("New directory: " + new_subdir);
                            await Task.Run(() => Directory.CreateDirectory(new_subdir));
                        }
                        else
                        {
                            Logger.Debug("Directory " + new_subdir + " already exists");
                        }
                    }

                    foreach (string old_file in Directory.GetFiles(old_choco_path, "*", SearchOption.AllDirectories))
                    {
                        string new_file = old_file.Replace(old_choco_path, new_choco_path);
                        if (!File.Exists(new_file))
                        {
                            Logger.Info("Copying " + old_file);
                            await Task.Run(() => File.Move(old_file, new_file));
                        }
                        else
                        {
                            Logger.Debug("File " + new_file + " already exists.");
                            File.Delete(old_file);
                        }
                    }

                    foreach (string old_subdir in Directory.GetDirectories(old_choco_path, "*", SearchOption.AllDirectories).Reverse())
                    {
                        if (!Directory.EnumerateFiles(old_subdir).Any() && !Directory.EnumerateDirectories(old_subdir).Any())
                        {
                            Logger.Debug("Deleting old empty subdirectory " + old_subdir);
                            Directory.Delete(old_subdir);
                        }
                    }

                    if (!Directory.EnumerateFiles(old_choco_path).Any() && !Directory.EnumerateDirectories(old_choco_path).Any())
                    {
                        Logger.Info("Deleting old Chocolatey directory " + old_choco_path);
                        Directory.Delete(old_choco_path);
                    }


                }
                catch (Exception e)
                {
                    Logger.Error("An error occurred while migrating chocolatey");
                    Logger.Error(e);
                }
            }

            if (Settings.Get("UseSystemChocolatey"))
            {
                status.ExecutablePath = (await CoreTools.Which("choco.exe")).Item2;
            }
            else if (File.Exists(Path.Join(new_choco_path, "choco.exe")))
            {
                status.ExecutablePath = Path.Join(new_choco_path, "choco.exe");
            }
            else
            {
                status.ExecutablePath = Path.Join(CoreData.UniGetUIExecutableDirectory, "choco-cli\\choco.exe");
            }

            status.Found = File.Exists(status.ExecutablePath);

            if (!status.Found)
            {
                return status;
            }

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
                string? path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
                if (!path?.Contains(status.ExecutablePath.Replace("\\choco.exe", "\\bin")) ?? false)
                {
                    Logger.ImportantInfo("Adding chocolatey to path since it was not on path.");
                    Environment.SetEnvironmentVariable("PATH", $"{status.ExecutablePath.Replace("\\choco.exe", "\\bin")};{path}", EnvironmentVariableTarget.User);
                    Environment.SetEnvironmentVariable("chocolateyinstall", Path.GetDirectoryName(status.ExecutablePath), EnvironmentVariableTarget.User);
                }
            }

            // Trick chocolatey into using the wanted installation
            var choco_dir = Path.GetDirectoryName(status.ExecutablePath)?.Replace('/', '\\').Trim('\\') ?? "";
            if (choco_dir.EndsWith("bin"))
            {
                choco_dir = choco_dir[..^3].Trim('\\');
            }
            Environment.SetEnvironmentVariable("chocolateyinstall", choco_dir, EnvironmentVariableTarget.Process);

            return status;
        }
    }
}
