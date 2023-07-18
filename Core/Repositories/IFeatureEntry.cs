using Microsoft.WindowsAzure.Storage.Table;

namespace Core.Repositories
{
    public interface IFeatureEntry : ITableEntity
    {
        public bool IsEnabled { get; set; }
    }
}
