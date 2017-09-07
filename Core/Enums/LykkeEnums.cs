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
}