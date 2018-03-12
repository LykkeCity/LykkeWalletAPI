using Core.Exchange;

namespace LkeServices.Exchange
{
    public class ExchangeSettings : IExchangeSettings
    {
        public string BaseAssetIos { get; set; }
        public string BaseAssetOther { get; set; }
        public bool SignOrder { get; set; }
    }
}