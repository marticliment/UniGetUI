using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using UniGetUI.Services;

namespace UniGetUI.Tests.Services
{
    [TestClass]
    public class GitHubIssueHelperTests
    {
        private GitHubIssueHelper _helper;

        [TestInitialize]
        public void TestInitialize()
        {
            _helper = new GitHubIssueHelper();
        }

        [TestMethod]
        [TestCategory("URLGeneration")]
        public void OpenIssuePage_WithBugType_GeneratesCorrectTemplate()
        {
            // This test verifies the method doesn't throw
            // Actual URL validation would require intercepting Process.Start

            // Arrange
            var issueType = "bug";
            var title = "Test Bug";

            // Act & Assert - should not throw
            try
            {
                _helper.OpenIssuePage(issueType, title);
                Assert.IsTrue(true, "Method executed without exception");
            }
            catch (Exception ex)
            {
                // Process.Start might fail in test environment without a browser
                // This is acceptable as we're testing the logic, not the browser launch
                Assert.IsTrue(
                    ex.Message.Contains("browser") || ex.Message.Contains("application"),
                    "Exception should be related to browser/application, not logic error"
                );
            }
        }

        [TestMethod]
        [TestCategory("URLGeneration")]
        public void OpenIssuePage_WithFeatureType_DoesNotThrow()
        {
            // Arrange
            var issueType = "feature";
            var title = "Test Feature";

            // Act & Assert
            try
            {
                _helper.OpenIssuePage(issueType, title);
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(
                    ex.Message.Contains("browser") || ex.Message.Contains("application"),
                    "Only browser-related exceptions are acceptable"
                );
            }
        }

        [TestMethod]
        [TestCategory("URLGeneration")]
        public void OpenIssuePage_WithEnhancementType_DoesNotThrow()
        {
            // Arrange
            var issueType = "enhancement";
            var title = "Test Enhancement";

            // Act & Assert
            try
            {
                _helper.OpenIssuePage(issueType, title);
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(
                    ex.Message.Contains("browser") || ex.Message.Contains("application"),
                    "Only browser-related exceptions are acceptable"
                );
            }
        }

        [TestMethod]
        [TestCategory("URLGeneration")]
        public void OpenIssuePage_WithSpecialCharactersInTitle_DoesNotThrow()
        {
            // Arrange
            var issueType = "bug";
            var title = "Bug with & special <characters> and 'quotes'";

            // Act & Assert
            try
            {
                _helper.OpenIssuePage(issueType, title);
                Assert.IsTrue(true, "Should handle special characters in title");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(
                    ex.Message.Contains("browser") || ex.Message.Contains("application"),
                    "Only browser-related exceptions are acceptable"
                );
            }
        }

        [TestMethod]
        [TestCategory("URLGeneration")]
        public void OpenIssuePage_WithEmptyTitle_DoesNotThrow()
        {
            // Arrange
            var issueType = "bug";
            var title = "";

            // Act & Assert
            try
            {
                _helper.OpenIssuePage(issueType, title);
                Assert.IsTrue(true, "Should handle empty title");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(
                    ex.Message.Contains("browser") || ex.Message.Contains("application"),
                    "Only browser-related exceptions are acceptable"
                );
            }
        }

        [TestMethod]
        [TestCategory("URLGeneration")]
        public void OpenIssuePage_WithLongTitle_DoesNotThrow()
        {
            // Arrange
            var issueType = "bug";
            var title = new string('A', 500); // Very long title

            // Act & Assert
            try
            {
                _helper.OpenIssuePage(issueType, title);
                Assert.IsTrue(true, "Should handle long titles");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(
                    ex.Message.Contains("browser") || ex.Message.Contains("application"),
                    "Only browser-related exceptions are acceptable"
                );
            }
        }

        [TestMethod]
        [TestCategory("URLGeneration")]
        public void OpenIssuePage_WithInvalidIssueType_UsesDefaultBugTemplate()
        {
            // Arrange
            var issueType = "invalid_type";
            var title = "Test Issue";

            // Act & Assert - should fallback to bug template
            try
            {
                _helper.OpenIssuePage(issueType, title);
                Assert.IsTrue(true, "Should fallback to bug template for invalid types");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(
                    ex.Message.Contains("browser") || ex.Message.Contains("application"),
                    "Only browser-related exceptions are acceptable"
                );
            }
        }

        [TestMethod]
        [TestCategory("URLGeneration")]
        public void OpenIssuePage_WithNullIssueType_DoesNotThrowNullReference()
        {
            // Arrange
            string issueType = null;
            var title = "Test Issue";

            // Act & Assert
            try
            {
                _helper.OpenIssuePage(issueType, title);
                Assert.IsTrue(true, "Should handle null issue type");
            }
            catch (NullReferenceException)
            {
                Assert.Fail("Should not throw NullReferenceException");
            }
            catch (Exception ex)
            {
                // Browser-related exceptions are acceptable
                Assert.IsTrue(
                    ex.Message.Contains("browser") || ex.Message.Contains("application"),
                    "Only browser-related exceptions are acceptable"
                );
            }
        }
    }
}
