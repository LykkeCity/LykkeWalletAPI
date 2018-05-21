using Lykke.Service.ClientAccount.Client.Models;

namespace LykkeApi2.Models.Settings
{
    public class AppSettingsModel
    {
        public int RateRefreshPeriod { get; set; }
        public string BaseAssetId { get; set; }
        public bool SignOrder { get; set; }
        public RefundAddressSettingsModel RefundSettings { get; set; }
        public double MarketOrderPriceDeviation { get; set; }
    }
}
