using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading.Tasks;
using UniGetUI.Core.Data;
using UniGetUI.PackageEngine.Operations;

namespace UniGetUI.Core
{

    public class AppTools : IAppTools, IAppConfig
    {
        /// <summary>
        /// This should resolved by DI
        /// </summary>
        private readonly ILogger Logger = AppLogger.Instance;

        public class __tooltip_options
        {
            private int _errors_occurred = 0;
            public int ErrorsOccurred { get { return _errors_occurred; } set { _errors_occurred = value; AppTools.Instance.App.MainWindow.UpdateSystemTrayStatus(); } }
            private bool _restart_required = false;
            public bool RestartRequired { get { return _restart_required; } set { _restart_required = value; AppTools.Instance.App.MainWindow.UpdateSystemTrayStatus(); } }
            private int _operations_in_progress = 0;
            public int OperationsInProgress { get { return _operations_in_progress; } set { _operations_in_progress = value; AppTools.Instance.App.MainWindow.UpdateSystemTrayStatus(); } }
            private int _available_updates = 0;
            public int AvailableUpdates { get { return _available_updates; } set { _available_updates = value; AppTools.Instance.App.MainWindow.UpdateSystemTrayStatus(); } }
        }


        public MainApp App => (MainApp)Application.Current;

        public ThemeListener ThemeListener;
        public List<AbstractOperation> OperationQueue = new();

        public __tooltip_options TooltipStatus { get; private set;  } = new();

        private LanguageEngine LanguageEngine = new();

        string ApiAuthToken;

        /// <summary>
        /// This should only be accessed by DI or removed
        /// </summary>
        private static AppTools instance;
        public static AppTools Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AppTools();
                }
                return instance;
            }
        }

        /// <summary>
        /// This should only be accessed by DI or removed
        /// </summary>
        private AppTools()
        {
            ThemeListener = new ThemeListener();

            ApiAuthToken = RandomString(64);
            SetSettingsValue("CurrentSessionToken", ApiAuthToken);
            Logger.Log("Api auth token: " + ApiAuthToken);
        }

        private string RandomString(int length)
        {
            var random = new Random();
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
            var chars = Enumerable.Range(0, length)
                .Select(x => pool[random.Next(0, pool.Length)]);
            return new string(chars.ToArray());
        }

        public bool GetSettings(string setting, bool invert = false)
        { return AppTools.GetSettings_Static(setting, invert); }

        public static bool GetSettings_Static(string setting, bool invert = false)
        {
            return File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, setting)) ^ invert;
        }

        public void SetSettings(string setting, bool value)
        { AppTools.SetSettings_Static(setting, value); }

        public static void SetSettings_Static(string setting, bool value)
        {
            try { 
                if (value)
                {
                    if (!File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, setting)))
                        File.WriteAllText(Path.Join(CoreData.UniGetUIDataDirectory, setting), "");
                }
                else
                {
                    if (File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, setting)))
                        File.Delete(Path.Join(CoreData.UniGetUIDataDirectory, setting));
                }
            }
            catch (Exception e)
            {
                ((ILogger)Core.AppLogger.Instance).Log($"CRITICAL ERROR: CANNOT SET SETTING FOR setting={setting} enabled={value}: {e.Message}");
            }
        }
        public string GetSettingsValue(string setting)
        { return AppTools.GetSettingsValue_Static(setting); }

        public static string GetSettingsValue_Static(string setting)
        {
            if (!File.Exists(Path.Join(CoreData.UniGetUIDataDirectory, setting)))
                return "";
            return File.ReadAllText(Path.Join(CoreData.UniGetUIDataDirectory, setting));
        }
        public void SetSettingsValue(string setting, string value)
        { AppTools.SetSettingsValue_Static(setting, value); }

        public static void SetSettingsValue_Static(string setting, string value)
        {
            try
            {
                File.WriteAllText(Path.Join(CoreData.UniGetUIDataDirectory, setting), value);
            }
            catch (Exception e)
            {
                ((ILogger)Core.AppLogger.Instance).Log($"CRITICAL ERROR: CANNOT SET SETTING VALUE FOR setting={setting} value={value}: {e.Message}");
            }
        }

        /// <summary>
        /// Translate a string to the current language
        /// </summary>
        /// <param name="text">The string to translate</param>
        /// <returns>The translated string if available, the original string otherwise</returns>
        public string Translate(string text)
        {
            return LanguageEngine.Translate(text);
        }

        /// <summary>
        /// Dummy function to capture the strings that need to be translated but the translation is handled by a custom widget
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public string AutoTranslated(string text)
        {
            return text;
        }

        public void RestartApp()
        {
            Logger.Log(Environment.GetCommandLineArgs()[0].Replace(".dll", ".exe"));
            Process.Start(Environment.GetCommandLineArgs()[0].Replace(".dll", ".exe"));
            App.DisposeAndQuit();
        }

        public async Task<string> Which(string command)
        {
            Process process = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = "/C where " + command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string line = await process.StandardOutput.ReadLineAsync();
            string output;
            if (line == null)
                output = "";
            else
                output = line.Trim();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0 || output == "")
                return Path.Join(Environment.GetLogicalDrives()[0], "ThisExe\\WasNotFound\\InPath", command);
            else
                return output;
        }

        public string FormatAsName(string name)
        {
            name = name.Replace(".install", "").Replace(".portable", "").Replace("-", " ").Replace("_", " ").Split("/")[^1];
            string newName = "";
            for (int i = 0; i < name.Length; i++)
            {
                if (i == 0 || name[i - 1] == ' ')
                    newName += name[i].ToString().ToUpper();
                else
                    newName += name[i];
            }
            return newName;
        }

        public void AddOperationToList(AbstractOperation operation)
        {
            App.MainWindow.NavigationPage.OperationStackPanel.Children.Add(operation);
        }

        public static void ReportFatalException(Exception e)
        {
            string LangName = "Unknown";
            try
            {
                LangName = LanguageEngine.MainLangDict["langName"];
            }
            catch { }

            string Error_String = $@"
                        OS: {Environment.OSVersion.Platform}
                   Version: {Environment.OSVersion.VersionString}
           OS Architecture: {Environment.Is64BitOperatingSystem}
          APP Architecture: {Environment.Is64BitProcess}
                  Language: {LangName}
               APP Version: {CoreData.VersionName}
                Executable: {Environment.ProcessPath}

Crash Message: {e.Message}

Crash Traceback: 
{e.StackTrace}";

            Console.WriteLine(Error_String);


            string ErrorBody = "https://www.marticliment.com/error-report/?appName=UniGetUI^&errorBody=" + Uri.EscapeDataString(Error_String.Replace("\n", "{l}"));

            Console.WriteLine(ErrorBody);

            using System.Diagnostics.Process cmd = new();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = false;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            cmd.StandardInput.WriteLine("start " + ErrorBody);
            cmd.StandardInput.WriteLine("exit");
            cmd.WaitForExit();
            Environment.Exit(1);

        }

        public static async void LaunchBatchFile(string path, string WindowTitle = "", bool RunAsAdmin = false)
        {
            Process p = new();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = "/C start \"" + WindowTitle + "\" \"" + path + "\"";
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.Verb = RunAsAdmin ? "runas" : "";
            p.Start();
            await p.WaitForExitAsync();
        }

        public bool AuthenticateToken(string token)
        {
            return token == ApiAuthToken
                ;
        }

        /// <summary>
        /// Update UniGetUI
        /// </summary>
        /// <param name="round"></param>
        public async void UpdateUniGetUIIfPossible(int round = 0)
        {
            InfoBar? banner = null; ;
            try
            {
                Logger.Log("Starting update check");

                string fileContents = "";

                using (HttpClient client = new())
                    fileContents = await client.GetStringAsync("https://www.marticliment.com/versions/unigetui.ver");


                if (!fileContents.Contains("///"))
                    throw new FormatException("The updates file does not follow the FloatVersion///Sha256Hash format");

                float LatestVersion = float.Parse(fileContents.Split("///")[0].Replace("\n", "").Trim(), CultureInfo.InvariantCulture);
                string InstallerHash = fileContents.Split("///")[1].Replace("\n", "").Trim().ToLower();

                if (LatestVersion > CoreData.VersionNumber)
                {
                    Logger.Log("Updates found, downloading installer...");
                    Logger.Log("Current version: " + CoreData.VersionNumber.ToString(CultureInfo.InvariantCulture));
                    Logger.Log("Latest version : " + LatestVersion.ToString(CultureInfo.InvariantCulture));

                    banner = App.MainWindow.UpdatesBanner;
                    banner.Title= Translate("WingetUI version {0} is being downloaded.").Replace("{0}", LatestVersion.ToString(CultureInfo.InvariantCulture));
                    banner.Message = Translate("This may take a minute or two");
                    banner.Severity = InfoBarSeverity.Informational;
                    banner.IsOpen = true;
                    banner.IsClosable = false;

                    Uri DownloadUrl = new Uri("https://github.com/marticliment/WingetUI/releases/latest/download/UniGetUI.Installer.exe");
                    string InstallerPath = Path.Join(Directory.CreateTempSubdirectory().FullName, "unigetui-updater.exe");

                    using (HttpClient client = new())
                    {
                        var result = await client.GetAsync(DownloadUrl);
                        using (var fs = new FileStream(InstallerPath, FileMode.CreateNew))
                            await result.Content.CopyToAsync(fs);
                    }

                    string Hash = "";
                    SHA256 Sha256 = SHA256.Create();
                    using (FileStream stream = File.OpenRead(InstallerPath))
                    {
                        Hash = Convert.ToHexString(Sha256.ComputeHash(stream)).ToLower();
                    }

                    if (Hash == InstallerHash)
                    {

                        banner.Title = Translate("WingetUI {0} is ready to be installed.").Replace("{0}", LatestVersion.ToString(CultureInfo.InvariantCulture));
                        banner.Message = Translate("The update will be installed upon closing WingetUI");
                        banner.ActionButton = new Button();
                        banner.ActionButton.Content = Translate("Update now");
                        banner.ActionButton.Click += (sender, args) => { Instance.App.MainWindow.HideWindow(); };
                        banner.Severity = InfoBarSeverity.Success;
                        banner.IsOpen = true;
                        banner.IsClosable = true;

                        if (Instance.App.MainWindow.Visible)
                            Logger.Log("Waiting for mainWindow to be hidden");

                        while (Instance.App.MainWindow.Visible)
                            await Task.Delay(100);

                        Logger.Log("Hash ok, starting update");
                        Process p = new Process();
                        p.StartInfo.FileName = "cmd.exe";
                        p.StartInfo.Arguments = $"/c start /B \"\" \"{InstallerPath}\" /silent";
                        p.StartInfo.UseShellExecute = true;
                        p.StartInfo.CreateNoWindow = true;
                        p.Start();
                        Instance.App.DisposeAndQuit();
                    }
                    else
                    {
                        Logger.Log("Hash mismatch, not updating!");
                        Logger.Log("Current hash : " + Hash);
                        Logger.Log("Expected hash: " + InstallerHash);
                        File.Delete(InstallerPath);

                        banner.Title = Translate("The installer hash does not match the expected value.");
                        banner.Message = Translate("The update will not continue.");
                        banner.Severity = InfoBarSeverity.Error;
                        banner.IsOpen = true;
                        banner.IsClosable = true;

                        await Task.Delay(7200000); // Check again in 2 hours
                        UpdateUniGetUIIfPossible();
                    }
                }
                else
                {
                    Logger.Log("UniGetUI is up to date");
                    await Task.Delay(7200000); // Check again in 2 hours
                    UpdateUniGetUIIfPossible();
                }
            }
            catch (Exception e)
            {
                if(banner != null)
                {
                    banner.Title = Translate("An error occurred when checking for updates: ");
                    banner.Message = e.Message;
                    banner.Severity = InfoBarSeverity.Error;
                    banner.IsOpen = true;
                    banner.IsClosable = true;
                }

                Logger.Log(e);

                if (round >= 3)
                    return;

                await Task.Delay(600000); // Try again in 10 minutes
                UpdateUniGetUIIfPossible(round + 1);
            }
        }
        public bool IsAdministrator()
        {
            try
            {
                return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                          .IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception e)
            {
                Logger.Log(e);
                return false;
            }
        }

        /// <summary>
        /// Returns the size (in MB) of the file at the given URL
        /// </summary>
        /// <param name="url">a valid Uri object containing a URL to a file</param>
        /// <returns>a double representing the size in MBs, 0 if the process fails</returns>
        public async Task<double> GetFileSizeAsync(Uri url)
        {
            try
            {
#pragma warning disable SYSLIB0014 // Type or member is obsolete
                WebRequest req = WebRequest.Create(url);
#pragma warning restore SYSLIB0014 // Type or member is obsolete
                req.Method = "HEAD";
                WebResponse resp = await req.GetResponseAsync();
                long ContentLength;
                if (long.TryParse(resp.Headers.Get("Content-Length"), out ContentLength))
                {
                    return ContentLength / 1048576;
                }

            }
            catch (Exception e)
            {
                Logger.Log(e);
            }
            return 0;
        }
    }
}
