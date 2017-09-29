using System.Collections.Generic;

namespace Core.Mappers
{
    public interface IHistoryOperationMapProvider
    {
        IDictionary<string, string> Cash { get; }
        IDictionary<string, string> CashOutAttempt { get; }
        IDictionary<string, string> ClientTrade { get; }
        IDictionary<string, string> TransferEvent { get; }
    }
}
