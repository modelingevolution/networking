using ModelingEvolution.NetworkManager;
using ModelingEvolution.NetworkManager.Contracts;
using MicroPlumberd;
using MicroPlumberd.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using Tmds.DBus.Protocol;

namespace ModelingEvolution.NetworkManager.Services;

[CommandHandler]
public partial class NetworkManagerCommandHandler : IAsyncDisposable
{
    private NetworkManagerClient? _client;
    private readonly IPlumberInstance _plumber;
    private readonly IEnvironment _env;
    private readonly ILogger<NetworkManagerCommandHandler> _log;
    private readonly Channel<Func<CancellationToken, Task>> _channel;
    private readonly CancellationTokenSource _cts;

    public NetworkManagerCommandHandler(IPlumberInstance plumber, IEnvironment env, ILogger<NetworkManagerCommandHandler> log)
    {
        _plumber = plumber;
        _env = env;
        _log = log;
        _channel = Channel.CreateBounded<Func<CancellationToken, Task>>(new BoundedChannelOptions(5) { FullMode = BoundedChannelFullMode.DropOldest });
        _cts = new CancellationTokenSource();
        _ = Task.Factory.StartNew(OnStateAppender, TaskCreationOptions.LongRunning);
    }

    private async Task OnStateAppender()
    {
        await foreach (var i in _channel.Reader.ReadAllAsync(_cts.Token))
            await i(_cts.Token);
    }

    [ThrowsFaultException<WrongHostError>]
    [ThrowsFaultException<ConnectionError>]
    public async Task Handle(HostName hostName, DefineWirelessProfile data)
    {
        _log.LogInformation("DefineWirelessProfile: Ssid={Ssid}", data.Ssid);

        if (string.IsNullOrWhiteSpace(data.Password?.Value) || data.Password.Value.Length < 8 || data.Password.Value.Length > 64)
        {
            _log.LogWarning("DefineWirelessProfile failed: Invalid password length");
            throw new FaultException<ConnectionError>(new ConnectionError
            {
                Message = "Password cannot be empty. Must be at least 8 and less than 64 chars.",
                Reason = ConnectionErrorReason.Unknown
            });
        }

        await Prepare(hostName);

        var ap = await Ax.FirstOrDefaultAsync(
            Ax.OrderByDescending(
                Ax.Where(_client!.GetAccessPoints(), x => x.Ssid == data.Ssid),
                x => x.SignalStrength));

        if (ap != null)
        {
            var n = DateTime.Now;
            var conName = $"{data.Ssid}-connection-{n.Year:D4}{n.Month:D2}{n.Day:D2}.{n.Hour:D2}{n.Minute:D2}{n.Second:D2}";
            _log.LogInformation("DefineWirelessProfile: Creating connection {ConnectionName}", conName);
            if (data.FileName != null)
                await ap.UpdatePwd(data.Password!, data.FileName, conName);
            else
                await ap.New(data.Password!, conName);
            _log.LogInformation("DefineWirelessProfile: Success");
        }
        else
        {
            _log.LogWarning("DefineWirelessProfile failed: Access point not found for Ssid={Ssid}", data.Ssid);
            throw new FaultException<ConnectionError>(new ConnectionError
            {
                Message = "Access point was not found. Try again later.",
                Reason = ConnectionErrorReason.AccessPointNotFound
            });
        }

        await WirelessProfilesService.AppendIfRequired(_client, _plumber, _env);
    }

    [ThrowsFaultException<WrongHostError>]
    [ThrowsFaultException<ConnectionError>]
    public async Task Handle(HostName hostName, ConnectAccessPoint cmd)
    {
        _log.LogInformation("ConnectAccessPoint: Ssid={Ssid}", cmd.Ssid);

        await Prepare(hostName);

        if (await Ax.AnyAsync(
                Ax.OfType<WifiProfileSettings>(Ax.Select(_client!.GetProfiles(), x => x.Settings())),
                x => x.Ssid == cmd.Ssid))
        {
            var dev = await Ax.FirstOrDefaultAsync(Ax.OfType<WifiDeviceInfo>(_client.GetDevices()));
            try
            {
                await dev!.ConnectAccessPoint(cmd.Ssid!);
                _log.LogInformation("ConnectAccessPoint: Success for Ssid={Ssid}", cmd.Ssid);
            }
            catch (ActivationFailedException ex)
            {
                _log.LogError(ex, "ConnectAccessPoint failed: Reason={Reason}, ReasonName={ReasonName}", (uint)ex.Reason, ex.Reason.ToString());
                throw new FaultException<ConnectionError>(new ConnectionError
                {
                    Message = ex.Message,
                    Reason = MapReason(ex.Reason),
                    ProfileFileName = ex.ProfileFileName
                });
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "ConnectAccessPoint failed: {Message}", ex.Message);
                throw new FaultException<ConnectionError>(new ConnectionError
                {
                    Message = ex.Message,
                    Reason = ConnectionErrorReason.Unknown
                });
            }
        }
        else
        {
            _log.LogWarning("ConnectAccessPoint failed: No profile for Ssid={Ssid}", cmd.Ssid);
            throw new FaultException<ConnectionError>(new ConnectionError
            {
                Message = $"No profile defined for {cmd.Ssid} ssid.",
                Reason = ConnectionErrorReason.MissingProfile
            });
        }

        await WirelessProfilesService.AppendIfRequired(_client, _plumber, _env);
        await WirelessConnectivityService.Append(_client, _plumber, _env);
    }

    private ConnectionErrorReason MapReason(ActivationFailedReason exReason)
    {
        return exReason switch
        {
            ActivationFailedReason.IpConfigInvalid => ConnectionErrorReason.IpConfigInvalid,
            ActivationFailedReason.ConnectTimeout or ActivationFailedReason.ServiceStartTimeout => ConnectionErrorReason.ConnectTimeout,
            ActivationFailedReason.NoSecrets => ConnectionErrorReason.NoSecrets,
            ActivationFailedReason.LoginFailed => ConnectionErrorReason.LoginFailed,
            ActivationFailedReason.DeviceDisconnected => ConnectionErrorReason.DeviceDisconnected,
            _ => ConnectionErrorReason.Unknown
        };
    }

    [ThrowsFaultException<WrongHostError>]
    public async Task Handle(HostName hostName, RequestWifiScan cmd)
    {
        _log.LogInformation("RequestWifiScan: Starting");

        await Prepare(hostName);

        _ = Task.Run(async () =>
        {
            try
            {
                await using var c = await NetworkManagerClient.Create();
                await c.RequestWifiScan();
                await WirelessStationService.AppendIfRequired(c, _plumber, _env);
                await WirelessProfilesService.AppendIfRequired(c, _plumber, _env);
                await WirelessConnectivityService.Append(c, _plumber, _env);
                _log.LogInformation("RequestWifiScan: Completed");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "RequestWifiScan failed: {Message}", ex.Message);
            }
        });

        _log.LogInformation("RequestWifiScan: Request queued");
    }

    [ThrowsFaultException<WrongHostError>]
    [ThrowsFaultException<ProfileNotFound>]
    public async Task Handle(HostName hostName, DeactivateWirelessProfile profile)
    {
        _log.LogInformation("DeactivateWirelessProfile: ProfileId={ProfileId}", profile.ProfileId);

        await Prepare(hostName);

        var p = await GetProfileById(profile.ProfileId);
        await p!.Deactivate();

        _log.LogInformation("DeactivateWirelessProfile: Success");
    }

    [ThrowsFaultException<WrongHostError>]
    [ThrowsFaultException<ProfileNotFound>]
    public async Task Handle(HostName hostName, DisconnectWirelessNetwork profile)
    {
        _log.LogInformation("DisconnectWirelessNetwork: ProfileId={ProfileId}", profile.ProfileId);

        await Prepare(hostName);

        var p = await GetProfileById(profile.ProfileId);

        await foreach (var i in Ax.OfType<WifiDeviceInfo>(_client!.GetDevices()))
        {
            var con = await i.GetConnectionProfileId();
            if (con != p!.Id) continue;
            await i.DisconnectAsync();

            await WirelessProfilesService.AppendIfRequired(_client, _plumber, _env);
            await WirelessConnectivityService.Append(_client, _plumber, _env);
            _log.LogInformation("DisconnectWirelessNetwork: Success");
            break;
        }
    }

    [ThrowsFaultException<WrongHostError>]
    [ThrowsFaultException<ProfileNotFound>]
    [ThrowsFaultException<ConnectionError>]
    public async Task Handle(HostName hostName, ActivateWirelessProfile profile)
    {
        _log.LogInformation("ActivateWirelessProfile: ProfileId={ProfileId}", profile.ProfileId);

        await Prepare(hostName);

        var p = await GetProfileById(profile.ProfileId);

        await p!.Activate();
        await WirelessProfilesService.AppendIfRequired(_client!, _plumber, _env);
        await WirelessConnectivityService.Append(_client!, _plumber, _env);

        _log.LogInformation("ActivateWirelessProfile: Success");
    }

    [ThrowsFaultException<WrongHostError>]
    [ThrowsFaultException<ProfileNotFound>]
    public async Task Handle(HostName hostName, DeleteWirelessProfile profile)
    {
        _log.LogInformation("DeleteWirelessProfile: ProfileId={ProfileId}", profile.ProfileId);

        await Prepare(hostName);

        var p = await GetProfileById(profile.ProfileId);

        await p!.Delete();

        await WirelessProfilesService.AppendIfRequired(_client!, _plumber, _env);

        _log.LogInformation("DeleteWirelessProfile: Success");
    }

    private async Task<ProfileInfo?> GetProfileById(Guid profileId)
    {
        var p = await Ax.FirstOrDefaultAsync(_client!.GetProfiles(), x => x.FileId == profileId);
        if (p == null)
        {
            _log.LogWarning("Profile not found: ProfileId={ProfileId}", profileId);
            throw new FaultException<ProfileNotFound>(profileId);
        }
        return p;
    }

    private async Task Prepare(HostName hostName)
    {
        if (hostName != _env.HostName)
        {
            _log.LogWarning("Wrong host: Expected={Expected}, Received={Received}", _env.HostName, hostName);
            throw new FaultException<WrongHostError>(WrongHostError.Error);
        }
        _client ??= await NetworkManagerClient.Create();
    }

    [ThrowsFaultException<WrongHostError>]
    [ThrowsFaultException<ConnectionError>]
    public async Task Handle(HostName hostName, ConfigureEthernetConnection cmd)
    {
        _log.LogInformation("ConfigureEthernetConnection: Interface={Interface}, Method={Method}, IpConfig={IpConfig}, Mtu={Mtu}",
            cmd.InterfaceName, cmd.Method, cmd.IpConfig, cmd.Mtu);

        await Prepare(hostName);

        var device = await Ax.FirstOrDefaultAsync(
            Ax.Where(_client!.GetDevices(), x => x.InterfaceName == cmd.InterfaceName && x.DeviceType == DeviceType.Ethernet));

        if (device == null)
        {
            _log.LogWarning("ConfigureEthernetConnection failed: Interface not found: {Interface}", cmd.InterfaceName);
            throw new FaultException<ConnectionError>(new ConnectionError
            {
                Message = $"Ethernet interface '{cmd.InterfaceName}' not found.",
                Reason = ConnectionErrorReason.Unknown
            });
        }

        var profile = await device.GetConnectionProfile();
        if (profile == null)
        {
            _log.LogWarning("ConfigureEthernetConnection failed: No profile for {Interface}", cmd.InterfaceName);
            throw new FaultException<ConnectionError>(new ConnectionError
            {
                Message = $"No connection profile found for '{cmd.InterfaceName}'.",
                Reason = ConnectionErrorReason.MissingProfile
            });
        }

        try
        {
            var settings = await profile.GetConnectionSettings();
            var newSettings = BuildEthernetSettings(settings, cmd);

            await profile.UpdateConnectionSettings(newSettings);

            // Reactivate connection to apply changes
            await _client.ReactivateConnection(profile.Id, device.Id);

            // Append updated state so UI receives the changes via EventStore
            await EthernetConnectionService.AppendIfRequired(_client, _plumber, _env, cmd.InterfaceName!, default);

            _log.LogInformation("ConfigureEthernetConnection: Success for {Interface}", cmd.InterfaceName);
        }
        catch (Exception ex) when (ex is not FaultException<ConnectionError>)
        {
            _log.LogError(ex, "ConfigureEthernetConnection failed: {Message}", ex.Message);
            throw new FaultException<ConnectionError>(new ConnectionError
            {
                Message = ex.Message,
                Reason = ConnectionErrorReason.Unknown
            });
        }
    }

    [ThrowsFaultException<WrongHostError>]
    [ThrowsFaultException<ConnectionError>]
    public async Task Handle(HostName hostName, ConfigureDns cmd)
    {
        _log.LogInformation("ConfigureDns: Interface={Interface}, Method={Method}, Primary={Primary}, Secondary={Secondary}",
            cmd.InterfaceName, cmd.Method, cmd.PrimaryDns, cmd.SecondaryDns);

        await Prepare(hostName);

        var device = await Ax.FirstOrDefaultAsync(
            Ax.Where(_client!.GetDevices(), x => x.InterfaceName == cmd.InterfaceName));

        if (device == null)
        {
            _log.LogWarning("ConfigureDns failed: Interface not found: {Interface}", cmd.InterfaceName);
            throw new FaultException<ConnectionError>(new ConnectionError
            {
                Message = $"Interface '{cmd.InterfaceName}' not found.",
                Reason = ConnectionErrorReason.Unknown
            });
        }

        var profile = await device.GetConnectionProfile();
        if (profile == null)
        {
            _log.LogWarning("ConfigureDns failed: No profile for {Interface}", cmd.InterfaceName);
            throw new FaultException<ConnectionError>(new ConnectionError
            {
                Message = $"No connection profile found for '{cmd.InterfaceName}'.",
                Reason = ConnectionErrorReason.MissingProfile
            });
        }

        try
        {
            var settings = await profile.GetConnectionSettings();
            var newSettings = BuildDnsSettings(settings, cmd);

            await profile.UpdateConnectionSettings(newSettings);

            // Reactivate connection to apply changes
            await _client.ReactivateConnection(profile.Id, device.Id);

            // Append updated state so UI receives the changes via EventStore
            await EthernetConnectionService.AppendIfRequired(_client, _plumber, _env, cmd.InterfaceName!, default);

            _log.LogInformation("ConfigureDns: Success for {Interface}", cmd.InterfaceName);
        }
        catch (Exception ex) when (ex is not FaultException<ConnectionError>)
        {
            _log.LogError(ex, "ConfigureDns failed: {Message}", ex.Message);
            throw new FaultException<ConnectionError>(new ConnectionError
            {
                Message = ex.Message,
                Reason = ConnectionErrorReason.Unknown
            });
        }
    }

    private static Dictionary<string, Dictionary<string, Variant>> BuildEthernetSettings(
        Dictionary<string, Dictionary<string, VariantValue>> currentSettings,
        ConfigureEthernetConnection cmd)
    {
        var settings = new Dictionary<string, Dictionary<string, Variant>>();

        // Copy connection section
        if (currentSettings.TryGetValue("connection", out var connSection))
        {
            var conn = new Dictionary<string, Variant>();
            if (connSection.TryGetValue("id", out var id)) conn["id"] = new Variant(id.GetString());
            if (connSection.TryGetValue("type", out var type)) conn["type"] = new Variant(type.GetString());
            if (connSection.TryGetValue("uuid", out var uuid)) conn["uuid"] = new Variant(uuid.GetString());
            conn["interface-name"] = new Variant(cmd.InterfaceName!);
            settings["connection"] = conn;
        }

        // Build ipv4 section
        var ipv4 = new Dictionary<string, Variant>();
        if (cmd.Method == Ip4Method.Auto)
        {
            ipv4["method"] = new Variant("auto");
        }
        else
        {
            ipv4["method"] = new Variant("manual");

            if (cmd.IpConfig.HasValue)
            {
                var config = cmd.IpConfig.Value;
                // Build address-data array: aa{sv} - array of dictionaries with address/prefix
                var addressEntry = new Dictionary<string, Variant>
                {
                    ["address"] = new Variant(config.Address.ToString()),
                    ["prefix"] = new Variant((uint)config.PrefixLength)
                };
                ipv4["address-data"] = Variant.FromArray(new Array<Dictionary<string, Variant>>(new[] { addressEntry }));
                ipv4["gateway"] = new Variant(config.Gateway.ToString());
            }
        }

        settings["ipv4"] = ipv4;

        // Build 802-3-ethernet section for MTU
        var ethernet = new Dictionary<string, Variant>();
        if (cmd.Mtu.HasValue && cmd.Mtu.Value > 0)
        {
            ethernet["mtu"] = new Variant(cmd.Mtu.Value);
        }
        if (ethernet.Count > 0)
        {
            settings["802-3-ethernet"] = ethernet;
        }

        return settings;
    }

    private static Dictionary<string, Dictionary<string, Variant>> BuildDnsSettings(
        Dictionary<string, Dictionary<string, VariantValue>> currentSettings,
        ConfigureDns cmd)
    {
        var settings = new Dictionary<string, Dictionary<string, Variant>>();

        // Copy connection section
        if (currentSettings.TryGetValue("connection", out var connSection))
        {
            var conn = new Dictionary<string, Variant>();
            if (connSection.TryGetValue("id", out var id)) conn["id"] = new Variant(id.GetString());
            if (connSection.TryGetValue("type", out var type)) conn["type"] = new Variant(type.GetString());
            if (connSection.TryGetValue("uuid", out var uuid)) conn["uuid"] = new Variant(uuid.GetString());
            if (connSection.TryGetValue("interface-name", out var iface)) conn["interface-name"] = new Variant(iface.GetString());
            settings["connection"] = conn;
        }

        // Build ipv4 section - preserve existing IP settings, update DNS
        var ipv4 = new Dictionary<string, Variant>();

        if (currentSettings.TryGetValue("ipv4", out var currentIpv4))
        {
            // Preserve method
            if (currentIpv4.TryGetValue("method", out var method))
                ipv4["method"] = new Variant(method.GetString());
            // Preserve gateway
            if (currentIpv4.TryGetValue("gateway", out var gw))
            {
                var gwStr = gw.GetString();
                if (!string.IsNullOrEmpty(gwStr))
                    ipv4["gateway"] = new Variant(gwStr);
            }
        }

        // Set DNS
        if (cmd.Method == DnsMethod.Auto)
        {
            ipv4["ignore-auto-dns"] = new Variant(false);
        }
        else
        {
            ipv4["ignore-auto-dns"] = new Variant(true);

            var dnsList = new List<uint>();
            if (cmd.PrimaryDns.HasValue)
                dnsList.Add(cmd.PrimaryDns.Value.Value);
            if (cmd.SecondaryDns.HasValue)
                dnsList.Add(cmd.SecondaryDns.Value.Value);

            if (dnsList.Count > 0)
                ipv4["dns"] = Variant.FromArray(new Array<uint>(dnsList.ToArray()));
        }

        settings["ipv4"] = ipv4;

        return settings;
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null) await _client.DisposeAsync();
    }
}
