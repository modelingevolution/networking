using Tmds.DBus.Protocol;

namespace ModelingEvolution.NetworkManager;

record DnsManagerProperties
{
    internal string Mode { get; set; } = default!;
    internal string RcManager { get; set; } = default!;
    internal Dictionary<string, VariantValue>[] Configuration { get; set; } = default!;
}