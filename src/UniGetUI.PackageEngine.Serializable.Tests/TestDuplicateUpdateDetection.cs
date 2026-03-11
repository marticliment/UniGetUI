using NSubstitute;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Tests;

/// <summary>
/// Tests for duplicate detection in update operations (Issue #4131).
/// These tests verify the logic used in AppOperationHelper.UpdateAll() and UpdateAllForManager()
/// to prevent multiple update operations for the same package from being queued simultaneously.
/// </summary>
public class TestDuplicateUpdateDetection
{
    /// <summary>
    /// Test that verifies Package.GetHash() returns the same value for identical packages.
    /// This is the core mechanism used to detect duplicate operations in the queue.
    /// 
    /// Background: Issue #4131 - When unattended updates trigger repeatedly while UAC prompts
    /// are pending, UpdateAll() was creating dozens of duplicate operations for the same package.
    /// The fix uses GetHash() to check if an operation for a package already exists in the queue.
    /// </summary>
    [Fact]
    public void GetHash_ShouldReturnSameValue_ForIdenticalPackages()
    {
        // Arrange - Create mock manager and source
        // Note: We use the same instances to ensure consistent hashing
        var mockManager = Substitute.For<IPackageManager>();
        mockManager.Name.Returns("TestManager");

        var mockSource = Substitute.For<IManagerSource>();
        mockSource.Name.Returns("TestSource");

        // Create two package instances with identical identity (same manager, source, and ID)
        var package1 = new Package(
            name: "TestPackage",
            id: "test.package.id",
            version: "1.0.0",
            source: mockSource,
            manager: mockManager
        );

        var package2 = new Package(
            name: "TestPackage",
            id: "test.package.id",
            version: "1.0.0",
            source: mockSource,
            manager: mockManager
        );

        // Act - Get hash for both packages
        long hash1 = package1.GetHash();
        long hash2 = package2.GetHash();

        // Assert - Hashes must be equal for the duplicate detection to work
        Assert.Equal(hash1, hash2);
    }

    /// <summary>
    /// Test that verifies Package.GetHash() returns different values for different packages.
    /// This ensures the duplicate detection doesn't falsely match unrelated packages.
    /// </summary>
    [Fact]
    public void GetHash_ShouldReturnDifferentValues_ForDifferentPackages()
    {
        // Arrange
        var mockManager = Substitute.For<IPackageManager>();
        mockManager.Name.Returns("TestManager");

        var mockSource = Substitute.For<IManagerSource>();
        mockSource.Name.Returns("TestSource");

        // Create packages with different IDs
        var package1 = new Package(
            name: "TestPackage1",
            id: "test.package.one",
            version: "1.0.0",
            source: mockSource,
            manager: mockManager
        );

        var package2 = new Package(
            name: "TestPackage2",
            id: "test.package.two",
            version: "1.0.0",
            source: mockSource,
            manager: mockManager
        );

        // Act
        long hash1 = package1.GetHash();
        long hash2 = package2.GetHash();

        // Assert - Hashes must be different so we don't skip legitimate different packages
        Assert.NotEqual(hash1, hash2);
    }

    /// <summary>
    /// Test that verifies packages from different managers have different hashes.
    /// The duplicate detection must consider the package manager as part of identity.
    /// </summary>
    [Fact]
    public void GetHash_ShouldDiffer_ForSamePackageFromDifferentManagers()
    {
        // Arrange - Create two different managers
        var mockManager1 = Substitute.For<IPackageManager>();
        mockManager1.Name.Returns("WinGet");

        var mockManager2 = Substitute.For<IPackageManager>();
        mockManager2.Name.Returns("Chocolatey");

        var mockSource = Substitute.For<IManagerSource>();
        mockSource.Name.Returns("TestSource");

        // Same package ID from different managers
        var packageFromWinGet = new Package(
            name: "TestPackage",
            id: "test.package",
            version: "1.0.0",
            source: mockSource,
            manager: mockManager1
        );

        var packageFromChoco = new Package(
            name: "TestPackage",
            id: "test.package",
            version: "1.0.0",
            source: mockSource,
            manager: mockManager2
        );

        // Act
        long hashWinGet = packageFromWinGet.GetHash();
        long hashChoco = packageFromChoco.GetHash();

        // Assert - Must be different because they're from different managers
        Assert.NotEqual(hashWinGet, hashChoco);
    }

    /// <summary>
    /// Test simulating the scenario where multiple UpdateAll() calls should detect duplicates.
    /// This represents what happens when auto-updates trigger while operations are queued.
    /// </summary>
    [Fact]
    public void SimulateDuplicateDetection_MultipleIdenticalPackages_ShouldHaveSameHash()
    {
        // Arrange - Simulate receiving same package multiple times from UpgradablePackagesLoader
        var mockManager = Substitute.For<IPackageManager>();
        mockManager.Name.Returns("WinGet");

        var mockSource = Substitute.For<IManagerSource>();
        mockSource.Name.Returns("winget");

        // Simulate UpdateAll() being called 3 times, each time getting "same" package
        var packagesFromFirstCall = new Package(
            name: "Git",
            id: "Git.Git",
            version: "2.40.0",
            source: mockSource,
            manager: mockManager
        );

        var packagesFromSecondCall = new Package(
            name: "Git",
            id: "Git.Git",
            version: "2.40.0",
            source: mockSource,
            manager: mockManager
        );

        var packagesFromThirdCall = new Package(
            name: "Git",
            id: "Git.Git",
            version: "2.40.0",
            source: mockSource,
            manager: mockManager
        );

        // Act - Get hashes that would be used for duplicate detection
        var hash1 = packagesFromFirstCall.GetHash();
        var hash2 = packagesFromSecondCall.GetHash();
        var hash3 = packagesFromThirdCall.GetHash();

        // Assert - All three should have the same hash, allowing duplicate detection
        Assert.Equal(hash1, hash2);
        Assert.Equal(hash2, hash3);
        Assert.Equal(hash1, hash3);

        // In the actual implementation, the second and third calls would find
        // an existing operation with matching hash and skip creating duplicates
    }
}

