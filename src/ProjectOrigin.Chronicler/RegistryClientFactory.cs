using System.Collections.Concurrent;
using System.Net.Http;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using ProjectOrigin.Chronicler.Exceptions;
using ProjectOrigin.Chronicler.Options;
using static ProjectOrigin.Registry.V1.RegistryService;

namespace ProjectOrigin.Chronicler;

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
                if (!_optionsMonitor.CurrentValue.Registries.TryGetValue(name, out var info))
                    throw new RegistryNotKnownException($"Registry {name} not found in configuration");

                return GrpcChannel.ForAddress(info.Url, new GrpcChannelOptions
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
            if (options.Registries.TryGetValue(record.Key, out var info)
                && record.Value.Target != info.Url
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
