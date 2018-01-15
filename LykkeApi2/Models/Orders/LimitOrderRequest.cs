namespace LykkeApi2.Models.Orders
{
    public class LimitOrderRequest
    {
        public string AssetPairId { get; set; }
        
        public OrderAction OrderAction { get; set; }
        
        public double Volume { get; set; }
        
        public double Price { get; set; }
    }
}