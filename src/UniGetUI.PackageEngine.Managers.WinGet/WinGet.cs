using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Manager.ManagerHelpers;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.ManagerClasses.Classes;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;
using Architecture = UniGetUI.PackageEngine.Enums.Architecture;

namespace UniGetUI.PackageEngine.Managers.WingetManager
{
    public class WinGet : PackageManager
    {
        public static new string[] FALSE_PACKAGE_NAMES = ["", "e(s)", "have", "the", "Id"];
        public static new string[] FALSE_PACKAGE_IDS = ["", "e(s)", "have", "an", "'winget", "pin'", "have", "an", "Version"];
        public static new string[] FALSE_PACKAGE_VERSIONS = ["", "have", "an", "'winget", "pin'", "have", "an", "Version"];
        public LocalWinGetSource LocalPcSource { get; }
        public LocalWinGetSource AndroidSubsystemSource { get; }
        public LocalWinGetSource SteamSource { get; }
        public LocalWinGetSource UbisoftConnectSource { get; }
        public LocalWinGetSource GOGSource { get; }
        public LocalWinGetSource MicrosoftStoreSource { get; }
        public static bool NO_PACKAGES_HAVE_BEEN_LOADED { get; private set; }

        public string WinGetBundledPath;

        public WinGet()
        {
            WinGetBundledPath = Path.Join(CoreData.UniGetUIExecutableDirectory, "winget-cli_x64", "winget.exe");

            Capabilities = new ManagerCapabilities
            {
                CanRunAsAdmin = true,
                CanSkipIntegrityChecks = true,
                CanRunInteractively = true,
                SupportsCustomVersions = true,
                CanDownloadInstaller = true,
                SupportsCustomArchitectures = true,
                SupportedCustomArchitectures = [Architecture.x86, Architecture.x64, Architecture.arm64],
                SupportsCustomScopes = true,
                SupportsCustomLocations = true,
                SupportsCustomSources = true,
                SupportsCustomPackageIcons = true,
                SupportsCustomPackageScreenshots = true,
                Sources = new SourceCapabilities
                {
                    KnowsPackageCount = false,
                    KnowsUpdateDate = true,
                    MustBeInstalledAsAdmin = true,
                },
                SupportsProxy = ProxySupport.Partially,
                SupportsProxyAuth = false
            };

            Properties = new ManagerProperties
            {
                Name = "Winget",
                DisplayName = "WinGet",
                Description = CoreTools.Translate("Microsoft's official package manager. Full of well-known and verified packages<br>Contains: <b>General Software, Microsoft Store apps</b>"),
                IconId = IconType.WinGet,
                ColorIconId = "winget_color",
                ExecutableFriendlyName = "winget.exe",
                InstallVerb = "install",
                UninstallVerb = "uninstall",
                UpdateVerb = "update",
                ExecutableCallArgs = "",
                KnownSources = [ new ManagerSource(this, "winget", new Uri("https://cdn.winget.microsoft.com/cache")),
                                 new ManagerSource(this, "msstore", new Uri("https://storeedgefd.dsx.mp.microsoft.com/v9.0")) ],
                DefaultSource = new ManagerSource(this, "winget", new Uri("https://cdn.winget.microsoft.com/cache"))
            };

            SourcesHelper = new WinGetSourceHelper(this);
            DetailsHelper = new WinGetPkgDetailsHelper(this);
            OperationHelper = new WinGetPkgOperationHelper(this);

            LocalPcSource = new LocalWinGetSource(this, CoreTools.Translate("Local PC"), IconType.LocalPc, LocalWinGetSource.Type_t.LocalPC);
            AndroidSubsystemSource = new(this, CoreTools.Translate("Android Subsystem"), IconType.Android, LocalWinGetSource.Type_t.Android);
            SteamSource = new(this, "Steam", IconType.Steam, LocalWinGetSource.Type_t.Steam);
            UbisoftConnectSource = new(this, "Ubisoft Connect", IconType.UPlay, LocalWinGetSource.Type_t.Ubisoft);
            GOGSource = new(this, "GOG", IconType.GOG, LocalWinGetSource.Type_t.GOG);
            MicrosoftStoreSource = new(this, "Microsoft Store", IconType.MsStore, LocalWinGetSource.Type_t.MicrosftStore);
        }

        public static string GetProxyArgument()
        {
            if (!Settings.Get(Settings.K.EnableProxy)) return "";
            var proxyUri = Settings.GetProxyUrl();
            if (proxyUri is null) return "";

            if (Settings.Get(Settings.K.EnableProxyAuth))
            {
                Logger.Warn("Proxy is enabled, but WinGet does not support proxy authentication, so the proxy setting will be ignored");
                return "";
            }
            return $"--proxy {proxyUri.ToString()[..^1]}";
        }

        protected override IReadOnlyList<Package> FindPackages_UnSafe(string query)
        {
            return WinGetHelper.Instance.FindPackages_UnSafe(query);
        }

        protected override IReadOnlyList<Package> GetAvailableUpdates_UnSafe()
        {
            return WinGetHelper.Instance.GetAvailableUpdates_UnSafe();
        }

        protected override IReadOnlyList<Package> GetInstalledPackages_UnSafe()
        {
            try
            {
                var packages = WinGetHelper.Instance.GetInstalledPackages_UnSafe();
                NO_PACKAGES_HAVE_BEEN_LOADED = !packages.Any();
                return packages;
            }
            catch (Exception)
            {
                NO_PACKAGES_HAVE_BEEN_LOADED = true;
                throw;
            }
        }

        public ManagerSource GetLocalSource(string id)
        {
            var IdPieces = id.Split('\\');
            if (IdPieces[0] == "MSIX")
            {
                return MicrosoftStoreSource;
            }

            string MeaningfulId = IdPieces[^1];

            // Fast Local PC Check
            if (MeaningfulId[0] == '{')
            {
                return LocalPcSource;
            }

            // Check if source is android
            if (MeaningfulId.Count(x => x == '.') >= 2 && MeaningfulId.All(c => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '.' || c == '…'))
            {
                return AndroidSubsystemSource;
            }

            // Check if source is Steam
            if (MeaningfulId == "Steam" || MeaningfulId.StartsWith("Steam App"))
            {
                return SteamSource;
            }

            // Check if source is Ubisoft Connect
            if (MeaningfulId == "Uplay" || MeaningfulId.StartsWith("Uplay Install"))
            {
                return UbisoftConnectSource;
            }

            // Check if source is GOG
            if (MeaningfulId.EndsWith("_is1") &&
                MeaningfulId.Replace("_is1", "").All(c => (c >= '0' && c <= '9')))
            {
                return GOGSource;
            }

            // Otherwise they are Local PC
            return LocalPcSource;
        }

        protected override ManagerStatus LoadManager()
        {
            ManagerStatus status = new();

            bool FORCE_BUNDLED = Settings.Get(Settings.K.ForceLegacyBundledWinGet);

            var (found, path) = CoreTools.Which("winget.exe");
            status.ExecutablePath = path;
            status.Found = found;

            if (!status.Found)
            {
                Logger.Error("User does not have WinGet installed, forcing bundled WinGet...");
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

            TryRepairTempFolderPermissions();

            Process process = new()
            {
                StartInfo = new ProcessStartInfo
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

            if (CoreTools.IsAdministrator())
            {
                string WinGetTemp = Path.Join(Path.GetTempPath(), "UniGetUI", "ElevatedWinGetTemp");
                process.StartInfo.Environment["TEMP"] = WinGetTemp;
                process.StartInfo.Environment["TMP"] = WinGetTemp;
            }

            process.Start();
            status.Version = $"{(FORCE_BUNDLED ? "Bundled" : "System")} WinGet CLI Version: {process.StandardOutput.ReadToEnd().Trim()}";
            string error = process.StandardError.ReadToEnd();
            if (error != "")
            {
                Logger.Error("WinGet STDERR not empty: " + error);
            }

            try
            {
                if (FORCE_BUNDLED)
                {
                    WinGetHelper.Instance = new BundledWinGetHelper(this);
                    status.Version += "\nUsing bundled WinGet helper (CLI parsing)";
                }
                else
                {
                    WinGetHelper.Instance = new NativeWinGetHelper(this);
                    status.Version += "\nUsing Native WinGet helper (COM Api)";
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Cannot instantiate {(FORCE_BUNDLED? "Bundled" : "Native")} WinGet Helper due to error: {ex.Message}");
                Logger.Warn(ex);
                Logger.Warn("WinGet will resort to using BundledWinGetHelper()");
                WinGetHelper.Instance = new BundledWinGetHelper(this);
                status.Version += "\nUsing bundled WinGet helper (CLI parsing, caused by exception)";
            }

            return status;
        }

        // For future usage
        private void ReRegisterCOMServer()
        {
            WinGetHelper.Instance = new NativeWinGetHelper(this);
            NativePackageHandler.Clear();
        }

        public override void AttemptFastRepair()
        {
            try
            {
                if (WinGetHelper.Instance is NativeWinGetHelper)
                {
                    Logger.ImportantInfo("Attempting to reconnect to WinGet COM Server...");
                    ReRegisterCOMServer();
                    TryRepairTempFolderPermissions();
                    NO_PACKAGES_HAVE_BEEN_LOADED = false;

                }
                else
                {
                    Logger.Warn("Attempted to reconnect to COM Server but Bundled WinGet is being used.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("An error ocurred while attempting to reconnect to COM Server");
                Logger.Error(ex);
            }
        }

        private static void TryRepairTempFolderPermissions()
        {
            if (Settings.Get(Settings.K.DisableNewWinGetTroubleshooter)) return;

            try
            {
                string tempPath = Path.GetTempPath();
                string winGetTempPath = Path.Combine(tempPath, "WinGet");

                if (!Directory.Exists(winGetTempPath))
                {
                    Logger.Warn("WinGet temp folder does not exist, creating it...");
                    Directory.CreateDirectory(winGetTempPath);
                }

                var directoryInfo = new DirectoryInfo(winGetTempPath);
                var accessControl = directoryInfo.GetAccessControl();
                var rules = accessControl.GetAccessRules(true, true, typeof(NTAccount));

                bool userHasAccess = false;
                string currentUser = WindowsIdentity.GetCurrent().Name;

                foreach (FileSystemAccessRule rule in rules)
                {
                    if (rule.IdentityReference.Value.Equals(currentUser, StringComparison.CurrentCultureIgnoreCase))
                    {
                        userHasAccess = true;
                        break;
                    }
                }

                if (!userHasAccess)
                {
                    Logger.Warn("WinGet temp folder does not have correct permissions set, adding the current user...");
                    var rule = new FileSystemAccessRule(
                        currentUser,
                        FileSystemRights.FullControl,
                        InheritanceFlags.ContainerInherit |
                        InheritanceFlags.ObjectInherit,
                        PropagationFlags.None,
                        AccessControlType.Allow);

                    accessControl.AddAccessRule(rule);
                    directoryInfo.SetAccessControl(accessControl);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred while attempting to properly configure WinGet's temp folder permissions.");
                Logger.Error(ex);
            }
        }

        public override void RefreshPackageIndexes()
        {
            using Process p = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Status.ExecutablePath,
                    Arguments = Properties.ExecutableCallArgs + " source update --disable-interactivity " + GetProxyArgument(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                }
            };

            IProcessTaskLogger logger = TaskLogger.CreateNew(LoggableTaskType.RefreshIndexes, p);

            if (CoreTools.IsAdministrator())
            {
                string WinGetTemp = Path.Join(Path.GetTempPath(), "UniGetUI", "ElevatedWinGetTemp");
                logger.AddToStdErr($"[WARN] Redirecting %TEMP% folder to {WinGetTemp}, since UniGetUI was run as admin");
                p.StartInfo.Environment["TEMP"] = WinGetTemp;
                p.StartInfo.Environment["TMP"] = WinGetTemp;
            }

            p.Start();
            logger.AddToStdOut(p.StandardOutput.ReadToEnd());
            logger.AddToStdErr(p.StandardError.ReadToEnd());
            logger.Close(p.ExitCode);
            p.WaitForExit();
            p.Close();
        }
    }

    public class LocalWinGetSource : ManagerSource
    {
        public enum Type_t
        {
            LocalPC,
            MicrosftStore,
            Steam,
            GOG,
            Android,
            Ubisoft
        }

        public readonly Type_t Type;
        private readonly string name;
        private readonly IconType __icon_id;
        public override IconType IconId { get => __icon_id; }

        public LocalWinGetSource(WinGet manager, string name, IconType iconId, Type_t type)
            : base(manager, name, new Uri("https://microsoft.com/local-pc-source"), isVirtualManager: true)
        {
            Type = type;
            this.name = name;
            __icon_id = iconId;
            AsString = Name;
            AsString_DisplayName = Name;
        }
    }
}
