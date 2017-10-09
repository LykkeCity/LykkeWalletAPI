using Core.Enumerators;
using System;

namespace Core.Exchange
{
    public class MarketOrder : IMarketOrder
    {
        public DateTime CreatedAt { get; set; }
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string AssetPairId { get; set; }
        public OrderAction OrderAction { get; set; }
        public double Volume { get; set; }
        public double Price { get; set; }
        public string Status { get; set; }
        public bool Straight { get; set; }

        public DateTime MatchedAt { get; set; }
    }
}
