namespace LykkeApi2.Models.Wallets
{
    public class CreateWalletRequest
    {
        public string ClientId { get; set; }        
        public string Type { get; set; }
        public string Name { get; set; }
    }
}