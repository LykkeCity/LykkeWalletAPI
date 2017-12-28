using Core.ExchangeSettings;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.ExchangeSettings
{
    public class ExchangeSettingsEntity : TableEntity, IExchangeSettings
    {
        internal static string GeneratePartitionKey()
        {
            return "ExchngSettings";
        }

        internal static string GenerateRowKey(string cleintId)
        {
            return cleintId;
        }

        public string BaseAssetIos { get; set; }
        public string BaseAssetOther { get; set; }
        public bool SignOrder { get; set; }

        public static ExchangeSettingsEntity CreateEmpty(string clientId, IExchangeSettings src)
        {
            return new ExchangeSettingsEntity
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(clientId),
                BaseAssetIos = src.BaseAssetIos,
                BaseAssetOther = src.BaseAssetOther,
                SignOrder = src.SignOrder
            };
        }
    }
}
