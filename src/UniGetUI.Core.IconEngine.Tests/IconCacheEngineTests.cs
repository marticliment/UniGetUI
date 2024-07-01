using UniGetUI.Core.Data;

namespace UniGetUI.Core.IconEngine.Tests
{
    public static class IconCacheEngineTests
    {
        [Theory]
        [InlineData("https://marticliment.com/resources/wingetui.png", 
            new byte[]{ 0x24, 0x4e, 0x42, 0xb6, 0xbe, 0x44, 0x04, 0x66, 0xc8, 0x77, 0xf7, 0x68, 0x8a, 0xe0, 0xa9, 0x45, 0xfb, 0x2e, 0x66, 0x8c, 0x41, 0x84, 0x1f, 0x2d, 0x10, 0xcf, 0x92, 0xd4, 0x0d, 0x8c, 0xbb, 0xf6 }, 
            "TestManager", "Package1")]
        public static async Task TestCacheEngineForSha256(string url, byte[] data, string managerName, string packageId)
        {
            string extension = url.Split(".")[^1];
            string expectedFile = Path.Join(CoreData.UniGetUICacheDirectory_Icons, managerName, packageId + "." + extension);
            if(File.Exists(expectedFile))
            {
                File.Delete(expectedFile);
            }

            CacheableIcon icon = new(new Uri(url), data);
            string path = await IconCacheEngine.DownloadIconOrCache(icon, managerName, packageId);
            Assert.Equal(expectedFile, path);
            Assert.True(File.Exists(path));

            DateTime oldModificationDate = File.GetLastWriteTime(path);

            icon = new CacheableIcon(new Uri(url.Replace("icon", "nonexistingicon")), data);
            path = await IconCacheEngine.DownloadIconOrCache(icon, managerName, packageId);
            DateTime newModificationDate = File.GetLastWriteTime(path);

            Assert.Equal(oldModificationDate, newModificationDate);
            Assert.Equal(expectedFile, path);
            Assert.True(File.Exists(path));
        }

        [Theory]
        [InlineData("https://marticliment.com/resources/wingetui.png", "v3.01", "TestManager", "Package2")]
        public static async Task TestCacheEngineForPackageVersion(string url, string version, string managerName, string packageId)
        {
            string extension = url.Split(".")[^1];
            string expectedFile = Path.Join(CoreData.UniGetUICacheDirectory_Icons, managerName, packageId + "." + extension);
            if (File.Exists(expectedFile))
            {
                File.Delete(expectedFile);
            }

            CacheableIcon icon = new(new Uri(url), version);
            string path = await IconCacheEngine.DownloadIconOrCache(icon, managerName, packageId);
            Assert.Equal(expectedFile, path);
            Assert.True(File.Exists(path));

            DateTime oldModificationDate = File.GetLastWriteTime(path);

            icon = new CacheableIcon(new Uri(url), version);
            path = await IconCacheEngine.DownloadIconOrCache(icon, managerName, packageId);
            DateTime newModificationDate = File.GetLastWriteTime(path);

            Assert.Equal(oldModificationDate, newModificationDate);
            Assert.Equal(expectedFile, path);
            Assert.True(File.Exists(path));

            icon = new CacheableIcon(new Uri(url), version+"-beta0");
            path = await IconCacheEngine.DownloadIconOrCache(icon, managerName, packageId);
            DateTime newNewModificationDate = File.GetLastWriteTime(path);

            Assert.NotEqual(oldModificationDate, newNewModificationDate);
            Assert.Equal(expectedFile, path);
            Assert.True(File.Exists(path));
        }


        [Theory]
        [InlineData("https://marticliment.com/resources/wingetui.png", 47903, "TestManager", "Package3")]
        public static async Task TestCacheEngineForPackageSize(string url, long size, string managerName, string packageId)
        {
            string extension = url.Split(".")[^1];
            string expectedFile = Path.Join(CoreData.UniGetUICacheDirectory_Icons, managerName, packageId + "." + extension);
            if (File.Exists(expectedFile))
            {
                File.Delete(expectedFile);
            }

            CacheableIcon icon = new(new Uri(url), size);
            string path = await IconCacheEngine.DownloadIconOrCache(icon, managerName, packageId);
            Assert.Equal(expectedFile, path);
            Assert.True(File.Exists(path));

            DateTime oldModificationDate = File.GetLastWriteTime(path);

            icon = new CacheableIcon(new Uri(url), size);
            path = await IconCacheEngine.DownloadIconOrCache(icon, managerName, packageId);
            DateTime newModificationDate = File.GetLastWriteTime(path);

            Assert.Equal(oldModificationDate, newModificationDate);
            Assert.Equal(expectedFile, path);
            Assert.True(File.Exists(path));

            icon = new CacheableIcon(new Uri(url), size+1);
            path = await IconCacheEngine.DownloadIconOrCache(icon, managerName, packageId);
            DateTime newNewModificationDate = File.GetLastWriteTime(path);

            Assert.NotEqual(oldModificationDate, newNewModificationDate);
            Assert.Equal(expectedFile, path);
            Assert.True(File.Exists(path));
        }
    }
}
