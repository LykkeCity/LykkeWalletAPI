using AzureStorage;
using Core.CashOperations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AzureRepositories.CashOperations
{
    public class LimitTradeEventsRepository : ILimitTradeEventsRepository
    {
        private readonly INoSQLTableStorage<LimitTradeEventEntity> _storage;

        public LimitTradeEventsRepository(INoSQLTableStorage<LimitTradeEventEntity> storage)
        {
            _storage = storage;
        }

        public async Task<IEnumerable<ILimitTradeEvent>> GetEventsAsync(string clientId)
        {
            return await _storage.GetDataAsync(LimitTradeEventEntity.GeneratePartitionKey(clientId));
        }

        public async Task<IEnumerable<ILimitTradeEvent>> GetEventsAsync(string clientId, string orderId)
        {
            return await _storage.GetDataAsync(LimitTradeEventEntity.GeneratePartitionKey(clientId), entity => entity.OrderId == orderId);
        }
    }
}
