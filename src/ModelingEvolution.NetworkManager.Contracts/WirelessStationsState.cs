using System.Collections.ObjectModel;
using MicroPlumberd;

namespace ModelingEvolution.NetworkManager.Contracts;

[OutputStream("WirelessStations")]
public class WirelessStationsState : Collection<WirelessStation>, IStatefulStream<HostName>
{
    public static string FullStreamName(HostName id) => $"WirelessStations-{id}";
}
