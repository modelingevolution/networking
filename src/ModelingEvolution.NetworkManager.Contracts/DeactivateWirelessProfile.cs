using MicroPlumberd;

namespace ModelingEvolution.NetworkManager.Contracts;

[ThrowsFaultException<WrongHostError>]
[ThrowsFaultException<ProfileNotFound>]
public record DeactivateWirelessProfile
{
    [GuidNotEmpty]
    public Guid ProfileId { get; init; }
}
