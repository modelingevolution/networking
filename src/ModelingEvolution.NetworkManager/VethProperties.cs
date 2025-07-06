using Tmds.DBus.Protocol;

namespace ModelingEvolution.NetworkManager;

record VethProperties
{
    internal ObjectPath Peer { get; set; } = default!;
}