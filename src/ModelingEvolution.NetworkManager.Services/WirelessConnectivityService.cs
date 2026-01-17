using ModelingEvolution.NetworkManager;
using ModelingEvolution.NetworkManager.Contracts;
using MicroPlumberd;

namespace ModelingEvolution.NetworkManager.Services;

static class WirelessConnectivityService
{
    public static async Task Append(NetworkManagerClient client, IPlumberInstance plumber, IEnvironment env,
        DeviceState? st = null,
        PathId? id = null,
        CancellationToken stoppingToken = default)
    {
        var wifiDev = id != null
            ? (WifiDeviceInfo)(await client.GetDevice(id.Value))!
            : await Ax.FirstOrDefaultAsync(Ax.OfType<WifiDeviceInfo>(client.GetDevices()), stoppingToken);

        await Append(wifiDev, st, plumber, env, stoppingToken);
    }

    private static async Task Append(WifiDeviceInfo? wifiDev, DeviceState? st, IPlumberInstance plumber, IEnvironment env,
        CancellationToken stoppingToken)
    {
        if (wifiDev != null)
        {
            var cstate = (DeviceState)wifiDev.State;
            if ((st ?? cstate) == DeviceState.Activated)
            {
                var ac = await wifiDev.GetConnectionProfile();
                var connectionInfo = await wifiDev.GetConnectionInfo();
                var accessPointInfo = await wifiDev.AccessPoint();

                WirelessConnectivityState state = new WirelessConnectivityState
                {
                    ConnectionName = ac?.FileName,
                    Ssid = accessPointInfo?.Ssid,
                    IpConfig = connectionInfo?.Ip4Config,
                    InterfaceName = wifiDev.InterfaceName,
                    State = st ?? (DeviceState)wifiDev.State,
                    Signal = accessPointInfo?.SignalStrength ?? 0
                };
                await plumber.AppendState(state, env.HostName, token: stoppingToken);
            }
            else
            {
                var accessPointInfo = await wifiDev.AccessPoint();
                var ac = await wifiDev.GetConnectionProfile();

                WirelessConnectivityState state = new WirelessConnectivityState
                {
                    ConnectionName = ac?.FileName,
                    Ssid = accessPointInfo?.Ssid,
                    IpConfig = null,
                    InterfaceName = wifiDev.InterfaceName,
                    State = st ?? (DeviceState)wifiDev.State,
                    Signal = accessPointInfo?.SignalStrength ?? 0
                };
                await plumber.AppendState(state, env.HostName, token: stoppingToken);
            }
        }
    }
}
