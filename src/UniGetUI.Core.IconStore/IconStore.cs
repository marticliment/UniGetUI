using System.Text.Json;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;

namespace UniGetUI.Core.IconEngine
{
    public class IconStore
    {
        public struct IconCount
        {
            public int PackagesWithIconCount = 0;
            public int TotalScreenshotCount = 0;
            public int PackagesWithScreenshotCount = 0;
            public IconCount() { }
        }

        /// <summary>
        /// The icon and screenshot database
        /// </summary>
        private Dictionary<string, IconScreenshotDatabase_v2.PackageIconAndScreenshots> IconDatabaseData = new();
        private IconCount __icon_count = new();

        /// <summary>
        /// Tis class represents the structure of the icon and screenshot database. It is used to deserialize the JSON data.
        /// </summary>


        /// <summary>
        /// Download the icon and screenshots database to a local file, and load it into memory
        /// </summary>
        /// <returns></returns>

        public async Task LoadIconAndScreenshotsDatabase()
        {
            string IconsAndScreenshotsFile = Path.Join(CoreData.UniGetUICacheDirectory_Data, "Icon Database.json");
            try
            {
                Uri DownloadUrl = new("https://raw.githubusercontent.com/marticliment/WingetUI/main/WebBasedData/screenshot-database-v2.json");
                if (Settings.Get("IconDataBaseURL"))
                    DownloadUrl = new Uri(Settings.GetValue("IconDataBaseURL"));

                using (HttpClient client = new())
                {
                    string fileContents = await client.GetStringAsync(DownloadUrl);
                    await File.WriteAllTextAsync(IconsAndScreenshotsFile, fileContents);
                }

                Logger.Log("Downloaded icons and screenshots successfully!");

            }
            catch (Exception e)
            {
                Logger.Log("Failed to download icons and screenshots");
                Logger.Log(e);
            }


            if (!File.Exists(IconsAndScreenshotsFile))
            {
                Logger.Log("WARNING: Icon Database file not found");
                return;
            }

            try
            {
                IconScreenshotDatabase_v2 JsonData = JsonSerializer.Deserialize<IconScreenshotDatabase_v2>(await File.ReadAllTextAsync(IconsAndScreenshotsFile));
                if (JsonData.icons_and_screenshots != null)
                    IconDatabaseData = JsonData.icons_and_screenshots;
                __icon_count = new IconCount()
                    {
                     PackagesWithIconCount = JsonData.package_count.packages_with_icon,
                     PackagesWithScreenshotCount = JsonData.package_count.packages_with_screenshot,
                     TotalScreenshotCount = JsonData.package_count.total_screenshots,
                };
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to load icon database");
                Logger.Log(ex);
            }
        }

        public string GetIconUrlForId(string id)
        {
            return IconDatabaseData.ContainsKey(id) ? IconDatabaseData[id].icon : "";
        }

        public string[] GetScreenshotsUrlForId(string id)
        {
            return IconDatabaseData.ContainsKey(id) ? IconDatabaseData[id].images.ToArray() : [];
        }
        
        public IconCount GetIconCount()
        {
            return __icon_count;
        }

    }
}
