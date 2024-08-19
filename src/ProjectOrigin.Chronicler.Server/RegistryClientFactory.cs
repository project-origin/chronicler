using System.Collections.Concurrent;
using System.Net.Http;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using ProjectOrigin.Chronicler.Server.Exceptions;
using ProjectOrigin.Chronicler.Server.Options;
using static ProjectOrigin.Registry.V1.RegistryService;

namespace ProjectOrigin.Chronicler.Server;

public class RegistryClientFactory : IRegistryClientFactory
{
    private readonly ConcurrentDictionary<string, GrpcChannel> _registries = new ConcurrentDictionary<string, GrpcChannel>();
    private readonly IOptionsMonitor<NetworkOptions> _optionsMonitor;

    public RegistryClientFactory(IOptionsMonitor<NetworkOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
        _optionsMonitor.OnChange(UpdateNetworkOptions);
    }
    public GrpcChannel GetChannel(string registryName)
    {
        return _registries.GetOrAdd(
            registryName,
            (name) =>
            {
                if (!_optionsMonitor.CurrentValue.RegistryUrls.TryGetValue(name, out var address))
                    throw new RegistryNotKnownException($"Registry {name} not found in configuration");

                return GrpcChannel.ForAddress(address, new GrpcChannelOptions
                {
                    HttpHandler = new SocketsHttpHandler
                    {
                        EnableMultipleHttp2Connections = true,
                    }
                });
            });
    }

    public RegistryServiceClient GetClient(string registryName)
    {
        return new RegistryServiceClient(GetChannel(registryName));
    }

    private void UpdateNetworkOptions(NetworkOptions options, string? _)
    {
        foreach (var record in _registries)
        {
            // If the registry is still in the configuration, and the address has changed, we need to recreate the channel
            if (options.RegistryUrls.TryGetValue(record.Key, out var uri)
                && record.Value.Target != uri
                && _registries.TryRemove(record.Key, out var oldChannel))
            {
                oldChannel.ShutdownAsync().Wait();
            }
            // If the registry is no longer in the configuration, we need to remove the channel
            else if (_registries.TryRemove(record.Key, out var removedChannel))
            {
                removedChannel.ShutdownAsync().Wait();
            }
        }
    }
}
