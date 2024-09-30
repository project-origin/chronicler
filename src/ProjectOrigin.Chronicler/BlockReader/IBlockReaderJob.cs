using System.Threading;
using System.Threading.Tasks;

namespace ProjectOrigin.Chronicler.BlockReader;

public interface IBlockReaderJob
{
    Task ProcessAllRegistries(CancellationToken cancellationToken);
    Task ProcessRegistryBlocks(string registryName, int previousBlockHeight, CancellationToken cancellationToken);
}
