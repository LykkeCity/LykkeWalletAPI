using System;
using Core.Exchange;
using Microsoft.WindowsAzure.Storage.Table;

namespace LkeServices.Exchange
{
    public class ExchangeSettingsEntity : TableEntity, IExchangeSettings 
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string ETag { get; set; }
        public string BaseAssetIos { get; }
        public string BaseAssetOther { get; }
        public bool SignOrder { get; }

        public string GeneratePartitionKey()
        {
            return "ExchngSettings";
        }

        public string GenerateRowKey(string clientId)
        {
            return clientId;
        }
    }
}