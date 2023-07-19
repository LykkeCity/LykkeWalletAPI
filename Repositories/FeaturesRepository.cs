using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories;

namespace Repositories
{
    /// <summary>
    /// RowKey = featureName
    /// PartitionKey = clientId || GlobalSettings (const)
    /// </summary>
    public class FeaturesRepository : IFeaturesRepository
    {
        public const string TableName = "Features";

        public const string GlobalSettingsPartitionKey = "GlobalSettings";
        
        private readonly INoSQLTableStorage<FeatureEntry> _tableStorage;

        public FeaturesRepository(INoSQLTableStorage<FeatureEntry> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IDictionary<string, bool>> GetAll(string clientId = null)
        {
            var globals = await _tableStorage.GetDataAsync(partition: GlobalSettingsPartitionKey);

            var result = globals.ToDictionary(
                key => key.FeatureName,
                value => value.IsEnabled);
            
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                var overrides = await _tableStorage.GetDataAsync(partition: clientId);
                foreach (var overrideEntry in overrides)
                {
                    result[overrideEntry.FeatureName] = overrideEntry.IsEnabled;
                }
            }

            return result;
        }
    }
}
