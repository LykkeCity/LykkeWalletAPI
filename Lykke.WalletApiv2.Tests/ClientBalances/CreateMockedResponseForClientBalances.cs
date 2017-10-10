using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.Balances.AutorestClient.Models;
using Lykke.Service.Balances.Client.ResponseModels;

namespace Lykke.WalletApiv2.Tests.ClientBalances
{
    public static class CreateMockedResponseForClientBalances
    {
        public static Task<IEnumerable<ClientBalanceResponseModel>> GetAllBalancesForClient()
        {
            var balances = new List<ClientBalanceResponseModel>
            {
                new ClientBalanceResponseModel()
                {
                    AssetId = "USD",
                    Balance = 1000
                },
                new ClientBalanceResponseModel()
                {
                    AssetId = "BTC",
                    Balance = 1000
                },
                new ClientBalanceResponseModel()
                {
                    AssetId = "EUR",
                    Balance = 1000
                }
            };

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
