using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;
using Core.PaymentSystem;

namespace LkeServices.PaymentSystem
{
    public class PaymentTransactionsRepository : IPaymentTransactionsRepository
    {
        private readonly INoSQLTableStorage<PaymentTransactionEntity> _tableStorage;
        private readonly INoSQLTableStorage<AzureMultiIndex> _tableStorageIndices;

        private const string IndexPartitionKey = "IDX";

        public PaymentTransactionsRepository(INoSQLTableStorage<PaymentTransactionEntity> tableStorage,
            INoSQLTableStorage<AzureMultiIndex> tableStorageIndices)
        {
            _tableStorage = tableStorage;
            _tableStorageIndices = tableStorageIndices;
        }

        public async Task CreateAsync(IPaymentTransaction src)
        {

            var commonEntity = PaymentTransactionEntity.Create(src);
            commonEntity.PartitionKey = PaymentTransactionEntity.IndexCommon.GeneratePartitionKey();
            await _tableStorage.InsertAndGenerateRowKeyAsDateTimeAsync(commonEntity, src.Created);

            var entityByClient = PaymentTransactionEntity.Create(src);
            entityByClient.PartitionKey = PaymentTransactionEntity.IndexByClient.GeneratePartitionKey(src.ClientId);
            entityByClient.RowKey = PaymentTransactionEntity.IndexByClient.GenerateRowKey(src.Id);


            var index = AzureMultiIndex.Create(IndexPartitionKey, src.Id, commonEntity, entityByClient);


            await Task.WhenAll(
                _tableStorage.InsertAsync(entityByClient),
                _tableStorageIndices.InsertAsync(index)
            );

        }

        public async Task<IEnumerable<IPaymentTransaction>> GetAsync(DateTime from, DateTime to, Func<IPaymentTransaction, bool> filter)
        {
            to = to.Date.AddDays(1);
            var partitionKey = PaymentTransactionEntity.IndexCommon.GeneratePartitionKey();
            return await _tableStorage.WhereAsync(partitionKey, from, to, ToIntervalOption.ExcludeTo, filter);
        }

        public async Task<IEnumerable<IPaymentTransaction>> GetByClientIdAsync(string clientId)
        {
            var partitionKey = PaymentTransactionEntity.IndexByClient.GeneratePartitionKey(clientId);
            return await _tableStorage.GetDataAsync(partitionKey);
        }


        public async Task<IPaymentTransaction> GetByTransactionIdAsync(string id)
        {
            return await _tableStorageIndices.GetFirstOrDefaultAsync(IndexPartitionKey, id, _tableStorage);
        }

        public async Task<IPaymentTransaction> TryCreateAsync(IPaymentTransaction paymentTransaction)
        {
            if (paymentTransaction == null) throw new ArgumentNullException(nameof(paymentTransaction));

            var existingRecord =
                await
                    _tableStorage.GetDataAsync(
                        PaymentTransactionEntity.IndexByClient.GeneratePartitionKey(paymentTransaction.ClientId),
                        PaymentTransactionEntity.IndexByClient.GenerateRowKey(paymentTransaction.Id));

            if (existingRecord != null)
                return null;

            await CreateAsync(paymentTransaction);

            return paymentTransaction;
        }

        public async Task<IPaymentTransaction> StartProcessingTransactionAsync(string id, string paymentAggregatorTransactionId = null)
        {
            var meTransactionId = Guid.NewGuid().ToString();

            return await _tableStorageIndices.MergeAsync(IndexPartitionKey, id, _tableStorage, entity =>
            {
                if (entity.GetPaymentStatus() != PaymentStatus.Created)
                    return null;

                entity.SetPaymentStatus(PaymentStatus.Processing);
                entity.AggregatorTransactionId = paymentAggregatorTransactionId;
                entity.MeTransactionId = meTransactionId;
                return entity;
            });
        }


        public async Task<IPaymentTransaction> SetAggregatorTransactionId(string id, string aggregatorTransactionId)
        {
            return await _tableStorageIndices.MergeAsync(IndexPartitionKey, id, _tableStorage, entity =>
            {
                entity.AggregatorTransactionId = aggregatorTransactionId;
                return entity;
            });
        }

        public async Task<IEnumerable<IPaymentTransaction>> ScanAndFindAsync(Func<IPaymentTransaction, bool> callback)
        {
            var partitionKey = PaymentTransactionEntity.IndexCommon.GeneratePartitionKey();
            return await _tableStorage.GetDataAsync(partitionKey, callback);
        }

        public async Task<IPaymentTransaction> SetStatus(string id, PaymentStatus status)
        {

            return await _tableStorageIndices.MergeAsync(IndexPartitionKey, id, _tableStorage, entity =>
            {
                entity.SetPaymentStatus(status);
                return entity;
            });

        }

        public async Task<IPaymentTransaction> SetAsOkAsync(string id, double depositedAmount, double? rate)
        {
            return await _tableStorageIndices.MergeAsync(IndexPartitionKey, id, _tableStorage, entity =>
            {
                entity.SetPaymentStatus(PaymentStatus.NotifyProcessed);
                entity.DepositedAmount = depositedAmount;
                entity.Rate = rate;
                return entity;
            });
        }


        public async Task<IPaymentTransaction> GetLastByDate(string clientId)
        {

            var partitionKey = PaymentTransactionEntity.IndexByClient.GeneratePartitionKey(clientId);

            var entities = await _tableStorage.GetDataAsync(partitionKey);

            return entities.OrderByDescending(itm => itm.Created).FirstOrDefault();
        }


    }

}