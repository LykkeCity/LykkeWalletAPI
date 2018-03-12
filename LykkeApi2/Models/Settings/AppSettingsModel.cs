using Lykke.Service.ClientAccount.Client.Models;

namespace LykkeApi2.Models.Settings
{
    public class AppSettingsModel
    {
        public int RateRefreshPeriod { get; set; }
        public ApiAssetModel BaseAsset { get; set; }
        public bool SignOrder { get; set; }
        public string DepositUrl { get; set; }
        public bool DebugMode { get; set; }
        public RefundAddressSettingsModel RefundSettings { get; set; }
        public double MarketOrderPriceDeviation { get; set; }
        public ApiFee FeeSettings { get; set; }
    }
}
