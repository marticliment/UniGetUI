namespace UniGetUI.Core.IconEngine
{
    internal struct IconScreenshotDatabase_v2
    {
        public struct PackageCount
        {
            public int total { get; set; }
            public int done { get; set; }
            public int packages_with_icon { get; set; }
            public int packages_with_screenshot { get; set; }
            public int total_screenshots { get; set; }
        }
        public struct PackageIconAndScreenshots
        {
            public string icon { get; set; }
            public List<string> images { get; set; }
        }

        public PackageCount package_count { get; set; }
        public Dictionary<string, PackageIconAndScreenshots> icons_and_screenshots { get; set; }
    }
}
