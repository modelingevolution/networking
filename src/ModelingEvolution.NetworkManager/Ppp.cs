using Tmds.DBus.Protocol;

namespace ModelingEvolution.NetworkManager;

partial class Ppp : NetworkManagerObject
{
    private const string __Interface = "org.freedesktop.NetworkManager.Device.Ppp";
    internal Ppp(NetworkManagerService service, ObjectPath path) : base(service, path)
    { }
}