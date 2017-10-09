using Core.Enumerators;
using System;

namespace Core.CashOperations
{
    public class LimitTradeEvent : ILimitTradeEvent
    {
        public DateTime CreatedDt { get; set; }
        public OrderType OrderType { get; set; }
        public double Volume { get; set; }
        public string AssetId { get; set; }
        public string AssetPair { get; set; }
        public double Price { get; set; }
        public OrderStatus Status { get; set; }
        public bool IsHidden { get; set; }
        public string ClientId { get; set; }
        public string Id { get; set; }
        public string OrderId { get; set; }
    }
}
