using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UniGetUI.Core.Logging;

namespace UniGetUI.Services
{
    public class GitHubIssueHelper
    {
        private const string GitHubApiBaseUrl = "https://api.github.com";
        private const string RepositoryOwner = "marticliment";
        private const string RepositoryName = "UniGetUI";

        private readonly HttpClient _httpClient;

        public GitHubIssueHelper()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "UniGetUI-FeedbackApp");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        }

        public void OpenIssuePage(string issueType, string title, string body)
        {
            try
            {
                var templateName = issueType switch
                {
                    "bug" => "bug-issue.yml",
                    "feature" => "feature-request.yml",
                    "enhancement" => "enhancement.yml",
                    _ => "bug-issue.yml"
                };

                var labelParam = issueType switch
                {
                    "bug" => "bug",
                    "feature" => "new-feature",
                    "enhancement" => "enhancement",
                    _ => "bug"
                };

                var encodedTitle = Uri.EscapeDataString(title);
                var url = $"https://github.com/{RepositoryOwner}/{RepositoryName}/issues/new?assignees={RepositoryOwner}&labels={labelParam}&template={templateName}&title={encodedTitle}";

                Logger.Info($"Opening GitHub issue page: {url}");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to open GitHub issue page: {ex.Message}");
            }
        }
    }

    public enum IssueType { Bug, Enhancement, Feature }
}
