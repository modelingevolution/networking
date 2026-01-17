namespace ModelingEvolution.NetworkManager.Contracts;

public record WirelessProfile
{
    public Guid Id => FileName?.ToGuid() ?? Guid.Empty;
    public string? ProfileName { get; init; }
    public string? Ssid { get; init; }
    public string? InterfaceName { get; init; }
    public string? FileName { get; init; }
    public bool IsConnected { get; init; }
}
