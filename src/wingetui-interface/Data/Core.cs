using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernWindow.Data
{
    public static class CoreData
    {
        public static string VersionName = "NeXT";
        public static double VersionNumber = 3.0;

        private static string __ignored_updates_database_file =  Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".wingetui", "IgnoredPackageUpdates.json");
        public static string IgnoredUpdatesDatabaseFile
        {
            get
            {
                if (!Directory.Exists(Path.GetDirectoryName(__ignored_updates_database_file)))
                    Directory.CreateDirectory(Path.GetDirectoryName(__ignored_updates_database_file));
                if (!File.Exists(__ignored_updates_database_file))
                    File.WriteAllText(__ignored_updates_database_file, "{}");
                return __ignored_updates_database_file;
            }
        }
        public static string WingetUIExecutableDirectory = Directory.GetParent(Environment.ProcessPath).FullName;

        public static string WingetUIDataDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".wingetui");
        public static string WingetUICacheDirectory_Data = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WingetUI\\CachedData");
        public static string WingetUICacheDirectory_Icons = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WingetUI\\CachedIcons");
        public static string WingetUICacheDirectory_Lang = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WingetUI\\CachedLangFiles");

        public static string DEFAULT_PACKAGE_BACKUP_DIR = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WingetUI");
    }
}
