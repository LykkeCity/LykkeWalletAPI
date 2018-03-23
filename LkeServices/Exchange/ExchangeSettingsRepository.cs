using System.Threading.Tasks;
using AzureStorage;
using Core.Exchange;

namespace LkeServices.Exchange
{
    public class ExchangeSettingsRepository : IExchangeSettingsRepository
    {
        public string GeneratePartitionKey() => "ExchngSettings";
        public string GenerateRowKey(string clientId) => clientId;

        private readonly IExchangeSettings _defaultExchangeSettings;
        private readonly INoSQLTableStorage<ExchangeSettingsEntity> _tableStorage;
        
        public ExchangeSettingsRepository(INoSQLTableStorage<ExchangeSettingsEntity> tableStorage, IExchangeSettings defaultExchangeSettings)
        {
            _tableStorage = tableStorage;
            _defaultExchangeSettings = defaultExchangeSettings;
        }

        public async Task<IExchangeSettings> GetOrDefaultAsync(string clientId)
        {
            return await GetAsync(clientId) ?? _defaultExchangeSettings;
        }

        public async Task<IExchangeSettings> GetAsync(string clientId)
        {
            var partitionKey = GeneratePartitionKey();
            var rowKey = GenerateRowKey(clientId);
            return await _tableStorage.GetDataAsync(partitionKey, rowKey);
        }
    }
}