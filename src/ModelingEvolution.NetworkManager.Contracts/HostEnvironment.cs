using Microsoft.Extensions.Configuration;

namespace ModelingEvolution.NetworkManager.Contracts;

class HostEnvironment(IConfiguration config) : IEnvironment
{
    private readonly HostName _host = (HostName)(config.GetValue<string>("HostName") ?? Environment.MachineName);

    public HostName HostName => _host;
}
