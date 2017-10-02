using Core.Exchange;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzureRepositories.CashOperations
{
    public interface IMarketOrdersRepository
    {
        Task CreateAsync(IMarketOrder marketOrder);
        Task<IMarketOrder> GetAsync(string orderId);
        Task<IMarketOrder> GetAsync(string clientId, string orderId);
        Task<IEnumerable<IMarketOrder>> GetOrdersAsync(string clientId);
        Task<IEnumerable<IMarketOrder>> GetOrdersAsync(IEnumerable<string> orderIds);
    }
}
