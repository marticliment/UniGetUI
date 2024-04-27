namespace UniGetUI.Core.Data.Tests
{
    public class ContributorsTests
    {

        [Fact]
        public void CheckIfContributorListIsEmpty()
        {
            Assert.NotEmpty(ContributorsData.Contributors);
        }
    }
}
