namespace UniGetUI.Core.IconEngine.Tests
{
    [TestClass]
    public class IconStoreTests
    {
        [TestMethod]
        public void LoadIconsAndScreenshotsDatabaseTest()
        {
            // Serializable is implicitly tested when calling the method
            // LoadIconAndScreenshotsDatabase()
            
            var iconStore = new IconStore();
            iconStore.LoadIconAndScreenshotsDatabase().Wait();

            var icon = iconStore.GetIconUrlForId("__test_entry_DO_NOT_EDIT_PLEASE");
            Assert.AreEqual("https://this.is.a.test/url/used_for/automated_unit_testing.png", icon, "The icon url does not match");

            var screenshots = iconStore.GetScreenshotsUrlForId("__test_entry_DO_NOT_EDIT_PLEASE");
            Assert.AreEqual(3, screenshots.Length, "The amount of screenshots does not match the expected value");
            Assert.AreEqual("https://image_number.com/1.png", screenshots[0], "The screenshot does not match the expected value");
            Assert.AreEqual("https://image_number.com/2.png", screenshots[1], "The screenshot does not match the expected value");
            Assert.AreEqual("https://image_number.com/3.png", screenshots[2], "The screenshot does not match the expected value");

            var nonexistent_icon = iconStore.GetIconUrlForId("__test_entry_THIS_ICON_DOES_NOT_EXTST");
            Assert.AreEqual("", nonexistent_icon, "The icon url for a non-existing Id must be empty");

            var nonexistent_screenshots = iconStore.GetScreenshotsUrlForId("__test_entry_THIS_ICON_DOES_NOT_EXTST");
            Assert.AreEqual(0, nonexistent_screenshots.Length, "The amount of screenshots for a non-existent Id must be zero");
        }
    }
}