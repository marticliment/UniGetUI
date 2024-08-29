namespace UniGetUI.Core.Data.Tests
{
    public class CoreTests
    {
        public static object[][] Data =>
            [
                [CoreData.UniGetUIDataDirectory],
                [CoreData.UniGetUIInstallationOptionsDirectory ],
                [CoreData.UniGetUICacheDirectory_Data ],
                [CoreData.UniGetUICacheDirectory_Icons ],
                [CoreData.UniGetUICacheDirectory_Lang ],
                [CoreData.UniGetUI_DefaultBackupDirectory ]
             ];

        [Theory]
        [MemberData(nameof(Data))]
        public void CheckDirectoryAttributes(string directory)
        {
            Assert.True(Directory.Exists(directory), $"Directory ${directory} does not exist, but it should have been created automatically");
        }

        [Fact]
        public void CheckOtherAttributes()
        {
            Assert.NotEmpty(CoreData.VersionName);
            Assert.NotEqual(0, CoreData.VersionNumber);
            Assert.True(File.Exists(CoreData.IgnoredUpdatesDatabaseFile), "The Ignored Updates database file does not exist, but it should have been created automatically.");

            int notif_3 = CoreData.UpdatesAvailableNotificationTag;
            int notif_4 = CoreData.UpdatesAvailableNotificationTag;
            Assert.True(notif_3 == notif_4, "The UpdatesAvailableNotificationId must be always the same");
            Assert.NotEqual(0, CoreData.UpdatesAvailableNotificationTag);

            Assert.True(Directory.Exists(CoreData.UniGetUIExecutableDirectory), "Directory where the executable is located does not exist");
            Assert.True(File.Exists(CoreData.UniGetUIExecutableFile), "The executable file does not exist");
        }
    }
}
