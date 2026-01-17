using System.ComponentModel.DataAnnotations;
using MicroPlumberd;
using ModelingEvolution.Ipv4;

namespace ModelingEvolution.NetworkManager.Contracts;

[ThrowsFaultException<WrongHostError>]
[ThrowsFaultException<ConnectionError>]
public record ConfigureEthernetConnection
{

    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string? InterfaceName { get; set; }

    public Ip4Method Method { get; set; } = Ip4Method.Auto;

    /// <summary>
    /// IP configuration (address/prefix + gateway). Required when Method is Manual.
    /// </summary>
    public Ipv4Configuration? IpConfig { get; set; }

    /// <summary>
    /// MTU (Maximum Transmission Unit). 0 or null means auto/default (typically 1500).
    /// Use 9000 for jumbo frames (GigE Vision cameras).
    /// </summary>
    public uint? Mtu { get; set; }
}
