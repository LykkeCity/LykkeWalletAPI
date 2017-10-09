using System.ComponentModel;

namespace Core.Enums
{
    public enum EnNetworkType
    {
        Main,
        Testnet
    }
    public enum EnAsset
    {
        BTC,
        LKK,
        USD,
        EUR,
        CHF,
        GBP,
        ETH,
        SLR,
        [Description("ChronoBankAssetId")]
        TIME,
        [Description("QuantaAssetId")]
        QNT
    }
    public enum EnAssetPair
    {
        [Description("LKKUSDPairId")]
        LKKUSD,        
    }

    public enum EnPriceType
    {
        Unspecified = 0,
        Bid = 1,
        Ask = 2,
        Mid = 3
    }

    public enum EnTimeInterval
    {
        Unspecified = 0,
        Sec = 1,
        Minute = 60,
        Min5 = 300,
        Min15 = 900,
        Min30 = 1800,
        Hour = 3600,
        Hour4 = 7200,
        Hour6 = 21600,
        Hour12 = 43200,
        Day = 86400,
        Week = 604800,
        Month = 3000000
    }
}