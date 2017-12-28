using System.Threading.Tasks;
using AzureStorage;
using Core.ExchangeSettings;

namespace AzureRepositories.ExchangeSettings
{
    public class ExchangeSettingsRepository : IExchangeSettingsRepository
    {
        private readonly INoSQLTableStorage<ExchangeSettingsEntity> _tableStorage;

        public ExchangeSettingsRepository(INoSQLTableStorage<ExchangeSettingsEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }
        
        public async Task<IExchangeSettings> GetOrDefaultAsync(string clientId)
        {
            var partitionKey = ExchangeSettingsEntity.GeneratePartitionKey();
            var rowKey = ExchangeSettingsEntity.GenerateRowKey(clientId);

            var result = await _tableStorage.GetDataAsync(partitionKey, rowKey);
            if (result != null)
                return result;

            return Core.ExchangeSettings.ExchangeSettings.CreateDeafault();
        }
    }
}