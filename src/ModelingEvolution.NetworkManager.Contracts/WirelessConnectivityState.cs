using MicroPlumberd;
using ModelingEvolution.Ipv4;
using ModelingEvolution.NetworkManager;

namespace ModelingEvolution.NetworkManager.Contracts;

[OutputStream("WirelessConnectivity")]
public record WirelessConnectivityState : IStatefulStream<HostName>
{
    public static string FullStreamName(HostName id) => $"WirelessConnectivity-{id}";

    public string? Ssid { get; init; }
    public string? InterfaceName { get; init; }
    public Ipv4Configuration? IpConfig { get; init; }
    public string? ConnectionName { get; init; }
    public DeviceState State { get; init; }
    public ActivationFailedReason? FailedReason { get; init; }
    public byte Signal { get; init; }
}
