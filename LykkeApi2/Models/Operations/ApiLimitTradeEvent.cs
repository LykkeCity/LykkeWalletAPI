using System;

namespace LykkeApi2.Models.Operations
{
    public class ApiLimitTradeEvent
    {
        public string Id { get; set; }
        public string OrderId { get; set; }
        public DateTime DateTime { get; set; }
        public string Asset { get; set; }
        public string AssetPair { get; set; }
        public double Volume { get; set; }
        public double Price { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public double TotalCost { get; set; }
    }
}
