using System;

namespace Core.GlobalSettings
{
    public interface IAppGlobalSettings
    {
        string DepositUrl { get; }
        bool DebugMode { get; }
        string DefaultIosAssetGroup { get; set; }
        string DefaultAssetGroupForOther { get; set; }
        bool IsOnReview { get; }
        double? MinVersionOnReview { get; }
        double IcoLkkSold { get; }
        bool IsOnMaintenance { get; }
        int LowCashOutTimeoutMins { get; }
        int LowCashOutLimit { get; }
        bool MarginTradingEnabled { get; }
        bool CashOutBlocked { get; }
        bool BtcOperationsDisabled { get; }
        bool BitcoinBlockchainOperationsDisabled { get; }
        bool LimitOrdersEnabled { get; }
        double MarketOrderPriceDeviation { get; }
        string[] BlockedAssetPairs { get; set; }
        string OnReviewAssetConditionLayer { get; set; }
        DateTime? IcoStartDtForWhitelisted { get; set; }
        DateTime? IcoStartDt { get; set; }
        bool ShowIcoBanner { get; set; }
    }
}