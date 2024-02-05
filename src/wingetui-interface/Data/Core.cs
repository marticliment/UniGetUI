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
            get {
                if(!Directory.Exists(Path.GetDirectoryName(__ignored_updates_database_file)))
                    Directory.CreateDirectory(Path.GetDirectoryName(__ignored_updates_database_file));
                if (!File.Exists(__ignored_updates_database_file))
                    File.WriteAllText(__ignored_updates_database_file, "{}"); 
                return __ignored_updates_database_file;
            } 
        }    
    }
}
