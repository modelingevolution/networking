using CommunityToolkit.Mvvm.ComponentModel;
using ModelingEvolution.Ipv4;
using ModelingEvolution.NetworkManager.Contracts;
using MicroPlumberd;

namespace ModelingEvolution.NetworkManager.Ui;

[EventHandler]
[INotifyPropertyChanged]
internal partial class EthernetConfigurationVm(IPlumber plumber, ICommandBus commandBus) : IAsyncDisposable
{
    private CancellationTokenSource _cts = new();
    private EthernetConnectionState _state = new();
    private HostName _hostName;
    private string? _interfaceName;

    // Current state
    public EthernetConnectionState State
    {
        get => _state;
        private set => SetProperty(ref _state, value);
    }

    // Edit mode fields
    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    private Ip4Method _editMethod;
    public Ip4Method EditMethod
    {
        get => _editMethod;
        set => SetProperty(ref _editMethod, value);
    }

    private string _editAddress = "";
    public string EditAddress
    {
        get => _editAddress;
        set => SetProperty(ref _editAddress, value);
    }

    private uint _editPrefix = 24;
    public uint EditPrefix
    {
        get => _editPrefix;
        set => SetProperty(ref _editPrefix, value);
    }

    private string _editGateway = "";
    public string EditGateway
    {
        get => _editGateway;
        set => SetProperty(ref _editGateway, value);
    }

    private uint _editMtu;
    public uint EditMtu
    {
        get => _editMtu;
        set => SetProperty(ref _editMtu, value);
    }

    public void StartEdit()
    {
        EditMethod = State.Ip4Method;
        EditAddress = State.IpConfig?.Address.ToString() ?? "";
        EditPrefix = (uint)(State.IpConfig?.PrefixLength ?? 24);
        EditGateway = State.IpConfig?.Gateway.ToString() ?? "";
        EditMtu = State.Mtu;
        IsEditing = true;
    }

    public void CancelEdit()
    {
        IsEditing = false;
    }

    public async Task SaveEdit()
    {
        try
        {
            var cmd = new ConfigureEthernetConnection
            {
                InterfaceName = _interfaceName, // Use tracked interface, not State which may be empty
                Method = EditMethod,
                Mtu = EditMtu > 0 ? EditMtu : null
            };

            if (EditMethod == Ip4Method.Manual)
            {
                if (Ipv4Address.TryParse(EditAddress, null, out var addr) &&
                    Ipv4Address.TryParse(EditGateway, null, out var gw))
                {
                    cmd.IpConfig = new Ipv4Configuration(addr, (int)EditPrefix, gw);
                }
            }

            Console.WriteLine($"[EthernetConfigurationVm] Sending ConfigureEthernetConnection: Host={_hostName}, Interface={_interfaceName}, Method={EditMethod}");
            await commandBus.SendAsync(_hostName, cmd);
            Console.WriteLine($"[EthernetConfigurationVm] Command sent successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EthernetConfigurationVm] Error: {ex}");
            throw;
        }
        finally
        {
            IsEditing = false;
        }
    }

    private Task Given(Metadata m, EthernetConnectionState state)
    {
        State = state;
        return Task.CompletedTask;
    }

    public async Task<EthernetConfigurationVm> Initialize(HostName hostName, string interfaceName)
    {
        // If same interface, no need to reinitialize
        if (_interfaceName == interfaceName && _hostName == hostName) return this;

        // Cancel previous subscription if any
        if (_interfaceName != null)
        {
            await _cts.CancelAsync();
            _cts = new CancellationTokenSource();
            State = new EthernetConnectionState(); // Reset state
        }

        _hostName = hostName;
        _interfaceName = interfaceName;

        // Subscribe to the interface-specific stream
        var connectionId = new EthernetConnectionId(hostName, interfaceName);
        var fullSn = EthernetConnectionState.FullStreamName(connectionId);
        await plumber.Subscribe(fullSn, FromRelativeStreamPosition.End - 1, cancellationToken: _cts.Token)
            .WithSnapshotHandler(this);
        return this;
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
    }
}
