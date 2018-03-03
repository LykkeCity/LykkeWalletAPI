namespace LykkeApi2.Models.Orders
{
    public class MarketOrderRequest
    {
        public string AssetPairId { get; set; }
        public string AssetId { get; set; }
        public OrderAction OrderAction { get; set; }
        public double Volume { get; set; }
    }
    
    public enum OrderAction
    {
        Buy = 0,
        Sell = 1
    }
}