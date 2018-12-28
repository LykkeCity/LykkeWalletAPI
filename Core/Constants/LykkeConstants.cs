namespace Core.Constants
{
    public static class LykkeConstants
    {
        public const string DefaultAssetSetting = "Default";

        public const string BitcoinAssetId = "BTC";
        public const string LykkeAssetId = "LKK";
        public const string LykkeForwardAssetId = "LKK1Y";
        public const string HcpAssetId = "HCP";

        public const string UsdAssetId = "USD";
        public const string EurAssetId = "EUR";
        public const string ChfAssetId = "CHF";
        public const string GbpAssetId = "GBP";
        public const string EthAssetId = "ETH";
        public const string SolarAssetId = "SLR";
        public const string ChronoBankAssetId = "TIME";
        public const string QuantaAssetId = "QNT";

        public const string LKKUSDPairId = "LKKUSD";

        public const int TotalLykkeAmount = 1250000000;

        public const int MinPwdLength = 6;
        public const int MaxPwdLength = 100;

        public const int MaxAddressLength = 255;
        public const int MaxFullNameLength = 50;
        public const int MaxZipLength = 10;
        public const int MaxCityLength = 50;
        public const int MaxPhoneLength = 20;
        public const int MaxEmailLength = 50;

        public const int DefaultRefundTimeoutDays = 30;
        public const string PartnerId = "http://lykke.com/oauth/partner_id";
        public const string SessionId = "http://lykke.com/oauth/sessionid";

        #region Cache keys

        public const string LastAskBidForAssetOnPeriod = "__Asset_{0}_Last_ask{1}_{2}__";

        #endregion

        public static string GetLastAskForAssetOnPeriodKey(string assetPairId, string period, bool ask)
        {
            return string.Format(LastAskBidForAssetOnPeriod, assetPairId, ask, period);
        }
    }
}
