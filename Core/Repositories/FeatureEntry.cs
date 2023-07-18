using Microsoft.WindowsAzure.Storage.Table;

namespace Core.Repositories
{
    public class FeatureEntry : TableEntity, IFeatureEntry
    {
        public bool IsEnabled { get; set; }
    }
}
