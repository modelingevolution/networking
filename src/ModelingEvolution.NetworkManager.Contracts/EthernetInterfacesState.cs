using MicroPlumberd;

namespace ModelingEvolution.NetworkManager.Contracts;

[OutputStream("EthernetInterfaces")]
public record EthernetInterfacesState : IStatefulStream<HostName>
{
    public static string FullStreamName(HostName id) => $"EthernetInterfaces-{id}";

    public EthernetInterface[] Interfaces { get; init; } = [];
}
