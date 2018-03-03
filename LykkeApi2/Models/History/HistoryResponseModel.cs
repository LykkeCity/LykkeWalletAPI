using System;
using Lykke.Service.OperationsHistory.AutorestClient.Models;

namespace LykkeApi2.Models.History
{
    public class HistoryResponseModel
    {
        public string Id { get; set; }
        
        public DateTime DateTime { get; set; }
        
        public HistoryOperationType Type { get; set; }
        
        public HistoryOperationState State { get; set; }
        
        public double Amount { get; set; }
        
        public string Asset { get; set; }
        
        public string AssetPair { get; set; }
        
        public double? Price { get; set; }
    }

    public static class HistoryOperationToResponseConverter
    {
        public static HistoryResponseModel ToResponseModel(this HistoryOperation operation)
        {
            if (operation == null)
                return null;

            return new HistoryResponseModel
            {
                Id = operation.Id,
                DateTime = operation.DateTime,
                Type = operation.Type,
                State = operation.State,
                Amount = operation.Amount,
                Asset = operation.Asset,
                AssetPair = operation.AssetPair,
                Price = operation.Price
            };
        }
    }
}