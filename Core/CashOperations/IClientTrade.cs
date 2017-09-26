using System;

namespace Core.CashOperations
{
    public interface IClientTrade : IBaseCashBlockchainOperation
    {
        string LimitOrderId { get; }
        string MarketOrderId { get; }
        double Price { get; }
        DateTime? DetectionTime { get; set; }
        int Confirmations { get; set; }
        string OppositeLimitOrderId { get; set; }
        bool IsLimitOrderResult { get; set; }
    }
}
