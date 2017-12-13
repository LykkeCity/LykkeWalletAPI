using Lykke.Service.OperationsRepository.Contract;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LykkeApi2.Models.Operations
{
    public class ApiTrade
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

        public string LimitOrderId { get; set; }
        public string MarketOrderId { get; set; }
    }
}
