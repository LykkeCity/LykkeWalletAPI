using System;

namespace LykkeApi2.Models.Orders
{
    public class LimitOrderResponseModel
    {
        public string Id { set; get; }
        public string AssetPairId { set; get; }
        public decimal Volume { set; get; }
        public decimal Price { set; get; }
        public DateTime CreateDateTime { set; get; }
        public string OrderAction { set; get; }
        public string Status { set; get; }
        public decimal RemainingVolume { get; set; }
    }
}