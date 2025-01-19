using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine;

public static class PackageBackupHelper
{
    public static async Task BackupPackages()
    {
        try
        {
            Logger.Debug("Starting package backup");
            List<IPackage> packagesToExport = [];
            foreach (IPackage package in PEInterface.InstalledPackagesLoader.Packages)
            {
                packagesToExport.Add(package);
            }

            string BackupContents = await PEInterface.PackageBundlesLoader.CreateBundle(packagesToExport.ToArray(), BundleFormatType.JSON);

            string dirName = Settings.GetValue("ChangeBackupOutputDirectory");
            if (dirName == "")
            {
                dirName = CoreData.UniGetUI_DefaultBackupDirectory;
            }

            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            string fileName = Settings.GetValue("ChangeBackupFileName");
            if (fileName == "")
            {
                fileName = CoreTools.Translate("{pcName} installed packages", new Dictionary<string, object?> { { "pcName", Environment.MachineName } });
            }

            if (Settings.Get("EnableBackupTimestamping"))
            {
                fileName += " " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
            }

            fileName += ".ubundle";

            string filePath = Path.Combine(dirName, fileName);
            await File.WriteAllTextAsync(filePath, BackupContents);
            Logger.ImportantInfo("Backup saved to " + filePath);
        }
        catch (Exception ex)
        {
            Logger.Error("An error occurred while performing a backup");
            Logger.Error(ex);
        }
    }
}
