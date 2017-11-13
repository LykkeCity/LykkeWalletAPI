using Lykke.Service.ClientAccount.Client.AutorestClient.Models;

namespace LykkeApi2.Models.Wallets
{
    public class CreateWalletRequest
    {
        public WalletType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}