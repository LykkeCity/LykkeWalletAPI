using System.Threading.Tasks;
using AzureStorage;
using Core.GlobalSettings;

namespace AzureRepositories.GlobalSettings
{
    public class AppGlobalSettingsRepository : IAppGlobalSettingsRepository
    {
        private readonly INoSQLTableStorage<AppGlobalSettingsEntity> _tableStorage;
        private readonly AppGlobalSettingsEntity _appGlobalSettingsEntity;
        private readonly AppGlobalSettings _defaultAppGlobalSettings;

        public AppGlobalSettingsRepository(AppGlobalSettingsEntity appGlobalSettingsEntity, 
            INoSQLTableStorage<AppGlobalSettingsEntity> tableStorage, 
            AppGlobalSettings defaultAppGlobalSettings)
        {
            _appGlobalSettingsEntity = appGlobalSettingsEntity;
            _tableStorage = tableStorage;
            _defaultAppGlobalSettings = defaultAppGlobalSettings;
        }

        public async Task<IAppGlobalSettings> GetFromDbOrDefault()
        {
            return await GetAsync() ?? _defaultAppGlobalSettings;
        }

        private async Task<IAppGlobalSettings> GetAsync()
        {
            var partitionKey = _appGlobalSettingsEntity.GeneratePartitionKey();
            var rowKey = _appGlobalSettingsEntity.GenerateRowKey();
            return await _tableStorage.GetDataAsync(partitionKey, rowKey);
        }
    }
}