using ModelingEvolution.NetworkManager;
using ModelingEvolution.NetworkManager.Contracts;
using MicroPlumberd;

namespace ModelingEvolution.NetworkManager.Services;

/// <summary>
/// Service to enumerate available Ethernet interfaces and populate EthernetInterfacesState.
/// Similar to WirelessStationService but for Ethernet devices.
/// </summary>
static class EthernetInterfacesService
{
    // Exclusion patterns for virtual interfaces
    private static readonly string[] VirtualInterfacePrefixes = ["veth", "docker", "br-", "virbr"];

    private static bool IsPhysicalInterface(string? name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (name == "lo") return false;
        return !VirtualInterfacePrefixes.Any(prefix => name.StartsWith(prefix));
    }

    public static async Task<bool> AppendIfRequired(NetworkManagerClient client, IPlumberInstance plumber, IEnvironment env,
        CancellationToken stoppingToken = default)
    {
        var interfaces = new List<EthernetInterface>();

        await foreach (var device in client.GetDevices())
        {
            if (device.DeviceType == DeviceType.Ethernet && IsPhysicalInterface(device.InterfaceName))
            {
                var props = await device.GetEthernetPropertiesAsync();
                interfaces.Add(new EthernetInterface
                {
                    InterfaceName = device.InterfaceName!,
                    HwAddress = props?.HwAddress ?? "",
                    State = device.State,
                    Speed = props?.Speed ?? 0,
                    Carrier = props?.Carrier ?? false
                });
            }
        }

        var state = new EthernetInterfacesState { Interfaces = interfaces.ToArray() };
        await plumber.AppendState(state, env.HostName, token: stoppingToken);
        return interfaces.Count > 0;
    }
}
