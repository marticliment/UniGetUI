namespace UniGetUI.Core.Data.Tests
{
    public class LicensesTest
    {
        [Fact]
        public void EnsureLicenseUrlsExist()
        {
            List<string> MissingUrls = [];

            foreach (string library in LicenseData.LicenseNames.Keys)
            {
                if (!LicenseData.LicenseURLs.ContainsKey(library))
                {
                    MissingUrls.Add(library);
                }
            }

            Assert.True(MissingUrls.Count == 0, "The list of missing licenses is not empty: " + MissingUrls.ToArray().ToString());
        }

        [Fact]
        public void EnsureHomepageUrlsExist()
        {
            List<string> MissingUrls = [];

            foreach (string library in LicenseData.LicenseNames.Keys)
            {
                if (!LicenseData.HomepageUrls.ContainsKey(library))
                {
                    MissingUrls.Add(library);
                }
            }

            Assert.True(MissingUrls.Count == 0, "The list of missing licenses is not empty: " + MissingUrls.ToArray().ToString());
        }

    }
}
