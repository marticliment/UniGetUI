using System.Reflection.Metadata.Ecma335;
using UniGetUI.Core.Logging;

namespace UniGetUI.Core.Data
{
    public static class CoreData
    {
        public static string VersionName = "3.1.0-beta"; // Do not modify this line, use file scripts/apply_versions.py
        public static double VersionNumber = 3.09; // Do not modify this line, use file scripts/apply_versions.py

        /// <summary>
        /// The directory where all the user data is stored. The directory is automatically created if it does not exist.
        /// </summary>
        public static string UniGetUIDataDirectory
        {
            get
            {
                string old_path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".wingetui");
                string new_path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniGetUI");
                return GetNewDataDirectoryOrMoveOld(old_path, new_path);
            }
        }

        /// <summary>
        /// The directory where the installation options are stored. The directory is automatically created if it does not exist.
        /// </summary>
        public static string UniGetUIInstallationOptionsDirectory
        {
            get
            {
                var path = Path.Join(UniGetUIDataDirectory, "InstallationOptions");
                if(!Directory.Exists(path)) Directory.CreateDirectory(path);
                return path;
            }
        }

        /// <summary>
        /// The directory where the metadata cache is stored. The directory is automatically created if it does not exist.
        /// </summary>
        public static string UniGetUICacheDirectory_Data
        {
            get
            {
                string old_path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WingetUI", "CachedData");
                string new_path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniGetUI", "CachedMetadata");
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
                string old_path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WingetUI", "CachedIcons");
                string new_path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniGetUI", "CachedMedia");
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
                string old_dir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WingetUI", "CachedLangFiles");
                string new_dir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniGetUI", "CachedLanguageFiles");
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
                string old_dir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WingetUI");
                string new_dir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "UniGetUI");
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
                string file_path = Path.Join(UniGetUIDataDirectory, "IgnoredPackageUpdates.json");
                if (!File.Exists(file_path))
                    File.WriteAllText(file_path, "{}");
                return file_path;
            }
        }
        public static bool IsDaemon = false;

        public static string ManagerLogs = "";


        private static int __volatile_notification_id_counter = 1235;

        /// <summary>
        /// A self-incremented value to generate random notification IDs
        /// </summary>
        public static int VolatileNotificationIdCounter
        {
            get => __volatile_notification_id_counter++;
        }

        /// <summary>
        /// The ID of the notification that is used to inform the user that updates are available
        /// </summary>
        public static int UpdatesAvailableNotificationId
        { 
            get => 1234;
        }

        /// <summary>
        /// A path pointing to the location where the app is installed
        /// </summary>
        public static string UniGetUIExecutableDirectory
        {
            get => Directory.GetParent(Environment.ProcessPath)?.FullName;
        }

        /// <summary>
        /// A path pointing to the executable file of the app
        /// </summary>
        public static string UniGetUIExecutableFile
        {
            get => Environment.ProcessPath;
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
                    Logger.Log("WARNING: Cannot move old data directory to new location. Directory to move: " + old_path + ". Destination: " + new_path);
                    Logger.Log(e);
                    return old_path;
                }
            }
            else
            {
                Logger.Log("Creating non-existing data directory at: " + new_path);
                Directory.CreateDirectory(new_path);
                return new_path;
            }
        }

    }
}
