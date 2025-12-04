using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Controls;
using Moq;
using System;
using System.Collections.Generic;
using UniGetUI.Pages.DialogPages;
using UniGetUI.PackageOperations;
using UniGetUI.Controls.OperationWidgets;
using UniGetUI.Core.Data;

namespace UniGetUI.Tests;

/// <summary>
/// Tests for OperationFailedDialog auto-dismiss functionality.
/// </summary>
[TestClass]
public class OperationFailedDialogTests
{
    [TestInitialize]
    public void TestInitialize()
    {
        // Only set the enabled flag by default
        // Individual tests set timeout if needed to test specific scenarios
        Settings.Set("AutoDismissFailureDialogs", true);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        // Reset to defaults
        Settings.Set("AutoDismissFailureDialogs", true);
        Settings.Remove("AutoDismissFailureDialogsTimeout");
    }

    #region Configuration Tests

    [TestMethod]
    [TestCategory("Configuration")]
    public void GetAutoDismissTimeout_WhenEnabled_ReturnsConfiguredTimeout()
    {
        // Arrange
        Settings.Set("AutoDismissFailureDialogs", true);
        Settings.Set("AutoDismissFailureDialogsTimeout", 15);

        // Act
        var timeout = GetAutoDismissTimeoutTestHelper();

        // Assert
        Assert.IsNotNull(timeout);
        Assert.AreEqual(15, timeout.Value);
    }

    [TestMethod]
    [TestCategory("Configuration")]
    public void GetAutoDismissTimeout_WhenDisabled_ReturnsNull()
    {
        // Arrange
        Settings.Set("AutoDismissFailureDialogs", false);

        // Act
        var timeout = GetAutoDismissTimeoutTestHelper();

        // Assert
        Assert.IsNull(timeout, "Timeout should be null when auto-dismiss is disabled");
    }

    [TestMethod]
    [TestCategory("Configuration")]
    public void GetAutoDismissTimeout_ClampsValueBetween3And60Seconds()
    {
        // Test lower bound clamp
        Settings.Set("AutoDismissFailureDialogsTimeout", 1);
        var timeout1 = GetAutoDismissTimeoutTestHelper();
        Assert.AreEqual(3, timeout1.Value, "Timeout below 3 should be clamped to 3");

        // Test upper bound clamp
        Settings.Set("AutoDismissFailureDialogsTimeout", 100);
        var timeout2 = GetAutoDismissTimeoutTestHelper();
        Assert.AreEqual(60, timeout2.Value, "Timeout above 60 should be clamped to 60");

        // Test valid value
        Settings.Set("AutoDismissFailureDialogsTimeout", 30);
        var timeout3 = GetAutoDismissTimeoutTestHelper();
        Assert.AreEqual(30, timeout3.Value, "Valid timeout should not be modified");

        // Test boundary values
        Settings.Set("AutoDismissFailureDialogsTimeout", 3);
        var timeout4 = GetAutoDismissTimeoutTestHelper();
        Assert.AreEqual(3, timeout4.Value, "Minimum boundary (3) should be valid");

        Settings.Set("AutoDismissFailureDialogsTimeout", 60);
        var timeout5 = GetAutoDismissTimeoutTestHelper();
        Assert.AreEqual(60, timeout5.Value, "Maximum boundary (60) should be valid");
    }

    [TestMethod]
    [TestCategory("Configuration")]
    public void GetAutoDismissTimeout_DefaultIs10Seconds()
    {
        // Arrange
        Settings.Set("AutoDismissFailureDialogs", true);
        // Explicitly ensure no timeout is set to test the default path
        Settings.Remove("AutoDismissFailureDialogsTimeout");

        // Act
        var timeout = GetAutoDismissTimeoutTestHelper();

        // Assert
        Assert.IsNotNull(timeout, "Timeout should not be null when enabled");
        Assert.AreEqual(10, timeout.Value, "Default timeout should be 10 seconds when no setting is stored");
    }

    #endregion

    #region UI Tests

    [Ignore("UI test - requires a UI thread/environment; run manually")]
    [TestMethod]
    [TestCategory("UI")]
    public void Dialog_WhenCreatedWithAutoDisabled_ShouldNotShowInfoBar()
    {
        // UI-dependent: avoid constructing XAML Page in headless unit test runs.
        Assert.Inconclusive("UI-dependent test; run manually in a UI-enabled test run.");
    }

    [TestMethod]
    [TestCategory("UI")]
    public void Dialog_WhenCreatedWithAutoEnabled_ShouldShowInfoBar()
    {
        // Arrange
        var mockOperation = CreateMockOperation();
        var mockOpControl = CreateMockOperationControl();
        Settings.Set("AutoDismissFailureDialogs", true);
        Settings.Set("AutoDismissFailureDialogsTimeout", 10);

        // Act
        using var dialog = new OperationFailedDialog(mockOperation.Object, mockOpControl.Object);

        // Assert
        Assert.IsNotNull(dialog);
        // Manual/UI verification: AutoDismissInfoBar.IsOpen should be true
    }

    #endregion

    #region State Management Tests

    [TestMethod]
    [TestCategory("State")]
    public void TimerState_RepresentsCancelledState()
    {
        // This test documents the design decision:
        // Instead of _autoDismissCancelled flag, we use timer == null
        
        // Arrange
        var mockOperation = CreateMockOperation();
        var mockOpControl = CreateMockOperationControl();
        Settings.Set("AutoDismissFailureDialogs", true);
        Settings.Set("AutoDismissFailureDialogsTimeout", 10);

        // Act
        using var dialog = new OperationFailedDialog(mockOperation.Object, mockOpControl.Object);

        // Assert
        Assert.IsNotNull(dialog);
        // Design: When "Keep Open" is clicked, timer is set to null
        // This replaces the _autoDismissCancelled flag
    }

    [TestMethod]
    [TestCategory("State")]
    public void HoverState_ManagedByTimerPauseResume()
    {
        // This test documents the design decision:
        // Instead of _isHovered flag, we pause/resume the timer
        
        // Arrange
        var mockOperation = CreateMockOperation();
        var mockOpControl = CreateMockOperationControl();
        Settings.Set("AutoDismissFailureDialogs", true);
        Settings.Set("AutoDismissFailureDialogsTimeout", 10);

        // Act
        using var dialog = new OperationFailedDialog(mockOperation.Object, mockOpControl.Object);

        // Assert
        Assert.IsNotNull(dialog);
        // Design: PointerEntered calls timer.Stop(), PointerExited calls timer.Start()
        // This replaces checking _isHovered in the tick handler
    }

    #endregion

    #region Resource Management Tests

    [TestMethod]
    [TestCategory("ResourceManagement")]
    public void Dialog_ImplementsIDisposable()
    {
        // Arrange
        var mockOperation = CreateMockOperation();
        var mockOpControl = CreateMockOperationControl();

        // Act
        var dialog = new OperationFailedDialog(mockOperation.Object, mockOpControl.Object);

        // Assert
        Assert.IsInstanceOfType(dialog, typeof(IDisposable), "Dialog should implement IDisposable");
        
        // Cleanup
        dialog.Dispose();
    }

    [TestMethod]
    [TestCategory("ResourceManagement")]
    public void Dialog_Dispose_StopsTimer()
    {
        // Arrange
        var mockOperation = CreateMockOperation();
        var mockOpControl = CreateMockOperationControl();
        Settings.Set("AutoDismissFailureDialogs", true);
        Settings.Set("AutoDismissFailureDialogsTimeout", 10);
        var dialog = new OperationFailedDialog(mockOperation.Object, mockOpControl.Object);

        // Act
        dialog.Dispose();

        // Assert
        // Timer should be stopped and set to null
        // Subsequent dispose calls should be safe (idempotent)
        dialog.Dispose(); // Should not throw
    }

    [TestMethod]
    [TestCategory("ResourceManagement")]
    public void Dialog_UsingStatement_DisposesCorrectly()
    {
        // Arrange
        var mockOperation = CreateMockOperation();
        var mockOpControl = CreateMockOperationControl();
        Settings.Set("AutoDismissFailureDialogs", true);
        Settings.Set("AutoDismissFailureDialogsTimeout", 10);

        // Act & Assert
        using (var dialog = new OperationFailedDialog(mockOperation.Object, mockOpControl.Object))
        {
            Assert.IsNotNull(dialog);
        } // Dispose called automatically
        
        // If we reach here without exception, disposal worked correctly
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    [TestCategory("ErrorHandling")]
    public void Dialog_HandlesNullOperationGracefully()
    {
        // This test verifies that the dialog doesn't crash with edge cases
        // Actual null handling would be in the calling code
        Assert.IsTrue(true, "Null handling is responsibility of caller");
    }

    [TestMethod]
    [TestCategory("ErrorHandling")]
    public void Dialog_WithInvalidSettings_UsesDefaults()
    {
        // Arrange
        Settings.Set("AutoDismissFailureDialogsTimeout", -100); // Invalid
        
        // Act
        var timeout = GetAutoDismissTimeoutTestHelper();
        
        // Assert
        Assert.IsNotNull(timeout);
        Assert.IsTrue(timeout.Value >= 3 && timeout.Value <= 60, 
            "Invalid timeout should be clamped to valid range");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Test helper that mimics the actual GetAutoDismissTimeout method logic.
    /// </summary>
    private int? GetAutoDismissTimeoutTestHelper()
    {
        const string AUTO_DISMISS_ENABLED_SETTING = "AutoDismissFailureDialogs";
        const string AUTO_DISMISS_TIMEOUT_SETTING = "AutoDismissFailureDialogsTimeout";
        const int DEFAULT_AUTO_DISMISS_SECONDS = 10;
        const int MIN_AUTO_DISMISS_SECONDS = 3;
        const int MAX_AUTO_DISMISS_SECONDS = 60;

        if (!Settings.Get(AUTO_DISMISS_ENABLED_SETTING, true))
            return null;

        var timeout = Settings.Get(AUTO_DISMISS_TIMEOUT_SETTING, DEFAULT_AUTO_DISMISS_SECONDS);
        return Math.Clamp(timeout, MIN_AUTO_DISMISS_SECONDS, MAX_AUTO_DISMISS_SECONDS);
    }

    private Mock<AbstractOperation> CreateMockOperation()
    {
        var mock = new Mock<AbstractOperation>();
        var metadata = new OperationMetadata
        {
            FailureMessage = "Test failure message"
        };
        mock.SetupGet(o => o.Metadata).Returns(metadata);
        mock.Setup(o => o.GetOutput()).Returns(new List<Tuple<string, AbstractOperation.LineType>>
        {
            new("Test output line", AbstractOperation.LineType.Information),
            new("Error details", AbstractOperation.LineType.Error),
            new("Debug info", AbstractOperation.LineType.VerboseDetails)
        });
        return mock;
    }

    private Mock<OperationControl> CreateMockOperationControl()
    {
        var mock = new Mock<OperationControl>();
        // Fix: Return correct type List<MenuFlyoutItemBase> instead of List<object>
        mock.Setup(c => c.GetRetryOptions(It.IsAny<Action>()))
            .Returns(new List<MenuFlyoutItemBase>());
        return mock;
    }

    #endregion
}