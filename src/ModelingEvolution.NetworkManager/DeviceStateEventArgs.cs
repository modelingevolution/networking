namespace ModelingEvolution.NetworkManager;

public class DeviceStateEventArgs : EventArgs
{
    public DeviceState OldState { get; init; } 
    public DeviceState NewState { get; init; }
}