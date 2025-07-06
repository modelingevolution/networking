using Tmds.DBus.Protocol;

namespace ModelingEvolution.NetworkManager;

record SettingsProperties
{
    internal ObjectPath[] Connections { get; set; } = default!;
    internal string Hostname { get; set; } = default!;
    internal bool CanModify { get; set; } = default!;
}