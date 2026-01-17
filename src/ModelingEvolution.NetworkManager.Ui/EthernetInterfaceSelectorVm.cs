using CommunityToolkit.Mvvm.ComponentModel;
using ModelingEvolution.NetworkManager.Contracts;
using MicroPlumberd;

namespace ModelingEvolution.NetworkManager.Ui;

[EventHandler]
[INotifyPropertyChanged]
internal partial class EthernetInterfaceSelectorVm(IPlumber plumber) : IAsyncDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private EthernetInterfacesState _state = new();
    private bool _isInitialized;
    private HostName _hostName;

    public EthernetInterfacesState State
    {
        get => _state;
        private set => SetProperty(ref _state, value);
    }

    private string? _selectedInterface;
    public string? SelectedInterface
    {
        get => _selectedInterface;
        set
        {
            if (SetProperty(ref _selectedInterface, value))
            {
                SelectedInterfaceChanged?.Invoke(this, value);
            }
        }
    }

    public event EventHandler<string?>? SelectedInterfaceChanged;

    public EthernetInterface[] Interfaces => State.Interfaces;

    private Task Given(Metadata m, EthernetInterfacesState state)
    {
        State = state;

        // Auto-select first connected interface if nothing selected
        if (string.IsNullOrEmpty(SelectedInterface) && state.Interfaces.Length > 0)
        {
            var connected = state.Interfaces.FirstOrDefault(x => x.Carrier);
            SelectedInterface = connected?.InterfaceName ?? state.Interfaces[0].InterfaceName;
        }

        return Task.CompletedTask;
    }

    public async Task<EthernetInterfaceSelectorVm> Initialize(HostName hostName)
    {
        if (_isInitialized) return this;
        _isInitialized = true;
        _hostName = hostName;

        var fullSn = EthernetInterfacesState.FullStreamName(hostName);
        await plumber.Subscribe(fullSn, FromRelativeStreamPosition.End - 1, cancellationToken: _cts.Token)
            .WithSnapshotHandler(this);
        return this;
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
    }
}
