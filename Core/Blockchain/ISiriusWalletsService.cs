using System.Threading.Tasks;
using Swisschain.Sirius.Api.ApiContract.Account;

namespace Core.Blockchain
{
    public interface ISiriusWalletsService
    {
        Task CreateWalletsAsync(string clientId, bool waitForCreation);
        Task<AccountDetailsResponse> GetWalletAdderssAsync(string clientId, long assetId);
    }
}
