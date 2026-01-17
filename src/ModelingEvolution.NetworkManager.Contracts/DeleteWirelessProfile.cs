using MicroPlumberd;

namespace ModelingEvolution.NetworkManager.Contracts;

[ThrowsFaultException<WrongHostError>]
[ThrowsFaultException<ProfileNotFound>]
public record DeleteWirelessProfile
{
    [GuidNotEmpty]
    public Guid ProfileId { get; init; }
}
