using Core.Mappers;
using System.Collections.Generic;

namespace LykkeApi2.Mappers
{
    public class HistoryOperationMapProvider : IHistoryOperationMapProvider
    {
        public IDictionary<string, string> Cash => new Dictionary<string, string>
        {
            {"Asset", "AssetId"}
        };

        public IDictionary<string, string> CashOutAttempt => new Dictionary<string, string>
        {
            {"Asset", "AssetId"},
            {"Volume", "Amount"}
        };

        public IDictionary<string, string> ClientTrade => new Dictionary<string, string>
        {
            {"Asset", "AssetId"},
            {"Volume", "Amount"}
        };

        public IDictionary<string, string> TransferEvent => new Dictionary<string, string>
        {
            {"Asset", "AssetId"},
            {"Volume", "Amount"}
        };

        public IDictionary<string, string> Default => new Dictionary<string, string>
        {
            {"Asset", "AssetId"},
            {"Volume", "Amount"}
        };
    }
}
