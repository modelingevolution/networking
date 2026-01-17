using MicroPlumberd;
using ModelingEvolution.Ipv4;
using ModelingEvolution.NetworkManager;

namespace ModelingEvolution.NetworkManager.Contracts;

[OutputStream("EthernetConnection")]
public record EthernetConnectionState : IStatefulStream<EthernetConnectionId>
{
    public static string FullStreamName(EthernetConnectionId id) => $"EthernetConnection-{id.HostName}-{id.InterfaceName}";

    public string? InterfaceName { get; init; }
    public string? ConnectionName { get; init; }
    public DeviceState State { get; init; }

    // IP Configuration
    public Ip4Method Ip4Method { get; init; }
    public Ipv4Configuration? IpConfig { get; init; }

    // DNS Configuration
    public DnsMethod DnsMethod { get; init; }
    public Ipv4Address? PrimaryDns { get; init; }
    public Ipv4Address? SecondaryDns { get; init; }

    // Ethernet-specific
    public uint Mtu { get; init; }
    public uint Speed { get; init; }  // Mbps
    public bool Carrier { get; init; } // Cable connected
    public string? HwAddress { get; init; } // MAC address
}
