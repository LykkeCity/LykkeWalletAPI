using Lykke.Service.OperationsHistory.AutorestClient.Models;

namespace LykkeApi2.Models.History
{
    public class RequestClientHistoryCsvRequestModel
    {
        public HistoryOperationType[] OperationType { set; get; }
        public string AssetId { set; get; }
        public string AssetPairId { set; get; }
    }
}