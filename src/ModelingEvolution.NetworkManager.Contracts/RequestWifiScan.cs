using MicroPlumberd;

namespace ModelingEvolution.NetworkManager.Contracts;

[ThrowsFaultException<WrongHostError>]
public record RequestWifiScan
{
}
