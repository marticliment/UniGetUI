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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UniGetUI.Core.Data;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Language;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Classes;
using UniGetUI.PackageEngine.Operations;

namespace UniGetUI.Core
{
    public class AppTools
    {
        public static IconStore IconDatabase;
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


        public MainApp App;

        public ThemeListener ThemeListener;
        public List<AbstractOperation> OperationQueue = new();

        public __tooltip_options TooltipStatus = new();

        private LanguageEngine LanguageEngine = new();

        private static AppTools instance;
        string ApiAuthToken;

        public static AppTools Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AppTools();
                    IconDatabase = new IconStore();
                }
                return instance;
            }
        }

        private AppTools()
        {
            App = (MainApp)Application.Current;
            ThemeListener = new ThemeListener();

            ApiAuthToken = RandomString(64);
            Settings.SetValue("CurrentSessionToken", ApiAuthToken);
            Logger.Log("Api auth token: " + ApiAuthToken);
        }

        private string RandomString(int length)
        {
            Random random = new();
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
            IEnumerable<char> chars = Enumerable.Range(0, length)
                .Select(x => pool[random.Next(0, pool.Length)]);
            return new string(chars.ToArray());
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
            System.Diagnostics.Process.Start(Environment.GetCommandLineArgs()[0].Replace(".dll", ".exe"));
            Environment.Exit(0);
        }

        public void AddOperationToList(AbstractOperation operation)
        {
            App.MainWindow.NavigationPage.OperationStackPanel.Children.Add(operation);
        }

        public static void LogManagerOperation(PackageManager manager, Process process, string output)
        {
            output = Regex.Replace(output, "\n.{0,6}\n", "\n");
            CoreData.ManagerLogs += $"\n▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄";
            CoreData.ManagerLogs += $"\n█▀▀▀▀▀▀▀▀▀ [{DateTime.Now}] {manager.Name} ▀▀▀▀▀▀▀▀▀▀▀";
            CoreData.ManagerLogs += $"\n█  Executable: {process.StartInfo.FileName}";
            CoreData.ManagerLogs += $"\n█  Arguments: {process.StartInfo.Arguments}";
            CoreData.ManagerLogs += "\n";
            CoreData.ManagerLogs += output;
            CoreData.ManagerLogs += "\n";
            CoreData.ManagerLogs += $"[{DateTime.Now}] Exit Code: {process.ExitCode}";
            CoreData.ManagerLogs += "\n";
            CoreData.ManagerLogs += "\n";
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
                    banner.Title = Translate("WingetUI version {0} is being downloaded.").Replace("{0}", LatestVersion.ToString(CultureInfo.InvariantCulture));
                    banner.Message = Translate("This may take a minute or two");
                    banner.Severity = InfoBarSeverity.Informational;
                    banner.IsOpen = true;
                    banner.IsClosable = false;

                    Uri DownloadUrl = new("https://github.com/marticliment/WingetUI/releases/latest/download/UniGetUI.Installer.exe");
                    string InstallerPath = Path.Join(Directory.CreateTempSubdirectory().FullName, "unigetui-updater.exe");

                    using (HttpClient client = new())
                    {
                        HttpResponseMessage result = await client.GetAsync(DownloadUrl);
                        using (FileStream fs = new(InstallerPath, FileMode.CreateNew))
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
                        Process p = new();
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
                if (banner != null)
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

        /// <summary>
        /// A path pointing to the gsudo executable bundled with the app
        /// </summary>
        public static string GSudoPath = Path.Join(CoreData.UniGetUIExecutableDirectory, "Assets", "Utilities", "gsudo.exe");


    }
}
