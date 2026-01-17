namespace ModelingEvolution.NetworkManager.Contracts;

public class ConnectionError
{
    public string? Message { get; init; }
    public ConnectionErrorReason Reason { get; init; }
    public string? ProfileFileName { get; init; }
}
