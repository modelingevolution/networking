using FluentAssertions;
using Xunit;

namespace ModelingEvolution.NetworkManager.Tests;

public class DeviceTypeTests
{
    [Fact]
    public void DeviceType_ShouldHaveExpectedValues()
    {
        // Arrange & Act & Assert
        ((int)DeviceType.Unknown).Should().Be(0);
        ((int)DeviceType.Ethernet).Should().Be(1);
        ((int)DeviceType.Wifi).Should().Be(2);
        ((int)DeviceType.Bridge).Should().Be(13);
        ((int)DeviceType.WireGuard).Should().Be(29);
        ((int)DeviceType.WiFiP2P).Should().Be(30);
    }

    [Theory]
    [InlineData(DeviceType.Ethernet)]
    [InlineData(DeviceType.Wifi)]
    [InlineData(DeviceType.Bridge)]
    [InlineData(DeviceType.Bond)]
    [InlineData(DeviceType.VLAN)]
    public void DeviceType_CommonTypes_ShouldBeValid(DeviceType deviceType)
    {
        // Arrange & Act
        var enumValue = (int)deviceType;

        // Assert
        enumValue.Should().BeGreaterThan(0);
        Enum.IsDefined(typeof(DeviceType), deviceType).Should().BeTrue();
    }
}