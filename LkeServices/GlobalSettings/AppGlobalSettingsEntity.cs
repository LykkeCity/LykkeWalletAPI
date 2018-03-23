using System;
using Core.GlobalSettings;
using Microsoft.WindowsAzure.Storage.Table;

namespace LkeServices.GlobalSettings
{
    public class AppGlobalSettingsEntity : TableEntity, IAppGlobalSettings
    {
        public string DepositUrl { get; }
        public bool DebugMode { get; }
        public string DefaultIosAssetGroup { get; set; }
        public string DefaultAssetGroupForOther { get; set; }
        public bool IsOnReview { get; }
        public double? MinVersionOnReview { get; }
        public double IcoLkkSold { get; }
        public bool IsOnMaintenance { get; }
        public int LowCashOutTimeoutMins { get; }
        public int LowCashOutLimit { get; }
        public bool MarginTradingEnabled { get; }
        public bool CashOutBlocked { get; }
        public bool BtcOperationsDisabled { get; }
        public bool BitcoinBlockchainOperationsDisabled { get; }
        public bool LimitOrdersEnabled { get; }
        public double MarketOrderPriceDeviation { get; }
        public string[] BlockedAssetPairs { get; set; }
        public string OnReviewAssetConditionLayer { get; set; }
        public DateTime? IcoStartDtForWhitelisted { get; set; }
        public DateTime? IcoStartDt { get; set; }
        public bool ShowIcoBanner { get; set; }
    }
}