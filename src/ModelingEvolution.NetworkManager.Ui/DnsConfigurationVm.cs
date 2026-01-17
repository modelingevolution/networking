using CommunityToolkit.Mvvm.ComponentModel;
using ModelingEvolution.Ipv4;
using ModelingEvolution.NetworkManager.Contracts;
using MicroPlumberd;

namespace ModelingEvolution.NetworkManager.Ui;

[EventHandler]
[INotifyPropertyChanged]
internal partial class DnsConfigurationVm(IPlumber plumber, ICommandBus commandBus) : IAsyncDisposable
{
    private CancellationTokenSource _cts = new();
    private EthernetConnectionState _state = new();
    private HostName _hostName;
    private string? _interfaceName;

    // Current state (shares with Ethernet)
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

    private DnsMethod _editMethod;
    public DnsMethod EditMethod
    {
        get => _editMethod;
        set => SetProperty(ref _editMethod, value);
    }

    private string _editPrimaryDns = "";
    public string EditPrimaryDns
    {
        get => _editPrimaryDns;
        set => SetProperty(ref _editPrimaryDns, value);
    }

    private string _editSecondaryDns = "";
    public string EditSecondaryDns
    {
        get => _editSecondaryDns;
        set => SetProperty(ref _editSecondaryDns, value);
    }

    public void StartEdit()
    {
        EditMethod = State.DnsMethod;
        EditPrimaryDns = State.PrimaryDns?.ToString() ?? "";
        EditSecondaryDns = State.SecondaryDns?.ToString() ?? "";
        IsEditing = true;
    }

    public void CancelEdit()
    {
        IsEditing = false;
    }

    public async Task SaveEdit()
    {
        var cmd = new ConfigureDns
        {
            InterfaceName = _interfaceName, // Use tracked interface, not State which may be empty
            Method = EditMethod
        };

        if (EditMethod == DnsMethod.Manual)
        {
            if (Ipv4Address.TryParse(EditPrimaryDns, null, out var primary))
                cmd.PrimaryDns = primary;
            if (Ipv4Address.TryParse(EditSecondaryDns, null, out var secondary))
                cmd.SecondaryDns = secondary;
        }

        await commandBus.SendAsync(_hostName, cmd);
        IsEditing = false;
    }

    private Task Given(Metadata m, EthernetConnectionState state)
    {
        State = state;
        return Task.CompletedTask;
    }

    public async Task<DnsConfigurationVm> Initialize(HostName hostName, string interfaceName)
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
