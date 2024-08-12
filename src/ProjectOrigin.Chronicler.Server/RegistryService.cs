using System.Linq;
using System.Threading.Tasks;

namespace ProjectOrigin.Chronicler.Server;

public class RegistryService : IRegistryService
{
    private readonly IRegistryClientFactory _registryClientFactory;

    public RegistryService(IRegistryClientFactory registryClientFactory)
    {
        _registryClientFactory = registryClientFactory;
    }

    public async Task<Registry.V1.Block?> GetNextBlock(string registryName, int previousBlockHeight)
    {
        var client = _registryClientFactory.GetClient(registryName);

        var response = await client.GetBlocksAsync(new Registry.V1.GetBlocksRequest
        {
            Skip = previousBlockHeight,
            Limit = 1,
            IncludeTransactions = true,
        });

        return response.Blocks.SingleOrDefault();
    }
}
