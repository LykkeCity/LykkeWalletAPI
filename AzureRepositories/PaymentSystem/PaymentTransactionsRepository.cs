using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;
using Core.PaymentSystem;

namespace AzureRepositories.PaymentSystem
{
    public class PaymentTransactionsRepository : IPaymentTransactionsRepository
    {
        private const string IndexPartitionKey = "IDX";

        private readonly INoSQLTableStorage<PaymentTransactionEntity> _tableStorage;
        private readonly INoSQLTableStorage<AzureMultiIndex> _tableStorageIndices;

        public PaymentTransactionsRepository(INoSQLTableStorage<PaymentTransactionEntity> tableStorage,
            INoSQLTableStorage<AzureMultiIndex> tableStorageIndices)
        {
            _tableStorage = tableStorage;
            _tableStorageIndices = tableStorageIndices;
        }

        public async Task InsertAsync(IPaymentTransaction src)
        {
            var commonEntity = PaymentTransactionEntity.Create(src);
            commonEntity.PartitionKey = PaymentTransactionEntity.GeneratePartitionKey();
            await _tableStorage.InsertAndGenerateRowKeyAsDateTimeAsync(commonEntity, src.Created);

            var entityByClient = PaymentTransactionEntity.Create(src);
            entityByClient.PartitionKey = PaymentTransactionEntity.GeneratePartitionKey(src.ClientId);
            entityByClient.RowKey = PaymentTransactionEntity.GenerateRowKey(src.Id);

            var index = AzureMultiIndex.Create(IndexPartitionKey, src.Id, commonEntity, entityByClient);

            await Task.WhenAll(
                _tableStorage.InsertAsync(entityByClient),
                _tableStorageIndices.InsertAsync(index)
            );
        }
        
        public async Task<IPaymentTransaction> GetLastByDate(string clientId)
        {
            var partitionKey = PaymentTransactionEntity.GeneratePartitionKey(clientId);
            var entities = await _tableStorage.GetDataAsync(partitionKey);

            return entities.OrderByDescending(itm => itm.Created).FirstOrDefault();
        }
    }
}