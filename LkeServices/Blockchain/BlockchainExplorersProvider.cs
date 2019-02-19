using Core.Blockchain;
using Lykke.HttpClientGenerator.Caching;
using Lykke.Service.BlockchainSettings.Client;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LkeServices.Blockchain
{
    public class BlockchainExplorersProvider : IBlockchainExplorersProvider
    {
        private readonly IBlockchainSettingsClient _client;
        private readonly IClientCacheManager _clientCacheManager; //Required to Invalidate Cache on request

        public BlockchainExplorersProvider(Lykke.Service.BlockchainSettings.Client.IBlockchainSettingsClient client,
            IClientCacheManager clientCacheManager)
        {
            _client = client;
            _clientCacheManager = clientCacheManager;
        }

        public async Task<IEnumerable<BlockchainExplorerLink>> GetAsync(string blockchainType, string txHash)
        {
            var explorers = await _client.GetBlockchainExplorerByTypeAsync(blockchainType);

            if (explorers?.Collection == null || !explorers.Collection.Any())
                return Enumerable.Empty<BlockchainExplorerLink>();

            var mapped = explorers
                .Collection
                .Select(x =>
                {
                    var explorer = new BlockchainExplorerLink()
                    {
                        ExplorerUrlTemplateFormatted = x
                            .ExplorerUrlTemplate
                            ?.Replace(Lykke.Service.BlockchainSettings.Contract.Constants.TxHashTemplate, txHash)
                    };

                    return explorer;
                });

            return mapped;
        }
    }
}
