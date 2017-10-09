using Core.Enumerators;

namespace Core.CashOperations
{
    public interface ICashInOutOperation : IBaseCashBlockchainOperation
    {
        bool IsRefund { get; set; }
        CashOperationType Type { get; set; }
    }
}
