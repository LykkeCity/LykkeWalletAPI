using System;
using Core.GlobalSettings;

namespace AzureRepositories.GlobalSettings
{
    public class AppGlobalSettings : IAppGlobalSettings
    {
        public string DepositUrl { get; set; }
        public bool DebugMode { get; set; }
        public string DefaultIosAssetGroup { get; set; }
        public string DefaultAssetGroupForOther { get; set; }
        public bool IsOnReview { get; set; }
        public double? MinVersionOnReview { get; set; }
        public double IcoLkkSold { get; set; }
        public bool IsOnMaintenance { get; set; }
        public int LowCashOutTimeoutMins { get; set; }
        public int LowCashOutLimit { get; set; }
        public bool MarginTradingEnabled { get; set; }
        public bool CashOutBlocked { get; set; }
        public bool BtcOperationsDisabled { get; set; }
        public bool BitcoinBlockchainOperationsDisabled { get; set; }
        public bool LimitOrdersEnabled { get; set; }
        public double MarketOrderPriceDeviation { get; set; }
        public string[] BlockedAssetPairs { get; set; }
        public string OnReviewAssetConditionLayer { get; set; }
        public DateTime? IcoStartDtForWhitelisted { get; set; }
        public DateTime? IcoStartDt { get; set; }
        public bool ShowIcoBanner { get; set; }
    }
}