using System;
using System.Collections.Generic;

namespace Core.Domain.Orderbook
{
    public class OrderBook : IOrderBook
    {
        public string AssetPair { get; set; }
        public bool IsBuy { get; set; }
        public DateTime Timestamp { get; set; }
        public List<VolumePrice> Prices { get; set; } = new List<VolumePrice>();
    }
}