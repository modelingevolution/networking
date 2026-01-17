using System.Collections.ObjectModel;
using MicroPlumberd;

namespace ModelingEvolution.NetworkManager.Contracts;

[OutputStream("WirelessProfiles")]
public class WirelessProfilesState : Collection<WirelessProfile>, IStatefulStream<HostName>
{
    public static string FullStreamName(HostName id) => $"WirelessProfiles-{id}";
}
