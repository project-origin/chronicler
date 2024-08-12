using Grpc.Net.Client;

namespace ProjectOrigin.Chronicler.Server;

public interface IRegistryClientFactory
{
    GrpcChannel GetChannel(string registryName);
    Registry.V1.RegistryService.RegistryServiceClient GetClient(string registryName);
}
