using System.Diagnostics.CodeAnalysis;
using MicroPlumberd;

namespace ModelingEvolution.NetworkManager.Contracts;

[ThrowsFaultException<WrongHostError>]
[ThrowsFaultException<ConnectionError>]
public record ConnectAccessPoint
{
    [NotNull]
    public string? Ssid { get; set; }
}
