using System.ComponentModel;

namespace ModelingEvolution.NetworkManager.Contracts;

public record WirelessStation
{
    public string? Ssid { get; init; }

    [Description("Interface name")]
    public string? InterfaceName { get; init; }

    public byte Signal { get; init; }
}
