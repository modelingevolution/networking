using MicroPlumberd.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ModelingEvolution.NetworkManager.Services;

public static class ContainerExtensions
{
    public static IServiceCollection AddNetworkManagerServices(this IServiceCollection services)
    {
        services.AddBackgroundServiceIfMissing<NetworkManagerListener>();
        services.AddSingleton<NetworkManagerCommandHandler>();
        services.AddCommandHandler<NetworkManagerCommandHandler>();
        return services;
    }
}
