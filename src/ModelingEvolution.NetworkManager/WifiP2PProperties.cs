using Tmds.DBus.Protocol;

namespace ModelingEvolution.NetworkManager;

record WifiP2PProperties
{
    internal string HwAddress { get; set; } = default!;
    internal ObjectPath[] Peers { get; set; } = default!;
}