using Microsoft.Extensions.DependencyInjection;

namespace ModelingEvolution.NetworkManager.Contracts;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNetworkManagerEnvironment(this IServiceCollection services) =>
        services.AddSingleton<IEnvironment, HostEnvironment>();
}
