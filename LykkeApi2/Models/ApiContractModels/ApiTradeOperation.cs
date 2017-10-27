using Core.Enumerators;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LykkeApi2.Models.ApiContractModels
{
    public class ApiTradeOperation
    {
        public string Id { get; set; }
        public string DateTime { get; set; }
        public string Asset { get; set; }
        public double Volume { get; set; }
        public string IconId { get; set; }
        public string BlockChainHash { get; set; }
        public string AddressFrom { get; set; }
        public string AddressTo { get; set; }
        public bool IsSettled { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public TransactionStates State { get; set; }

        public ApiMarketOrder MarketOrder { get; set; }

        public string OrderId { get; set; }
        public bool IsLimitTrade { get; set; }

        public string ClientId { get; set; }
        public string TransactionId { get; set; }
    }
}
