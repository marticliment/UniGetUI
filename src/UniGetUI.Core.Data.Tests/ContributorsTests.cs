using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniGetUI.Core.Data.Tests
{
    [TestClass]
    public class ContributorsTests
    {

        [TestMethod]
        public void CheckIfContributorListIsEmpty()
        {
            Assert.AreNotEqual(ContributorsData.Contributors.Length, 0, "Contributor list is empty");
        }
    }
}
