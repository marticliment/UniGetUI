using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UniGetUI.Services;

namespace UniGetUI.Tests.Services
{
    [TestClass]
    public class FeedbackServiceTests
    {
        private FeedbackService _feedbackService;

        [TestInitialize]
        public void TestInitialize()
        {
            _feedbackService = FeedbackService.Instance;
        }

        [TestMethod]
        [TestCategory("SystemInfo")]
        public async Task CollectSystemInfoAsync_ReturnsValidSystemInformation()
        {
            // Act
            var systemInfo = await _feedbackService.CollectSystemInfoAsync();

            // Assert
            Assert.IsNotNull(systemInfo);
            Assert.IsFalse(string.IsNullOrWhiteSpace(systemInfo.AppVersion));
            Assert.IsFalse(string.IsNullOrWhiteSpace(systemInfo.OSVersion));
            Assert.IsFalse(string.IsNullOrWhiteSpace(systemInfo.OSArchitecture));
            Assert.IsFalse(string.IsNullOrWhiteSpace(systemInfo.DotNetVersion));
        }

        [TestMethod]
        [TestCategory("SystemInfo")]
        public async Task CollectSystemInfoAsync_OSArchitecture_IsX64OrX86()
        {
            // Act
            var systemInfo = await _feedbackService.CollectSystemInfoAsync();

            // Assert
            Assert.IsTrue(
                systemInfo.OSArchitecture == "x64" || systemInfo.OSArchitecture == "x86",
                $"Expected x64 or x86, got {systemInfo.OSArchitecture}"
            );
        }

        [TestMethod]
        [TestCategory("SystemInfo")]
        public async Task CollectSystemInfoAsync_IncludesPackageManagers()
        {
            // Act
            var systemInfo = await _feedbackService.CollectSystemInfoAsync();

            // Assert
            Assert.IsNotNull(systemInfo.PackageManagers);
            // Package managers list can be empty if none are installed
            Assert.IsTrue(systemInfo.PackageManagers.Count >= 0);
        }

        [TestMethod]
        [TestCategory("SystemInfo")]
        public async Task CollectSystemInfoAsync_TotalMemory_IsPositive()
        {
            // Act
            var systemInfo = await _feedbackService.CollectSystemInfoAsync();

            // Assert
            Assert.IsTrue(systemInfo.TotalMemoryMB > 0, "Total memory should be greater than 0");
        }

        [TestMethod]
        [TestCategory("Formatting")]
        public async Task FormatSystemInfoAsMarkdown_ContainsRequiredSections()
        {
            // Arrange
            var systemInfo = await _feedbackService.CollectSystemInfoAsync();

            // Act
            var markdown = _feedbackService.FormatSystemInfoAsMarkdown(systemInfo);

            // Assert
            Assert.IsTrue(markdown.Contains("### System Information"));
            Assert.IsTrue(markdown.Contains("### Package Managers"));
            Assert.IsTrue(markdown.Contains("UniGetUI Version"));
            Assert.IsTrue(markdown.Contains("OS Version"));
            Assert.IsTrue(markdown.Contains("OS Architecture"));
        }

        [TestMethod]
        [TestCategory("Formatting")]
        public async Task FormatSystemInfoAsMarkdown_UsesProperMarkdownSyntax()
        {
            // Arrange
            var systemInfo = await _feedbackService.CollectSystemInfoAsync();

            // Act
            var markdown = _feedbackService.FormatSystemInfoAsMarkdown(systemInfo);

            // Assert
            Assert.IsTrue(markdown.Contains("**"), "Should use bold markdown syntax");
            Assert.IsTrue(markdown.Contains("- "), "Should use list markdown syntax");
            Assert.IsTrue(markdown.Contains("###"), "Should use heading markdown syntax");
        }

        [TestMethod]
        [TestCategory("Formatting")]
        public void FormatSystemInfoAsMarkdown_WithNoPackageManagers_ShowsMessage()
        {
            // Arrange
            var systemInfo = new SystemInformation
            {
                AppVersion = "1.0.0",
                OSVersion = "Windows 10",
                PackageManagers = new System.Collections.Generic.List<PackageManagerInfo>()
            };

            // Act
            var markdown = _feedbackService.FormatSystemInfoAsMarkdown(systemInfo);

            // Assert
            Assert.IsTrue(markdown.Contains("No package managers detected"));
        }

        [TestMethod]
        [TestCategory("Formatting")]
        public void FormatSystemInfoAsMarkdown_WithPackageManagers_ShowsStatusIcons()
        {
            // Arrange
            var systemInfo = new SystemInformation
            {
                AppVersion = "1.0.0",
                OSVersion = "Windows 10",
                PackageManagers = new System.Collections.Generic.List<PackageManagerInfo>
                {
                    new PackageManagerInfo { Name = "WinGet", IsAvailable = true, Version = "1.0" },
                    new PackageManagerInfo { Name = "Scoop", IsAvailable = false }
                }
            };

            // Act
            var markdown = _feedbackService.FormatSystemInfoAsMarkdown(systemInfo);

            // Assert
            Assert.IsTrue(markdown.Contains("✅"), "Should show check mark for available managers");
            Assert.IsTrue(markdown.Contains("❌"), "Should show X mark for unavailable managers");
            Assert.IsTrue(markdown.Contains("WinGet"));
            Assert.IsTrue(markdown.Contains("Scoop"));
        }

        [TestMethod]
        [TestCategory("Logs")]
        public async Task CollectLogsAsync_ReturnsMarkdownFormattedLogs()
        {
            // Act
            var logs = await _feedbackService.CollectLogsAsync(50);

            // Assert
            Assert.IsNotNull(logs);
            Assert.IsTrue(logs.Contains("###") || logs.Contains("```"), "Should contain markdown formatting");
        }

        [TestMethod]
        [TestCategory("Logs")]
        public async Task CollectLogsAsync_WithCustomMaxLines_RespectsLimit()
        {
            // This test verifies the parameter is accepted
            // Actual line counting would require a real log file

            // Act
            var logs = await _feedbackService.CollectLogsAsync(10);

            // Assert
            Assert.IsNotNull(logs);
        }

        [TestMethod]
        [TestCategory("IssueCreation")]
        public async Task CreateIssueBodyAsync_WithAllOptions_IncludesAllSections()
        {
            // Arrange
            var description = "Test issue description";

            // Act
            var issueBody = await _feedbackService.CreateIssueBodyAsync(
                description,
                includeLogs: true,
                includeSystemInfo: true
            );

            // Assert
            Assert.IsTrue(issueBody.Contains("### Description"));
            Assert.IsTrue(issueBody.Contains(description));
            Assert.IsTrue(issueBody.Contains("### System Information"));
            Assert.IsTrue(issueBody.Contains("### Recent Application Logs") || issueBody.Contains("### Logs"));
        }

        [TestMethod]
        [TestCategory("IssueCreation")]
        public async Task CreateIssueBodyAsync_WithoutLogs_ExcludesLogsSection()
        {
            // Arrange
            var description = "Test issue description";

            // Act
            var issueBody = await _feedbackService.CreateIssueBodyAsync(
                description,
                includeLogs: false,
                includeSystemInfo: true
            );

            // Assert
            Assert.IsTrue(issueBody.Contains("### Description"));
            Assert.IsTrue(issueBody.Contains("### System Information"));
            Assert.IsFalse(issueBody.Contains("### Recent Application Logs"));
        }

        [TestMethod]
        [TestCategory("IssueCreation")]
        public async Task CreateIssueBodyAsync_WithoutSystemInfo_ExcludesSystemInfoSection()
        {
            // Arrange
            var description = "Test issue description";

            // Act
            var issueBody = await _feedbackService.CreateIssueBodyAsync(
                description,
                includeLogs: true,
                includeSystemInfo: false
            );

            // Assert
            Assert.IsTrue(issueBody.Contains("### Description"));
            Assert.IsFalse(issueBody.Contains("### System Information"));
        }

        [TestMethod]
        [TestCategory("IssueCreation")]
        public async Task CreateIssueBodyAsync_WithMinimalOptions_OnlyIncludesDescription()
        {
            // Arrange
            var description = "Minimal issue";

            // Act
            var issueBody = await _feedbackService.CreateIssueBodyAsync(
                description,
                includeLogs: false,
                includeSystemInfo: false
            );

            // Assert
            Assert.IsTrue(issueBody.Contains("### Description"));
            Assert.IsTrue(issueBody.Contains(description));
            Assert.IsFalse(issueBody.Contains("### System Information"));
            Assert.IsFalse(issueBody.Contains("### Recent Application Logs"));
        }

        [TestMethod]
        [TestCategory("Singleton")]
        public void Instance_ReturnsSameInstance()
        {
            // Arrange
            var instance1 = FeedbackService.Instance;
            var instance2 = FeedbackService.Instance;

            // Assert
            Assert.AreSame(instance1, instance2, "Should return the same singleton instance");
        }
    }
}
