namespace LykkeApi2.Models.Orders
{
    public class StopLimitOrderRequest
    {
        public string AssetPairId { get; set; }        
        public double Volume { get; set; }
        public decimal? LowerLimitPrice { get; set; }
        public decimal? LowerPrice { get; set; }
        public decimal? UpperLimitPrice { get; set; }
        public decimal? UpperPrice { get; set; }
        public OrderAction OrderAction { get; set; }
    }
}
