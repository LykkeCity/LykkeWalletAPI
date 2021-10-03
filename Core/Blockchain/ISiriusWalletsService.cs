using System.Collections.Generic;
using System.Threading.Tasks;
using Swisschain.Sirius.Api.ApiContract.Account;
using Swisschain.Sirius.Api.ApiContract.Blockchain;

namespace Core.Blockchain
{
    public interface ISiriusWalletsService
    {
        Task CreateWalletsAsync(string clientId);
        Task<AccountDetailsResponse> GetWalletAdderssAsync(string clientId, long assetId);
        Task<bool> IsAddressValidAsync(string blockchainId, string address);
        Task<List<BlockchainResponse>> GetBlockchainsAsync();
    }
}
