using Lykke.Service.Wallets.Client.AutorestClient.Models;
using Lykke.Service.Wallets.Client.ResponseModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.WalletApiv2.Tests.ClientBalances
{
    public static class CreateMockedResponseForClientBalances
    {
        public static Task<IEnumerable<ClientBalanceResponseModel>> GetAllBalancesForClient()
        {
            var balances = new List<ClientBalanceResponseModel>();
            balances.Add(new ClientBalanceResponseModel()
            {
                AssetId = "USD",
                Balance = 1000
            });
            balances.Add(new ClientBalanceResponseModel()
            {
                AssetId = "BTC",
                Balance = 1000
            });
            balances.Add(new ClientBalanceResponseModel()
            {
                AssetId = "EUR",
                Balance = 1000
            });

            return Task.FromResult(balances.AsEnumerable());
        }

        public static Task<ClientBalanceModel> GetAllBalancesForClientByAssetId()
        {
            var balance = new ClientBalanceModel()
            {
                AssetId = "USD",
                Balance = 1000
            };        

            return Task.FromResult(balance);
        }
    }
}
