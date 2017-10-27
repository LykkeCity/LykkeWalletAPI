using System.Collections.Generic;

namespace LykkeApi2.Models.ClientBalancesModels
{
    public class WalletBalancesModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public IEnumerable<ClientBalanceResponseModel> Balances { get; set; }
    }
}
