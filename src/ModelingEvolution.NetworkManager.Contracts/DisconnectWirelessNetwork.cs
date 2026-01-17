using System.ComponentModel.DataAnnotations;
using MicroPlumberd;

namespace ModelingEvolution.NetworkManager.Contracts;

[ThrowsFaultException<WrongHostError>]
[ThrowsFaultException<ProfileNotFound>]
public record DisconnectWirelessNetwork
{
    [Required(AllowEmptyStrings = false)]
    public string? Ssid { get; init; }

    [GuidNotEmpty]
    public Guid ProfileId { get; init; }
}
