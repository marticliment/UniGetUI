using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;

namespace UniGetUI.Core.Data
{
    public static class CoreData
    {

        private static ILogger AppLogger => Core.AppLogger.Instance;

        public static string VersionName =  "3.1.0-beta"; // Do not modify this line, use file scripts/apply_versions.py
        public static double VersionNumber =  3.09; // Do not modify this line, use file scripts/apply_versions.py

        /// <summary>
        /// The directory where all the user data is stored. The directory is automatically created if it does not exist.
        /// </summary>
        public static string UniGetUIDataDirectory
        {
            get
            {
                var old_path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".wingetui");
                var new_path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniGetUI");
                return GetNewDataDirectoryOrMoveOld(old_path, new_path);
            }
        }

        /// <summary>
        /// The directory where the installation options are stored. The directory is automatically created if it does not exist.
        /// </summary>
        public static string UniGetUIInstallationOptionsDirectory
        {
            get => Path.Join(UniGetUIDataDirectory, "InstallationOptions");
        }

        /// <summary>
        /// The directory where the metadata cache is stored. The directory is automatically created if it does not exist.
        /// </summary>
        public static string UniGetUICacheDirectory_Data
        {
            get
            {
                var old_path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WingetUI", "CachedData");
                var new_path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniGetUI", "CachedMetadata");
                return GetNewDataDirectoryOrMoveOld(old_path, new_path);
            }
        }

        /// <summary>
        /// The directory where the cached icons and screenshots are saved. The directory is automatically created if it does not exist.
        /// </summary>
        public static string UniGetUICacheDirectory_Icons
        {
            get
            {
                var old_path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WingetUI", "CachedIcons");
                var new_path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniGetUI", "CachedMedia");
                return GetNewDataDirectoryOrMoveOld(old_path, new_path);
            }
        }

        /// <summary>
        /// The directory where the cached language files are stored. The directory is automatically created if it does not exist.
        /// </summary>
        public static string UniGetUICacheDirectory_Lang
        {
            get
            {
                var old_dir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WingetUI", "CachedLangFiles");
                var new_dir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniGetUI", "CachedLanguageFiles");
                return GetNewDataDirectoryOrMoveOld(old_dir, new_dir);
            }
        }

        /// <summary>
        /// The directory where package backups will be saved by default.
        /// </summary>
        public static string UniGetUI_DefaultBackupDirectory
        {
            get
            {
                var old_dir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WingetUI");
                var new_dir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "UniGetUI");
                return GetNewDataDirectoryOrMoveOld(old_dir, new_dir);
            }
        }

        /// <summary>
        /// The file where the screenshot metadata is stored. If the file does not exist, it will be created automatically.
        /// </summary>
        public static string IgnoredUpdatesDatabaseFile
        {
            get
            {
                // Calling the UniGetUIDataDirectory will create the directory if it does not exist
                var file_path = Path.Join(UniGetUIDataDirectory, "IgnoredPackageUpdates.json");
                if (!File.Exists(file_path))
                    File.WriteAllText(file_path, "{}");
                return file_path;
            }
        }
        public static bool IsDaemon = false;

        public static string UniGetUILog = "";
        public static string ManagerLogs = "";


        private static int __volatile_notification_id_counter = 1235;
        
        /// <summary>
        /// A self-incremented value to generate random notification IDs
        /// </summary>
        public static int VolatileNotificationIdCounter { 
            get => __volatile_notification_id_counter++; 
        }
        
        /// <summary>
        /// The ID of the notification that is used to inform the user that updates are available
        /// </summary>
        public static int UpdatesAvailableNotificationId = 1234;

        /// <summary>
        /// A path pointing to the location where the app is installed
        /// </summary>
        public static string UniGetUIExecutableDirectory = Directory.GetParent(Environment.ProcessPath).FullName;
        
        /// <summary>
        /// A path pointing to the executable file of the app
        /// </summary>
        public static string UniGetUIExecutableFile = Environment.ProcessPath;
        
        /// <summary>
        /// A path pointing to the gsudo executable bundled with the app
        /// </summary>
        public static string GSudoPath = Path.Join(UniGetUIExecutableDirectory, "Assets", "Utilities", "gsudo.exe");

        /// <summary>
        /// The icon and screensho database
        /// </summary>
        public static Dictionary<string, IconScreenshotDatabase_v2.PackageIconAndScreenshots> IconDatabaseData = new();

        /// <summary>
        /// Tis class represents the structure of the icon and screenshot database. It is used to deserialize the JSON data.
        /// </summary>
        public class IconScreenshotDatabase_v2
        {
            public class PackageCount
            {
                public int total { get; set; }
                public int done { get; set; }
                public int packages_with_icon { get; set; }
                public int packages_with_screenshot { get; set; }
                public int total_screenshots { get; set; }
            }
            public class PackageIconAndScreenshots
            {
                public string icon { get; set; }
                public List<string> images { get; set; }
            }

            public PackageCount package_count { get; set; }
            public Dictionary<string, PackageIconAndScreenshots> icons_and_screenshots { get; set; }
        }

        /// <summary>
        /// Download the icon and screenshots database to a local file, and load it into memory
        /// </summary>
        /// <returns></returns>
        public static async Task LoadIconAndScreenshotsDatabase()
        {
            string IconsAndScreenshotsFile = Path.Join(UniGetUICacheDirectory_Data, "Icon Database.json");

            try
            {
                Uri DownloadUrl = new("https://raw.githubusercontent.com/marticliment/WingetUI/main/WebBasedData/screenshot-database-v2.json");
                if (AppTools.GetSettings_Static("IconDataBaseURL"))
                    DownloadUrl = new Uri(AppTools.GetSettingsValue_Static("IconDataBaseURL"));

                using (HttpClient client = new())
                {
                    string fileContents = await client.GetStringAsync(DownloadUrl);
                    await File.WriteAllTextAsync(IconsAndScreenshotsFile, fileContents);
                }

                AppLogger.Log("Downloaded icons and screenshots successfully!");

            }
            catch (Exception e)
            {
                AppLogger.Log("Failed to download icons and screenshots");
                AppLogger.Log(e);
            }


            if (!File.Exists(IconsAndScreenshotsFile))
            {
                AppLogger.Log("WARNING: Icon Database file not found");
                return;
            }

            try
            {
                IconScreenshotDatabase_v2 JsonData = JsonSerializer.Deserialize<IconScreenshotDatabase_v2>(await File.ReadAllTextAsync(IconsAndScreenshotsFile));
                if (JsonData.icons_and_screenshots != null)
                    IconDatabaseData = JsonData.icons_and_screenshots;
            }
            catch (Exception ex)
            {
                AppLogger.Log("Failed to load icon database");
                AppLogger.Log(ex);
            }
        }

        /// <summary>
        /// This method will return the most appropriate data directory.
        /// If the new directory exists, it will be used.
        /// If the new directory does not exist, but the old directory does, it will be moved to the new location, and the new location will be used.
        /// If none exist, the new directory will be created.
        /// </summary>
        /// <param name="old_path">The old/legacy directory</param>
        /// <param name="new_path">The new directory</param>
        /// <returns>The path to an existing, valid directory</returns>
        private static string GetNewDataDirectoryOrMoveOld(string old_path, string new_path)
        {
            if (Directory.Exists(new_path))
                return new_path;
            else if (Directory.Exists(old_path))
            {
                try
                {
                    Directory.Move(old_path, new_path);
                    return new_path;
                }
                catch (Exception e)
                {
                    AppLogger.Log("WARNING: Cannot move old data directory to new location. Directory to move: " + old_path + ". Destination: " + new_path);
                    AppLogger.Log(e);
                    return old_path;
                }
            }
            else
            {
                AppLogger.Log("Creating non-existing data directory at: " + new_path);
                Directory.CreateDirectory(new_path);
                return new_path;
            }
        }

    }
}
