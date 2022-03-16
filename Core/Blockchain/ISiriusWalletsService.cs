using System.Collections.Generic;
using System.Threading.Tasks;
using Swisschain.Sirius.Api.ApiContract.Account;
using Swisschain.Sirius.Api.ApiContract.Asset;
using Swisschain.Sirius.Api.ApiContract.WhitelistItems;

namespace Core.Blockchain
{
    public interface ISiriusWalletsService
    {
        Task CreateWalletsAsync(string clientId);
        Task<AccountDetailsResponse> GetWalletAdderssAsync(string clientId, long assetId);
        Task<bool> IsAddressValidAsync(string blockchainId, string address);
        Task<List<Swisschain.Sirius.Api.ApiContract.Blockchain.Blockchain>> GetBlockchainsAsync();
        Task<AccountSearchResponse> SearchAccountAsync(string clientId, string walletId = null);
        Task<AssetResponse> GetAssetByIdAsync(long assetId);
        Task<WhitelistItemCreateResponse> CreateWhitelistItemAsync(WhitelistItemCreateRequest request);
        Task<List<WhitelistItemResponse>> GetWhitelistItemsAsync(long accountId);
        Task<WhitelistItemDeleteResponse> DeleteWhitelistItemsAsync(long id);
    }
}
