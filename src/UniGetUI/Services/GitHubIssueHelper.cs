using System;
using UniGetUI.Core.Logging;

namespace UniGetUI.Services
{
    /// <summary>
    /// Helper class for creating GitHub issues by opening pre-filled issue templates in the browser
    /// </summary>
    public class GitHubIssueHelper
    {
        private const string RepositoryOwner = "marticliment";
        private const string RepositoryName = "UniGetUI";

        /// <summary>
        /// Opens GitHub issue creation page in browser with pre-filled template.
        /// This approach is used instead of direct API submission to avoid requiring user authentication.
        /// The issue body should be copied to clipboard separately and pasted by the user.
        /// </summary>
        /// <param name="issueType">Type of issue (bug, feature, enhancement)</param>
        /// <param name="title">Pre-filled title for the issue</param>
        public void OpenIssuePage(string issueType, string title)
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
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo 
                { 
                    FileName = url, 
                    UseShellExecute = true 
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to open GitHub issue page: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Types of issues that can be created
    /// </summary>
    public enum IssueType
    {
        Bug,
        Enhancement,
        Feature
    }
}
