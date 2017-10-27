namespace LykkeApi2.Models.ClientBalancesModels
{
    public class WalletAssetBalanceModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public ClientBalanceResponseModel Balances { get; set; }
    }
}
