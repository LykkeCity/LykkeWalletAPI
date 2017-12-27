using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Domain.Orderbook;

namespace Core.Services
{
    public interface IOrderBooksService
    {
        Task<IEnumerable<IOrderBook>> GetAllAsync();
        Task<IEnumerable<IOrderBook>> GetAsync(string assetPairId);
    }
}
