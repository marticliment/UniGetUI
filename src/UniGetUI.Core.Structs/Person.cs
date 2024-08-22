using System.Collections.Specialized;

namespace UniGetUI.Core.Structs
{
    public readonly struct Person
    {
        public readonly string Name;
        public readonly Uri? ProfilePicture;
        public readonly Uri? GitHubUrl;
        public readonly bool HasPicture;
        public readonly bool HasGitHubProfile;
        public readonly string Language;

        public Person(string Name, Uri? ProfilePicture = null, Uri? GitHubUrl = null, string Language = "")
        {
            this.Name = Name;
            this.ProfilePicture = ProfilePicture;
            this.HasPicture = ProfilePicture != null;
            this.GitHubUrl = GitHubUrl;
            this.HasPicture = GitHubUrl != null;
            this.Language = Language;
        }
    }
}
