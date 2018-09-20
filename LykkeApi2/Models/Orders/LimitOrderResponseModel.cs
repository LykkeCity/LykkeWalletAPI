using System;

namespace LykkeApi2.Models.Orders
{
    public class LimitOrderResponseModel
    {
        public Guid Id { set; get; }
        public string AssetPairId { set; get; }
        public decimal Volume { set; get; }
        public decimal Price { set; get; }
        public decimal? LowerLimitPrice { get; set; }
        public decimal? LowerPrice { get; set; }
        public decimal? UpperLimitPrice { get; set; }
        public decimal? UpperPrice { get; set; }
        public DateTime CreateDateTime { set; get; }
        public string OrderAction { set; get; }
        public string Status { set; get; }
        public string Type { set; get; }
        public decimal RemainingVolume { get; set; }
    }
}
