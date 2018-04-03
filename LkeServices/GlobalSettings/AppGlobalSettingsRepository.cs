using System.Threading.Tasks;
using AzureStorage;
using Core.GlobalSettings;

namespace LkeServices.GlobalSettings
{
    public class AppGlobalSettingsRepository : IAppGlobalSettingsRepository
    {
        public string GeneratePartitionKey() => "Setup";
        public string GenerateRowKey() => "AppSettings";

        private readonly INoSQLTableStorage<AppGlobalSettingsEntity> _tableStorage;
        private readonly IAppGlobalSettings _defaultAppGlobalSettings;

        public AppGlobalSettingsRepository(INoSQLTableStorage<AppGlobalSettingsEntity> tableStorage, 
            IAppGlobalSettings defaultAppGlobalSettings)
        {
            _tableStorage = tableStorage;
            _defaultAppGlobalSettings = defaultAppGlobalSettings;
        }

        public async Task<IAppGlobalSettings> GetFromDbOrDefault()
        {
            return await GetAsync() ?? _defaultAppGlobalSettings;
        }

        private async Task<IAppGlobalSettings> GetAsync()
        {
            var partitionKey = GeneratePartitionKey();
            var rowKey = GenerateRowKey();
            return await _tableStorage.GetDataAsync(partitionKey, rowKey);
        }
    }
}