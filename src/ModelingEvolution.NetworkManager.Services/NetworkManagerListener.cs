using System.Threading.Channels;
using ModelingEvolution.NetworkManager;
using ModelingEvolution.NetworkManager.Contracts;
using MicroPlumberd;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ModelingEvolution.NetworkManager.Services;

internal class NetworkManagerListener(IPlumberInstance plumber, IEnvironment env, ILogger<NetworkManagerListener> log) : BackgroundService
{
    private NetworkManagerClient? _client;
    private Channel<Func<CancellationToken, Task>>? _channel;
    private CancellationTokenSource? _cts;
    private Disposables _d = new();

    // Exclusion patterns for virtual interfaces
    private static readonly string[] VirtualInterfacePrefixes = ["veth", "docker", "br-", "virbr"];

    private static bool IsPhysicalInterface(string? name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (name == "lo") return false;
        return !VirtualInterfacePrefixes.Any(prefix => name.StartsWith(prefix));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            log.LogInformation("Starting NetworkManager listener");
            _channel = Channel.CreateBounded<Func<CancellationToken, Task>>(new BoundedChannelOptions(5)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
            _client = await NetworkManagerClient.Create();
            await AppendStations(stoppingToken);
            await AppendWifiProfiles(stoppingToken);
            await AppendConnectivity(token: stoppingToken);
            await AppendEthernetInterfaces(stoppingToken);
            await AppendEthernetConnections(stoppingToken);

            // Subscribe to WiFi device events
            var wifis = await Ax.ToArrayAsync(Ax.OfType<WifiDeviceInfo>(_client.GetDevices()));
            foreach (var i in wifis)
            {
                i.StateChanged += OnWifiDeviceStateChanged;
                i.AccessPointVisilibityChanged += OnWifiAccessPointChanged;
                i.AccessPointSignalChanged += OnWifiSignalChanged;
                _d += await i.SubscribeStateChanged();
                _d += await i.SubscribeAccessPoint();
            }

            // Subscribe to Ethernet device events (reactive updates when state changes)
            var ethernetDevices = await Ax.ToArrayAsync(
                Ax.Where(_client.GetDevices(),
                    x => x.DeviceType == DeviceType.Ethernet && IsPhysicalInterface(x.InterfaceName)));
            foreach (var device in ethernetDevices)
            {
                device.StateChanged += OnEthernetDeviceStateChanged;
                _d += await device.SubscribeStateChanged();
                log.LogInformation("Subscribed to Ethernet state changes for {Interface}.", device.InterfaceName);
            }

            await Task.Factory.StartNew(OnStateAppender, TaskCreationOptions.LongRunning);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Network monitor failed to start.");
        }
    }

    private void OnWifiSignalChanged(object? sender, AccessPointPropertyChangedArgs e)
    {
        _channel?.Writer.WriteAsync(AppendStations);
    }

    private void OnWifiAccessPointChanged(object? sender, AccessPointDiscoveryArgs e)
    {
        _channel?.Writer.WriteAsync(AppendStations);
    }

    private async Task OnStateAppender()
    {
        await Task.Delay(100);
        await foreach (var i in _channel!.Reader.ReadAllAsync(_cts!.Token))
            await i(_cts.Token);
    }

    private void OnWifiDeviceStateChanged(object? sender, DeviceStateEventArgs e)
    {
        var w = (WifiDeviceInfo)sender!;
        _channel?.Writer.WriteAsync(AppendStations);
        _channel?.Writer.WriteAsync(async token => await AppendConnectivity((DeviceState)e.NewState, w.Id, token));
        _channel?.Writer.WriteAsync(AppendWifiProfiles);
    }

    private void OnEthernetDeviceStateChanged(object? sender, DeviceStateEventArgs e)
    {
        var device = (DeviceInfo)sender!;
        log.LogInformation("Ethernet state changed: {Interface} -> {NewState}", device.InterfaceName, e.NewState);
        // Update both the connection state and the interfaces list
        _channel?.Writer.WriteAsync(async token => await AppendEthernetConnection(device.InterfaceName!, token));
        _channel?.Writer.WriteAsync(AppendEthernetInterfaces);
    }

    private async Task AppendEthernetConnection(string interfaceName, CancellationToken stoppingToken)
    {
        try
        {
            await EthernetConnectionService.AppendIfRequired(_client!, plumber, env, interfaceName, stoppingToken);
            log.LogInformation("Ethernet state updated for {Interface}.", interfaceName);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Failed to append ethernet state for {Interface}.", interfaceName);
        }
    }

    private async Task AppendEthernetInterfaces(CancellationToken stoppingToken)
    {
        if (await EthernetInterfacesService.AppendIfRequired(_client!, plumber, env, stoppingToken))
            log.LogInformation("Appending ethernet interfaces.");
    }

    public async Task AppendConnectivity(DeviceState? st = null, PathId? id = null, CancellationToken token = default)
    {
        await WirelessConnectivityService.Append(_client!, plumber, env, st, id, token);
        log.LogInformation("Appending wifi connectivity. ({State})", st);
    }

    private async Task AppendWifiProfiles(CancellationToken stoppingToken)
    {
        if (await WirelessProfilesService.AppendIfRequired(_client!, plumber, env, stoppingToken))
            log.LogInformation("Appending wifi profiles.");
    }

    private async Task AppendStations(CancellationToken stoppingToken)
    {
        if (await WirelessStationService.AppendIfRequired(_client!, plumber, env, stoppingToken))
            log.LogInformation("Appending wifi stations.");
    }

    private async Task AppendEthernetConnections(CancellationToken stoppingToken)
    {
        // Append state for physical Ethernet interfaces (skip virtual: veth, docker, br-, virbr, lo)
        await foreach (var device in _client!.GetDevices())
        {
            if (device.DeviceType == DeviceType.Ethernet &&
                device.State == DeviceState.Activated &&
                IsPhysicalInterface(device.InterfaceName))
            {
                try
                {
                    await EthernetConnectionService.AppendIfRequired(_client, plumber, env, device.InterfaceName!, stoppingToken);
                    log.LogInformation("Appending ethernet connection for {Interface}.", device.InterfaceName);
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "Failed to append ethernet state for {Interface}.", device.InterfaceName);
                }
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_client != null)
            await _client.DisposeAsync();
        await _d.DisposeAsync();
    }
}
