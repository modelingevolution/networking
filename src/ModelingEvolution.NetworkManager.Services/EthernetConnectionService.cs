using ModelingEvolution.Ipv4;
using ModelingEvolution.NetworkManager;
using ModelingEvolution.NetworkManager.Contracts;
using MicroPlumberd;

namespace ModelingEvolution.NetworkManager.Services;

static class EthernetConnectionService
{
    public static async Task AppendIfRequired(NetworkManagerClient client, IPlumberInstance plumber, IEnvironment env,
        string interfaceName, CancellationToken stoppingToken = default)
    {
        var device = await Ax.FirstOrDefaultAsync(
            Ax.Where(client.GetDevices(), x => x.InterfaceName == interfaceName && x.DeviceType == DeviceType.Ethernet),
            stoppingToken);

        if (device == null) return;

        await Append(device, plumber, env, interfaceName, stoppingToken);
    }

    private static async Task Append(DeviceInfo device, IPlumberInstance plumber, IEnvironment env,
        string interfaceName, CancellationToken stoppingToken)
    {
        Ipv4Configuration? ipConfig = null;
        Ipv4Address? primaryDns = null;
        Ipv4Address? secondaryDns = null;
        var ip4Method = Ip4Method.Auto;
        var dnsMethod = DnsMethod.Auto;

        // Get IP configuration
        var connectionInfo = await device.GetConnectionInfo();
        if (connectionInfo?.Ip4Config != null)
        {
            ipConfig = connectionInfo.Ip4Config;
        }

        // Get DNS servers
        var dnsServers = await device.GetDnsServersAsync();
        if (dnsServers.Length > 0)
        {
            primaryDns = Ipv4Address.From(dnsServers[0]);
            if (dnsServers.Length > 1)
                secondaryDns = Ipv4Address.From(dnsServers[1]);
        }

        // Check connection settings to determine if manual or auto
        var profile = await device.GetConnectionProfile();
        if (profile != null)
        {
            var settings = await profile.GetConnectionSettings();
            if (settings.TryGetValue("ipv4", out var ipv4Settings))
            {
                if (ipv4Settings.TryGetValue("method", out var methodVal))
                {
                    var method = methodVal.GetString();
                    ip4Method = method == "manual" ? Ip4Method.Manual : Ip4Method.Auto;
                }

                if (ipv4Settings.TryGetValue("ignore-auto-dns", out var ignoreDnsVal))
                {
                    dnsMethod = ignoreDnsVal.GetBool() ? DnsMethod.Manual : DnsMethod.Auto;
                }
            }
        }

        // Get Ethernet-specific properties
        var ethProps = await device.GetEthernetPropertiesAsync();

        var state = new EthernetConnectionState
        {
            InterfaceName = interfaceName,
            ConnectionName = profile?.FileName,
            State = device.State,
            Ip4Method = ip4Method,
            IpConfig = ipConfig,
            DnsMethod = dnsMethod,
            PrimaryDns = primaryDns,
            SecondaryDns = secondaryDns,
            Mtu = ethProps?.Mtu ?? 0,
            Speed = ethProps?.Speed ?? 0,
            Carrier = ethProps?.Carrier ?? false,
            HwAddress = ethProps?.HwAddress
        };

        // Append using interface-specific stream name
        var connectionId = new EthernetConnectionId(env.HostName, interfaceName);
        await plumber.AppendState(state, connectionId, token: stoppingToken);
    }
}
