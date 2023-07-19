using Microsoft.WindowsAzure.Storage.Table;

namespace Core.Repositories
{
    public class FeatureEntry : TableEntity, IFeatureEntry
    {
        public string FeatureName => RowKey;
        
        public bool IsEnabled { get; set; }
    }
}
