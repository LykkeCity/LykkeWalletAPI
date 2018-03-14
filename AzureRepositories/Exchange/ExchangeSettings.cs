using Core.Exchange;

namespace AzureRepositories.Exchange
{
    public class ExchangeSettings : IExchangeSettings
    {
        public string BaseAssetIos { get; set; }
        public string BaseAssetOther { get; set; }
        public bool SignOrder { get; set; }
    }
}