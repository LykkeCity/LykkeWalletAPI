using AzureRepositories.CashOperations;
using AzureStorage;
using Core.Exchange;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureRepositories.Exchange
{
    public class MarketOrdersRepository : IMarketOrdersRepository
    {
        private readonly INoSQLTableStorage<MarketOrderEntity> _tableStorage;

        public MarketOrdersRepository(INoSQLTableStorage<MarketOrderEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IMarketOrder> GetAsync(string orderId)
        {
            var partitionKey = MarketOrderEntity.ByOrderId.GeneratePartitionKey();
            var rowKey = MarketOrderEntity.ByOrderId.GenerateRowKey(orderId);

            return await _tableStorage.GetDataAsync(partitionKey, rowKey);
        }

        public async Task<IMarketOrder> GetAsync(string clientId, string orderId)
        {
            var partitionKey = MarketOrderEntity.ByClientId.GeneratePartitionKey(clientId);
            var rowKey = MarketOrderEntity.ByClientId.GenerateRowKey(orderId);

            return await _tableStorage.GetDataAsync(partitionKey, rowKey);
        }

        public async Task<IEnumerable<IMarketOrder>> GetOrdersAsync(string clientId)
        {
            var partitionKey = MarketOrderEntity.ByClientId.GeneratePartitionKey(clientId);

            return await _tableStorage.GetDataAsync(partitionKey);
        }

        public async Task<IEnumerable<IMarketOrder>> GetOrdersAsync(IEnumerable<string> orderIds)
        {
            var partitionKey = MarketOrderEntity.ByOrderId.GeneratePartitionKey();
            orderIds = orderIds.Select(MarketOrderEntity.ByOrderId.GenerateRowKey);

            return await _tableStorage.GetDataAsync(partitionKey, orderIds);
        }

        public async Task CreateAsync(IMarketOrder marketOrder)
        {
            var byOrderEntity = MarketOrderEntity.ByOrderId.Create(marketOrder);
            var byClientEntity = MarketOrderEntity.ByClientId.Create(marketOrder);

            await _tableStorage.InsertAsync(byOrderEntity);
            await _tableStorage.InsertAsync(byClientEntity);
        }
    }

}
