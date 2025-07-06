using FluentAssertions;
using Xunit;

namespace ModelingEvolution.NetworkManager.Tests;

public class NetworkManagerStateTests
{
    [Fact]
    public void NetworkManagerState_ShouldHaveExpectedValues()
    {
        // Arrange & Act & Assert
        ((uint)NetworkManagerState.Unknown).Should().Be(0);
        ((uint)NetworkManagerState.ASleep).Should().Be(10);
        ((uint)NetworkManagerState.Disconnected).Should().Be(20);
        ((uint)NetworkManagerState.Disconnecting).Should().Be(30);
        ((uint)NetworkManagerState.Connecting).Should().Be(40);
    }

    [Theory]
    [InlineData(NetworkManagerState.Unknown)]
    [InlineData(NetworkManagerState.ASleep)]
    [InlineData(NetworkManagerState.Disconnected)]
    [InlineData(NetworkManagerState.Disconnecting)]
    [InlineData(NetworkManagerState.Connecting)]
    public void NetworkManagerState_AllStates_ShouldBeValid(NetworkManagerState state)
    {
        // Arrange & Act
        var enumValue = (uint)state;

        // Assert
        Enum.IsDefined(typeof(NetworkManagerState), state).Should().BeTrue();
    }
}