using System.Text.Json;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;

namespace UniGetUI.Core.IconEngine
{
    /// <summary>
    /// This class represents the structure of the icon and screenshot database. It is used to deserialize the JSON data.
    /// </summary>
    public class IconDatabase
    {
        public struct IconCount
        {
            public int PackagesWithIconCount = 0;
            public int TotalScreenshotCount = 0;
            public int PackagesWithScreenshotCount = 0;
            public IconCount() { }
        }

        private static IconDatabase? __instance;

        public static IconDatabase Instance
        {
            get
            {
                if (__instance is null)
                {
                    Logger.Error("IconStore.Instance was not initialized, creating an empty instance.");
                    InitializeInstance();
                    return Instance;
                }
                return __instance;
            }
        }

        public static void InitializeInstance()
        {
            __instance = new();
        }

        /// <summary>
        /// The icon and screenshot database
        /// </summary>
        private Dictionary<string, IconScreenshotDatabase_v2.PackageIconAndScreenshots> IconDatabaseData = [];
        private IconCount __icon_count = new();

        /// <summary>
        /// Download the icon and screenshots database to a local file, and load it into memory
        /// </summary>
        public async void LoadIconAndScreenshotsDatabase()
        {
            await LoadIconAndScreenshotsDatabaseAsync();
        }

        public async Task LoadIconAndScreenshotsDatabaseAsync()
        {
            string IconsAndScreenshotsFile = Path.Join(CoreData.UniGetUICacheDirectory_Data, "Icon Database.json");
            try
            {
                Uri DownloadUrl = new("https://raw.githubusercontent.com/marticliment/WingetUI/main/WebBasedData/screenshot-database-v2.json");
                if (Settings.Get("IconDataBaseURL"))
                {
                    DownloadUrl = new Uri(Settings.GetValue("IconDataBaseURL"));
                }

                using (HttpClient client = new(CoreData.GenericHttpClientParameters))
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(CoreData.UserAgentString);
                    string fileContents = await client.GetStringAsync(DownloadUrl);
                    await File.WriteAllTextAsync(IconsAndScreenshotsFile, fileContents);
                }

                Logger.ImportantInfo("Downloaded new icons and screenshots successfully!");

            }
            catch (Exception e)
            {
                Logger.Warn("Failed to download icons and screenshots");
                Logger.Warn(e);
            }

            if (!File.Exists(IconsAndScreenshotsFile))
            {
                Logger.Error("Icon Database file not found");
                return;
            }

            try
            {
                IconScreenshotDatabase_v2 JsonData = JsonSerializer.Deserialize<IconScreenshotDatabase_v2>(
                    await File.ReadAllTextAsync(IconsAndScreenshotsFile),
                    CoreData.SerializingOptions
                    );
                if (JsonData.icons_and_screenshots is not null)
                {
                    IconDatabaseData = JsonData.icons_and_screenshots;
                }

                __icon_count = new IconCount
                {
                    PackagesWithIconCount = JsonData.package_count.packages_with_icon,
                    PackagesWithScreenshotCount = JsonData.package_count.packages_with_screenshot,
                    TotalScreenshotCount = JsonData.package_count.total_screenshots,
                };
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load icon and screenshot database");
                Logger.Error(ex);
            }
        }

        public string GetIconUrlForId(string id)
        {
            return IconDatabaseData.TryGetValue(id, out var value) ? value.icon : "";
        }

        public string[] GetScreenshotsUrlForId(string id)
        {
            return IconDatabaseData.TryGetValue(id, out var value) ? value.images.ToArray() : [];
        }

        public IconCount GetIconCount()
        {
            return __icon_count;
        }

    }
}
