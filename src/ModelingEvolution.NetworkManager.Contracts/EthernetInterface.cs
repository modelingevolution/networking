using ModelingEvolution.NetworkManager;

namespace ModelingEvolution.NetworkManager.Contracts;

public record EthernetInterface
{
    public string InterfaceName { get; init; } = "";
    public string HwAddress { get; init; } = "";
    public DeviceState State { get; init; }
    public uint Speed { get; init; }
    public bool Carrier { get; init; }
}
