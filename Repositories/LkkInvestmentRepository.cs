using System;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;

namespace Repositories
{
    public class LkkInvestmentRequestRepository : ILkkInvestmentRequestRepository
    {
        public const string TableName = "LkkInvestmentRequest";
        
        private readonly INoSQLTableStorage<LkkInvestmentRequest> _tableStorage;

        public LkkInvestmentRequestRepository(
            INoSQLTableStorage<LkkInvestmentRequest> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task Add(string clientId,  string requestId, string amount, string purchaseOption)
        {
            return _tableStorage.InsertOrMergeAsync(new LkkInvestmentRequest
            {
                PartitionKey = clientId,
                RowKey = requestId,
                PurchaseOption = purchaseOption,
                Amount = amount
            });
        }
    }
    
    public class LkkInvestmentRequest : TableEntity
    {
        public string Amount { set; get; }
        public string PurchaseOption { set; get; }
    }
}
