using Lykke.Service.Balances.Client.ResponseModels;

namespace LykkeApi2.Models.ClientBalancesModels
{
    public class ClientBalanceResponseModel
    {
        public string AssetId { get; set; }
        public decimal? Balance { get; set; }
        public decimal? Reserved { get; set; }

        public static ClientBalanceResponseModel Create(ClientBalanceModel src)
        {
            return new ClientBalanceResponseModel
            {
                AssetId = src.AssetId,
                Balance = src.Balance,
                Reserved = src.Reserved
            };
        }

        public static ClientBalanceResponseModel Create(Lykke.Service.Balances.AutorestClient.Models.ClientBalanceResponseModel src)
        {
            return new ClientBalanceResponseModel
            {
                AssetId = src.AssetId,
                Balance = src.Balance,
                Reserved = src.Reserved
            };
        }
    }
}
