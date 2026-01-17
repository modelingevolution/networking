namespace ModelingEvolution.NetworkManager.Contracts;

/// <summary>
/// Composite identifier for Ethernet connection streams (HostName + InterfaceName).
/// </summary>
public readonly struct EthernetConnectionId : IEquatable<EthernetConnectionId>
{
    public HostName HostName { get; }
    public string InterfaceName { get; }

    public EthernetConnectionId(HostName hostName, string interfaceName)
    {
        HostName = hostName;
        InterfaceName = interfaceName;
    }

    public bool Equals(EthernetConnectionId other) =>
        HostName.Equals(other.HostName) &&
        string.Equals(InterfaceName, other.InterfaceName, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => obj is EthernetConnectionId other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(HostName, InterfaceName?.ToLowerInvariant());

    public override string ToString() => $"{HostName}-{InterfaceName}";

    public static bool operator ==(EthernetConnectionId left, EthernetConnectionId right) => left.Equals(right);
    public static bool operator !=(EthernetConnectionId left, EthernetConnectionId right) => !left.Equals(right);
}
