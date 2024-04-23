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
            Assert.AreNotEqual(0, ContributorsData.Contributors.Length, "Contributor list is empty");
        }
    }
}
