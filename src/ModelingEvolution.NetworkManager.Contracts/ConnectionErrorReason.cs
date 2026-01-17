namespace ModelingEvolution.NetworkManager.Contracts;

public enum ConnectionErrorReason
{
    Unknown,
    MissingProfile,
    AccessPointNotFound,
    LoginFailed,
    NoSecrets,
    IpConfigInvalid,
    ConnectTimeout,
    DeviceDisconnected
}
