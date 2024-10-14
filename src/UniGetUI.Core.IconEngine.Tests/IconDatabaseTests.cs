namespace UniGetUI.Core.IconEngine.Tests
{
    public class IconDatabaseTests
    {
        private readonly IconDatabase iconStore = new();

        [Fact]
        public async Task LoadIconsAndScreenshotsDatabaseTest()
        {
            // Serializable is implicitly tested when calling the method
            // LoadIconAndScreenshotsDatabase()

            await iconStore.LoadIconAndScreenshotsDatabaseAsync();

            IconDatabase.IconCount iconCount = iconStore.GetIconCount();
            Assert.NotEqual(0, iconCount.PackagesWithIconCount);
            Assert.NotEqual(0, iconCount.PackagesWithScreenshotCount);
            Assert.NotEqual(0, iconCount.TotalScreenshotCount);
        }

        [Fact]
        public async Task TestGetExistingIconAndImagesAsync()
        {
            await iconStore.LoadIconAndScreenshotsDatabaseAsync();

            string? icon = iconStore.GetIconUrlForId("__test_entry_DO_NOT_EDIT_PLEASE");
            Assert.Equal("https://this.is.a.test/url/used_for/automated_unit_testing.png", icon);

            string[] screenshots = iconStore.GetScreenshotsUrlForId("__test_entry_DO_NOT_EDIT_PLEASE");
            Assert.Equal(3, screenshots.Length);
            Assert.Equal("https://image_number.com/1.png", screenshots[0]);
            Assert.Equal("https://image_number.com/2.png", screenshots[1]);
            Assert.Equal("https://image_number.com/3.png", screenshots[2]);
        }

        [Fact]
        public async Task TestGetNonExistingIconAndImagesAsync()
        {
            await iconStore.LoadIconAndScreenshotsDatabaseAsync();

            string? nonexistent_icon = iconStore.GetIconUrlForId("__test_entry_THIS_ICON_DOES_NOT_EXTST");
            Assert.Null(nonexistent_icon);

            string[] nonexistent_screenshots = iconStore.GetScreenshotsUrlForId("__test_entry_THIS_ICON_DOES_NOT_EXTST");
            Assert.Empty(nonexistent_screenshots);
        }
    }
}
