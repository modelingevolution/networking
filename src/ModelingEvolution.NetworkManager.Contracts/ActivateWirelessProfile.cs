using MicroPlumberd;

namespace ModelingEvolution.NetworkManager.Contracts;

[ThrowsFaultException<WrongHostError>]
[ThrowsFaultException<ProfileNotFound>]
[ThrowsFaultException<ConnectionError>]
public record ActivateWirelessProfile
{
    [GuidNotEmpty]
    public Guid ProfileId { get; init; }
}
