using System.Threading.Tasks;
using AzureStorage;
using Core.Exchange;

namespace AzureRepositories.Exchange
{
    public class ExchangeSettingsRepository : IExchangeSettingsRepository
    {
        private readonly IExchangeSettings _defaultExchangeSettings;
        private readonly INoSQLTableStorage<ExchangeSettingsEntity> _tableStorage;
        private readonly ExchangeSettingsEntity _exchangeSettingsEntity;

        public ExchangeSettingsRepository(INoSQLTableStorage<ExchangeSettingsEntity> tableStorage, ExchangeSettingsEntity exchangeSettingsEntity, IExchangeSettings defaultExchangeSettings)
        {
            _tableStorage = tableStorage;
            _exchangeSettingsEntity = exchangeSettingsEntity;
            _defaultExchangeSettings = defaultExchangeSettings;
        }

        public async Task<IExchangeSettings> GetOrDefaultAsync(string clientId)
        {
            return await GetAsync(clientId) ?? _defaultExchangeSettings;
        }

        public async Task<IExchangeSettings> GetAsync(string clientId)
        {
            var partitionKey = _exchangeSettingsEntity.GeneratePartitionKey();
            var rowKey = _exchangeSettingsEntity.GenerateRowKey(clientId);
            return await _tableStorage.GetDataAsync(partitionKey, rowKey);
        }
    }
}