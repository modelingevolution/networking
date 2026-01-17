using System.ComponentModel.DataAnnotations;
using MicroPlumberd;
using ModelingEvolution.Ipv4;

namespace ModelingEvolution.NetworkManager.Contracts;

[ThrowsFaultException<WrongHostError>]
[ThrowsFaultException<ConnectionError>]
public record ConfigureDns
{
    [Required]
    public string? InterfaceName { get; set; }

    public DnsMethod Method { get; set; } = DnsMethod.Auto;

    /// <summary>
    /// Primary DNS server. Required when Method is Manual.
    /// </summary>
    public Ipv4Address? PrimaryDns { get; set; }

    /// <summary>
    /// Secondary DNS server. Optional.
    /// </summary>
    public Ipv4Address? SecondaryDns { get; set; }
}
