using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories;
using Lykke.AzureStorage.Tables;
using Microsoft.WindowsAzure.Storage.Table;

namespace Repositories
{
    public class HistoryExportsRepository : IHistoryExportsRepository
    {
        public const string TableName = "HistoryExportsApi2Projection";
        
        private readonly INoSQLTableStorage<HistoryExportEntry> _tableStorage;

        public HistoryExportsRepository(
            INoSQLTableStorage<HistoryExportEntry> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task Add(string clientId, string id, string url)
        {
            return _tableStorage.InsertOrMergeAsync(new HistoryExportEntry
            {
                PartitionKey = clientId,
                RowKey = id,
                Url = url
            });
        }

        public async Task<string> GetUrl(string clientId, string id)
        {
            var entity = await _tableStorage.GetDataAsync(clientId, id);

            return entity?.Url;
        }

        public Task Remove(string clientId, string id)
        {
            return _tableStorage.DeleteIfExistAsync(clientId, id);
        }
    }

    public class HistoryExportEntry : TableEntity
    {
        public string Url { set; get; }
    }
}