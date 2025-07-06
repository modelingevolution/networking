using FluentAssertions;
using Xunit;

namespace ModelingEvolution.NetworkManager.Tests;

public class PathIdTests
{
    [Fact]
    public void PathId_WithValidPath_ShouldCreateCorrectly()
    {
        // Arrange
        var path = "/org/freedesktop/NetworkManager/Devices/1";

        // Act
        PathId pathId = path;

        // Assert
        pathId.Path.Should().Be(path);
        pathId.ToString().Should().Be(path);
    }

    [Fact]
    public void PathId_WithEmptyPath_ShouldHandleGracefully()
    {
        // Arrange
        var path = "";

        // Act
        PathId pathId = path;

        // Assert
        pathId.Path.Should().Be(path);
        pathId.ToString().Should().Be(path);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/org/freedesktop/NetworkManager")]
    [InlineData("/org/freedesktop/NetworkManager/Devices/0")]
    [InlineData("/org/freedesktop/NetworkManager/AccessPoint/1")]
    public void PathId_WithVariousPaths_ShouldPreservePath(string path)
    {
        // Arrange & Act
        PathId pathId = path;

        // Assert
        pathId.Path.Should().Be(path);
        pathId.ToString().Should().Be(path);
    }

    [Fact]
    public void PathId_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var path = "/org/freedesktop/NetworkManager/Devices/1";
        PathId pathId1 = path;
        PathId pathId2 = path;
        PathId pathId3 = "/different/path";

        // Act & Assert
        pathId1.Should().Be(pathId2);
        pathId1.Should().NotBe(pathId3);
        pathId1.GetHashCode().Should().Be(pathId2.GetHashCode());
    }

    [Fact]
    public void PathId_ImplicitConversion_ShouldWork()
    {
        // Arrange
        var path = "/org/freedesktop/NetworkManager/Devices/1";

        // Act
        PathId pathId = path;

        // Assert
        pathId.Path.Should().Be(path);
    }
}