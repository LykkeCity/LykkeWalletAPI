using Core.CashOperations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureRepositories.CashOperations
{
    public interface ILimitTradeEventsRepository
    {
        Task<IEnumerable<ILimitTradeEvent>> GetEventsAsync(string clientId);
        Task<IEnumerable<ILimitTradeEvent>> GetEventsAsync(string clientId, string orderId);
    }
}
