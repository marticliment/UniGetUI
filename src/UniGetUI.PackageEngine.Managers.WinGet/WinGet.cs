using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes.Manager.Classes;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.WingetManager
{
    public class WinGet : PackageManager
    {
        public static new string[] FALSE_PACKAGE_NAMES = ["", "e(s)", "have", "the", "Id"];
        public static new string[] FALSE_PACKAGE_IDS = ["", "e(s)", "have", "an", "'winget", "pin'", "have", "an", "Version"];
        public static new string[] FALSE_PACKAGE_VERSIONS = ["", "have", "an", "'winget", "pin'", "have", "an", "Version"];
        public LocalWingetSource LocalPcSource { get; set; }
        public LocalWingetSource AndroidSubsystemSource { get; set; }
        public LocalWingetSource SteamSource { get; set; }
        public LocalWingetSource UbisoftConnectSource { get; set; }
        public LocalWingetSource GOGSource { get; set; }
        public LocalWingetSource MicrosoftStoreSource { get; set; }

        public readonly string PowerShellPath;
        public readonly string PowerShellPromptArgs;
        public readonly string PowerShellInlineArgs;
        public string WinGetBundledPath;

        public WinGet() : base()
        {
            PowerShellPath = Path.Join(Environment.SystemDirectory, "windowspowershell\\v1.0\\powershell.exe");
            PowerShellPromptArgs = "-ExecutionPolicy Bypass -NoLogo -NoProfile";
            PowerShellInlineArgs = "-ExecutionPolicy Bypass -NoLogo -NoProfile -NonInteractive";

            WinGetBundledPath = Path.Join(CoreData.UniGetUIExecutableDirectory, "winget-cli_x64", "winget.exe");

            Dependencies = [
                new ManagerDependency(
                    "WinGet PowerShell Module",
                    PowerShellPath,
                    PowerShellPromptArgs + " -Command \"& {Install-Module -Name Microsoft.WinGet.Client -Force -Confirm:$false -Scope CurrentUser; if($error.count -ne 0){pause}}\"",
                    "Install-Module -Name Microsoft.WinGet.Client -Scope CurrentUser",
                    async () =>
                    {
                        if (!Settings.Get("ForceUsePowerShellModules") || Settings.Get("ForceLegacyBundledWinGet"))
                        {
                            Logger.ImportantInfo("Microsoft.Powershell.Client detection has been forcefully skipped as the module is not required on the current context");
                            return true;
                        }

                        Process p = new()
                        {
                            StartInfo = new ProcessStartInfo() {
                                FileName = PowerShellPath,
                                Arguments = PowerShellPromptArgs,
                                RedirectStandardInput = true,
                                CreateNoWindow = true
                            },
                        };
                        p.Start();
                        await p.StandardInput.WriteAsync("if(Get-Module -ListAvailable -Name \"Microsoft.WinGet.Client\"){exit 0}Else{exit 1}");
                        p.StandardInput.Close();
                        await p.WaitForExitAsync();
                        return p.ExitCode == 0;
                     })
            ];

            Capabilities = new ManagerCapabilities()
            {
                CanRunAsAdmin = true,
                CanSkipIntegrityChecks = true,
                CanRunInteractively = true,
                SupportsCustomVersions = true,
                SupportsCustomArchitectures = true,
                SupportedCustomArchitectures = [Architecture.X86, Architecture.X64, Architecture.Arm64],
                SupportsCustomScopes = true,
                SupportsCustomLocations = true,
                SupportsCustomSources = true,
                SupportsCustomPackageIcons = true,
                SupportsCustomPackageScreenshots = true,
                Sources = new ManagerSource.Capabilities()
                {
                    KnowsPackageCount = false,
                    KnowsUpdateDate = true,
                    MustBeInstalledAsAdmin = true,
                }
            };

            Properties = new ManagerProperties()
            {
                Name = "Winget",
                DisplayName = "WinGet",
                Description = CoreTools.Translate("Microsoft's official package manager. Full of well-known and verified packages<br>Contains: <b>General Software, Microsoft Store apps</b>"),
                IconId = "winget",
                ColorIconId = "winget_color",
                ExecutableFriendlyName = "winget.exe",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "update",
                ExecutableCallArgs = "",
                KnownSources = [ new(this, "winget", new Uri("https://cdn.winget.microsoft.com/cache")),
                                 new(this, "msstore", new Uri("https://storeedgefd.dsx.mp.microsoft.com/v9.0")) ],
                DefaultSource = new(this, "winget", new Uri("https://cdn.winget.microsoft.com/cache"))
            };

            SourceProvider = new WinGetSourceProvider(this);
            PackageDetailsProvider = new WinGetPackageDetailsProvider(this);

            LocalPcSource = new LocalWingetSource(this, CoreTools.Translate("Local PC"), "local_pc");
            AndroidSubsystemSource = new(this, CoreTools.Translate("Android Subsystem"), "android");
            SteamSource = new(this, "Steam", "steam");
            UbisoftConnectSource = new(this, "Ubisoft Connect", "uplay");
            GOGSource = new(this, "GOG", "gog");
            MicrosoftStoreSource = new(this, "Microsoft Store", "ms_store");
        }

        protected override async Task<Package[]> FindPackages_UnSafe(string query)
        {
            return await Task.Run(() => WinGetHelper.Instance.FindPackages_UnSafe(this, query).GetAwaiter().GetResult());
        }

        protected override async Task<Package[]> GetAvailableUpdates_UnSafe()
        {
            return await Task.Run(() => WinGetHelper.Instance.GetAvailableUpdates_UnSafe(this).GetAwaiter().GetResult());
        }

        protected override async Task<Package[]> GetInstalledPackages_UnSafe()
        {
            return await Task.Run(() => WinGetHelper.Instance.GetInstalledPackages_UnSafe(this).GetAwaiter().GetResult());
        }

        public ManagerSource GetLocalSource(string id)
        {
            try
            {
                // Check if source is android
                bool AndroidValid = true;
                foreach (char c in id)
                {
                    if (!"abcdefghijklmnopqrstuvwxyz.".Contains(c))
                    {
                        AndroidValid = false;
                        break;
                    }
                }

                if (AndroidValid && id.Count(x => x == '.') >= 2)
                {
                    return AndroidSubsystemSource;
                }

                // Check if source is Steam
                if ((id == "Steam" || id.Contains("Steam App ")) && id.Split("Steam App").Length >= 2 && !id.Split("Steam App")[1].Trim().Any(x => !"1234567890".Contains(x)))
                {
                    return SteamSource;
                }

                // Check if source is Ubisoft Connect
                if (id == "Uplay" || (id.Contains("Uplay Install ") && id.Split("Uplay Install").Length >= 2 && !id.Split("Uplay Install")[1].Trim().Any(x => !"1234567890".Contains(x))))
                {
                    return UbisoftConnectSource;
                }

                // Check if source is GOG
                if (id.EndsWith("_is1") && !id.Split("_is1")[0].Any(x => !"1234567890".Contains(x)))
                {
                    return GOGSource;
                }

                // Check if source is Microsoft Store
                if (id.Count(x => x == '_') == 1 && (id.Split('_')[^1].Length == 14 | id.Split('_')[^1].Length == 13))
                {
                    return MicrosoftStoreSource;
                }

                // Otherwise, Source is localpc
                return LocalPcSource;
            }
            catch (Exception ex)
            {
                Logger.Warn($"Could not parse local source for package {id}");
                Logger.Warn(ex);
                return LocalPcSource;
            }
        }

        public override string[] GetInstallParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = GetUninstallParameters(package, options).ToList();
            parameters[0] = Properties.InstallVerb;

            parameters.Add("--accept-package-agreements");

            if (options.SkipHashCheck)
            {
                parameters.Add("--ignore-security-hash");
            }

            if (options.CustomInstallLocation != "")
            {
                parameters.Add("--location"); parameters.Add($"\"{options.CustomInstallLocation}\"");
            }

            parameters.AddRange(options.Architecture switch
                {
                    Architecture.X86 => ["--architecture", "x86"],
                    Architecture.X64 => ["--architecture", "x64"],
                    Architecture.Arm64 => ["--architecture", "arm64"],
                    _ => []
                }
            );
            return parameters.ToArray();
        }
        public override string[] GetUpdateParameters(Package package, InstallationOptions options)
        {
            if (package.Name.Contains("64-bit") || package.Id.ToLower().Contains("x64"))
            {
                options.Architecture = Architecture.X64;
            }
            else if (package.Name.Contains("32-bit") || package.Id.ToLower().Contains("x86"))
            {
                options.Architecture = Architecture.X86;
            }

            string[] parameters = GetInstallParameters(package, options);
            parameters[0] = Properties.UpdateVerb;
            List<string> p = parameters.ToList();
            p.Add("--force");
            p.Add("--include-unknown");
            parameters = p.ToArray();
            return parameters;
        }

        public override string[] GetUninstallParameters(Package package, InstallationOptions options)
        {
            List<string> parameters = [Properties.UninstallVerb, "--id", $"\"{package.Id}\"", "--exact"];
            if (!package.Source.IsVirtualManager)
            {
                parameters.AddRange(["--source", package.Source.Name]);
            }

            parameters.Add("--accept-source-agreements");

            parameters.AddRange(options.InstallationScope switch
                {
                    PackageScope.Local => ["--scope", "user"],
                    PackageScope.Global => ["--scope", "machine"],
                    _ => []
                }
            );

            if (!package.IsUpgradable && options.Version != "")
            {
                parameters.AddRange(["--version", $"\"{options.Version}\"", "--force"]);
            }
            else if (!package.IsUpgradable && package.Version != "Unknown")
            {
                parameters.AddRange(["--version", $"\"{package.Version}\""]);
            }

            if (options.InteractiveInstallation)
            {
                parameters.Add("--interactive");
            }
            else
            {
                parameters.AddRange(["--silent", "--disable-interactivity"]);
            }

            parameters.AddRange(options.CustomParameters);

            return parameters.ToArray();
        }

        public override OperationVeredict GetInstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            string output_string = string.Join("\n", Output);

            if (ReturnCode == -1978334967) // Use https://www.rapidtables.com/convert/number/hex-to-decimal.html for easy UInt(hex) to Int(dec) conversion
            {
                return OperationVeredict.Succeeded; // TODO: Needs restart
            }

            if (ReturnCode == -1978335215)
            {
                return OperationVeredict.Failed; // TODO: Needs skip checksum
            }

            if (output_string.Contains("No applicable upgrade found") || output_string.Contains("No newer package versions are available from the configured sources"))
            {
                return OperationVeredict.Succeeded;
            }
            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        public override OperationVeredict GetUpdateOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            return GetInstallOperationVeredict(package, options, ReturnCode, Output);
        }

        public override OperationVeredict GetUninstallOperationVeredict(Package package, InstallationOptions options, int ReturnCode, string[] Output)
        {
            string output_string = string.Join("\n", Output);

            if (output_string.Contains("1603") || output_string.Contains("0x80070005") || output_string.Contains("Access is denied"))
            {
                options.RunAsAdministrator = true;
                return OperationVeredict.AutoRetry;
            }

            return ReturnCode == 0 ? OperationVeredict.Succeeded : OperationVeredict.Failed;
        }

        protected override async Task<ManagerStatus> LoadManager()
        {
            ManagerStatus status = new();

            bool FORCE_BUNDLED = Settings.Get("ForceLegacyBundledWinGet");

            Tuple<bool, string> which_res = await CoreTools.Which("winget.exe");
            status.ExecutablePath = which_res.Item2;
            status.Found = which_res.Item1;

            if (!status.Found)
            {
                Logger.Error("User does not have WinGet installed");
                FORCE_BUNDLED = true;
            }

            if (FORCE_BUNDLED)
            {
                status.ExecutablePath = WinGetBundledPath;
                status.Found = File.Exists(WinGetBundledPath);
            }

            if (!status.Found)
            {
                return status;
            }

            Process process = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " --version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };
            process.Start();
            status.Version = $"{(FORCE_BUNDLED ? "Bundled" : "System")} WinGet CLI Version: {(await process.StandardOutput.ReadToEndAsync()).Trim()}";
            string error = await process.StandardError.ReadToEndAsync();
            if (error != "")
            {
                Logger.Error("WinGet STDERR not empty: " + error);
            }

            process = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = PowerShellPath,
                    Arguments = PowerShellInlineArgs + " -Command Write-Output (Get-Module -Name Microsoft.WinGet.Client).Version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                }
            };
            process.Start();
            status.Version += $"\nMicrosoft.WinGet.Client PSModule version: \"{(await process.StandardOutput.ReadToEndAsync()).Trim()}\"";
            error = await process.StandardError.ReadToEndAsync();
            if (error != "")
            {
                Logger.Error("WinGet STDERR not empty: " + error);
            }

            try
            {
                if (FORCE_BUNDLED)
                {
                    throw new InvalidOperationException("Bundled WinGet was forced by the user!");
                }

                await Task.Run(() => WinGetHelper.Instance = new NativeWinGetHelper());
                status.Version += "\nUsing Native WinGet helper (COM Api)";
            }
            catch (Exception ex)
            {
                Logger.Warn($"Cannot create native WinGet instance due to error: {ex.Message}");
                Logger.Warn(ex);
                Logger.Warn("WinGet will resort to using BundledWinGetHelper()");
                WinGetHelper.Instance = new BundledWinGetHelper();
                status.Version += "\nUsing bundled WinGet helper (CLI parsing)";
            }

            return status;
        }

        public override async Task RefreshPackageIndexes()
        {
            Process p = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " source update --disable-interactivity",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                }
            };

            ManagerClasses.Classes.ProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.RefreshIndexes, p);

            p.Start();
            logger.AddToStdOut(await p.StandardOutput.ReadToEndAsync());
            logger.AddToStdErr(await p.StandardError.ReadToEndAsync());
            logger.Close(p.ExitCode);
            await p.WaitForExitAsync();
            p.Close();
        }
    }

    public class LocalWingetSource : ManagerSource
    {
        private readonly string name;
        private readonly string __icon_id;
        public override string IconId { get { return __icon_id; } }

        public LocalWingetSource(WinGet manager, string name, string iconId)
            : base(manager, name, new Uri("https://microsoft.com/local-pc-source"), isVirtualManager: true)
        {
            this.name = name;
            __icon_id = iconId;
            AsString = Name;
            AsString_DisplayName = Name;
        }
    }
}
