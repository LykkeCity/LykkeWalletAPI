namespace Core.ExchangeSettings
{
    public class ExchangeSettings : IExchangeSettings
    {
        public string BaseAssetIos { get; set; }
        public string BaseAssetOther { get; set; }
        public bool SignOrder { get; set; }
        
        public static ExchangeSettings CreateDeafault()
        {
            return new ExchangeSettings
            {
                BaseAssetIos = string.Empty,
                BaseAssetOther = string.Empty,
                SignOrder = true
            };
        }
    }
}
