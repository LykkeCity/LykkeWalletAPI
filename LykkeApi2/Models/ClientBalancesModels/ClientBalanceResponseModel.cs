
using Lykke.Service.Wallets.Client.ResponseModels;

namespace LykkeApi2.Models.ClientBalancesModels
{
    public class ClientBalanceResponseModel
    {
        public string AssetId { get; set; }
        public double? Balance { get; set; }

        public static ClientBalanceResponseModel Create(ClientBalanceModel src)
        {
            return new ClientBalanceResponseModel()
            {
                AssetId = src.AssetId,
                Balance = src.Balance,
            };
        }
    }
}
