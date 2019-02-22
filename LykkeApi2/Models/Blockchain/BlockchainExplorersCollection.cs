using System.Collections.Generic;

namespace LykkeApi2.Models.Blockchain
{
    public class BlockchainExplorersCollection
    {
        public IEnumerable<BlockchainExplorerLinkResponse> Links { get; set; }
    }

    public struct BlockchainExplorerLinkResponse
    {
        public string Url { get; set; }

        public string Name { get; set; }
    }
}
