using Core.CashOperations;
using Core.Enumerators;
using System;

namespace AzureRepositories.CashOperations
{
    public class LimitTradeEventEntity : BaseEntity, ILimitTradeEvent
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

        public static string GeneratePartitionKey(string clientId)
        {
            return clientId;
        }
    }
}
