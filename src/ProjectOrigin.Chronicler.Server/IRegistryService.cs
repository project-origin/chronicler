using System.Threading.Tasks;

namespace ProjectOrigin.Chronicler.Server;

public interface IRegistryService
{
    Task<Registry.V1.Block?> GetNextBlock(string registryName, int previousBlockHeight);
}
