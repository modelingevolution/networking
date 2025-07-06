using Tmds.DBus.Protocol;

namespace ModelingEvolution.NetworkManager;

partial class Loopback : NetworkManagerObject
{
    private const string __Interface = "org.freedesktop.NetworkManager.Device.Loopback";
    internal Loopback(NetworkManagerService service, ObjectPath path) : base(service, path)
    { }
}