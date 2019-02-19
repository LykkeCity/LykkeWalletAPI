using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Blockchain
{
    public interface IBlockchainExplorersProvider
    {
        Task<IEnumerable<BlockchainExplorerLink>> GetAsync(string blockchainType, string hash);
    }
}
