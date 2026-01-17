using CommunityToolkit.Mvvm.ComponentModel;
using ModelingEvolution.NetworkManager.Contracts;
using MicroPlumberd;
using MudBlazor;
using Metadata = MicroPlumberd.Metadata;

namespace ModelingEvolution.NetworkManager.Ui;

[EventHandler]
[INotifyPropertyChanged]
internal partial class WirelessConnectionVm(IPlumber plumber, IDialogService dialogService) : IAsyncDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private WirelessConnectivityState _connectivity = new();
    private bool _isInitialized;

    public WirelessConnectivityState Connectivity
    {
        get => _connectivity;
        private set => SetProperty(ref _connectivity, value);
    }

    private async Task OnProcessPwd(WirelessConnectivityState st)
    {
        var parameters = new DialogParameters<WirelessStationPwdDialog>
        {
            { x => x.InterfaceName, st.InterfaceName },
            { x => x.Ssid, st.Ssid },
        };
        DialogOptions op = new() { BackgroundClass = "blur-background", MaxWidth = MaxWidth.Medium };
        await dialogService.ShowAsync<WirelessStationPwdDialog>("Wifi needs authentication", parameters, op);
    }

    public Func<Func<Task>, Task>? InvokeAsync { get; set; }

    private Task Given(Metadata m, WirelessConnectivityState state)
    {
        Connectivity = state;
        return Task.CompletedTask;
    }

    public async Task<WirelessConnectionVm> Initialize(HostName hostName)
    {
        if (_isInitialized) return this;
        _isInitialized = true;
        var fullSn = WirelessConnectivityState.FullStreamName(hostName);
        await plumber.Subscribe(fullSn, FromRelativeStreamPosition.End - 1, cancellationToken: _cts.Token)
            .WithSnapshotHandler(this);
        return this;
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
    }
}
