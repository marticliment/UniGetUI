using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniGetUI.Core.Classes.Tests
{
    [TestClass]
    public class PersonTests
    {
        [TestMethod]
        public void TestPerson()
        {
            Person p1 = new Person("Bernat-Miquel Guimerà", new Uri("https://github.com/BernatMiquelG.png"), new Uri("https://github.com/BernatMiquelG"));
            Person p2 = new Person("Bernat-Miquel Guimerà", ProfilePicture: new Uri("https://github.com/BernatMiquelG.png"), null);
            Person p3 = new Person("Bernat-Miquel Guimerà", GitHubUrl: new Uri("https://github.com/BernatMiquelG"));
            Person p4 = new Person("Bernat-Miquel Guimerà");

            Assert.IsTrue(p1.HasGitHubProfile, "Invalid automatically generated field for p1");
            Assert.IsTrue(p1.HasPicture, "Invalid automatically generated field for p1");
            Assert.IsFalse(p2.HasGitHubProfile, "Invalid automatically generated field for p2");
            Assert.IsTrue(p2.HasPicture, "Invalid automatically generated field for p2");
            Assert.IsTrue(p3.HasGitHubProfile, "Invalid automatically generated field for p3");
            Assert.IsFalse(p3.HasPicture, "Invalid automatically generated field for p3");
            Assert.IsFalse(p4.HasGitHubProfile, "Invalid automatically generated field for p4");
            Assert.IsFalse(p4.HasPicture, "Invalid automatically generated field for p4");
        }
    }
}
