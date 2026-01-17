using System.Collections.Concurrent;
using ModelingEvolution.NetworkManager.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace ModelingEvolution.NetworkManager.Ui;

public static class ContainerExtensions
{
    public static IServiceCollection AddNetworkManagerUi(this IServiceCollection services) =>
        services
            .AddScoped(typeof(VmHostRegister<>))
            .AddTransient<WirelessStationsVm>()
            .AddTransient<WirelessProfilesVm>()
            .AddTransient<WirelessConnectionVm>()
            .AddTransient<EthernetConfigurationVm>()
            .AddTransient<DnsConfigurationVm>()
            .AddTransient<EthernetInterfaceSelectorVm>();
}

internal class VmHostRegister<T>(IServiceProvider serviceProvider)
{
    private readonly ConcurrentDictionary<HostName, T> _index = new();
    public T Get(HostName name) => _index.GetOrAdd(name, _ => serviceProvider.GetRequiredService<T>());
}
