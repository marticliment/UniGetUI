using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniGetUI.Core.Data.Tests
{
    [TestClass]
    public class CoreTests
    {

        [TestMethod]
        public void CheckDirectoryAttributes()
        {
            foreach (var directory in new string[] {
                                        CoreData.UniGetUIDataDirectory,
                                        CoreData.UniGetUIInstallationOptionsDirectory,
                                        CoreData.UniGetUICacheDirectory_Data,
                                        CoreData.UniGetUICacheDirectory_Icons,
                                        CoreData.UniGetUICacheDirectory_Lang,
                                        CoreData.UniGetUI_DefaultBackupDirectory})
            {
                Assert.IsTrue(Directory.Exists(directory), $"Directory ${directory} does not exist, but it should have been created automatically");
            }
        }

        [TestMethod]
        public void CheckOtherAttributes()
        {
            Assert.AreNotEqual(CoreData.VersionName, "", "Version Name must not be empty");
            Assert.AreNotEqual(CoreData.VersionNumber, 0, "Version number must be different from 0");
            Assert.IsTrue(File.Exists(CoreData.IgnoredUpdatesDatabaseFile), "The Ignored Updates database file does not exist, but it should have been created automatically.");
            var notif_1 = CoreData.VolatileNotificationIdCounter;
            var notif_2 = CoreData.VolatileNotificationIdCounter;
            Assert.AreNotEqual(notif_1, notif_2, "The VolatileNotificationIdCounter attribute returned the same value when it should not");
            
            var notif_3 = CoreData.UpdatesAvailableNotificationId;
            var notif_4 = CoreData.UpdatesAvailableNotificationId;
            Assert.AreEqual(notif_3, notif_4, "The UpdatesAvailableNotificationId must be always the same");
            Assert.AreNotEqual(CoreData.UpdatesAvailableNotificationId, 0, "The UpdatesAvailableNotificationId must not be zero");

            Assert.IsTrue(Directory.Exists(CoreData.UniGetUIExecutableDirectory), "Directory where the executable is located does not exist");
            Assert.IsTrue(File.Exists(CoreData.UniGetUIExecutableFile), "The executable file does not exist");
        }
    }
}
