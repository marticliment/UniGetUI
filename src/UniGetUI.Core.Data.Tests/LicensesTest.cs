namespace UniGetUI.Core.Data.Tests
{
    [TestClass]
    public class LicensesTest
    {
        [TestMethod]
        public void EnsureLicenseUrlsExist()
        {
            List<string> MissingUrls = new();

            foreach (string library in LicenseData.LicenseNames.Keys)
                if(!LicenseData.LicenseURLs.ContainsKey(library))
                    MissingUrls.Add(library);

            Assert.AreEqual(MissingUrls.Count, 0, "The list of missing licenses is not empty: " + MissingUrls.ToArray().ToString());
        }

        [TestMethod]
        public void EnsureHomepageUrlsExist()
        {
            List<string> MissingUrls = new();

            foreach (string library in LicenseData.LicenseNames.Keys)
                if (!LicenseData.HomepageUrls.ContainsKey(library))
                    MissingUrls.Add(library);

            Assert.AreEqual(MissingUrls.Count, 0, "The list of missing licenses is not empty: " + MissingUrls.ToArray().ToString());
        }

    }
}