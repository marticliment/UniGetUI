namespace UniGetUI.Core.Classes.Tests
{
    public class PersonTests
    {
        [Theory]
        [InlineData("Bernat-Miquel Guimerà", "https://github.com/BernatMiquelG.png", "https://github.com/BernatMiquelG")]
        [InlineData("Bernat-Miquel Guimerà", "https://github.com/BernatMiquelG.png", null)]
        [InlineData("Bernat-Miquel Guimerà", null, "https://github.com/BernatMiquelG")]
        [InlineData("Bernat-Miquel Guimerà", null, null)]
        public void TestPerson(string name, string? profilePicture, string? gitHubUrl)
        {

            //arrange
            Person actual = new(Name: name,
                                ProfilePicture: profilePicture is null ? null : new Uri(profilePicture),
                                GitHubUrl: gitHubUrl is null ? null : new Uri(gitHubUrl));

            //Assert
            if (string.IsNullOrEmpty(profilePicture))
            {
                Assert.False(actual.HasPicture);
            }
            else
            {
                Assert.True(actual.HasPicture);
            }

            if (string.IsNullOrEmpty(gitHubUrl))
            {
                Assert.False(actual.HasGitHubProfile);
            }
            else
            {
                Assert.True(actual.HasGitHubProfile);
            }
        }
    }
}
