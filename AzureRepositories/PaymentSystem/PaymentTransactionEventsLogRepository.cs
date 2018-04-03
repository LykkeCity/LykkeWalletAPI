using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Core.PaymentSystem;

namespace AzureRepositories.PaymentSystem
{
    public class PaymentTransactionEventsLogRepository : IPaymentTransactionEventsLogRepository
    {
        private readonly INoSQLTableStorage<PaymentTransactionEventLogEntity> _tableStorage;

        public PaymentTransactionEventsLogRepository(INoSQLTableStorage<PaymentTransactionEventLogEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task InsertAsync(IPaymentTransactionEventLog newEvent)
        {
            var newEntity = PaymentTransactionEventLogEntity.Create(newEvent);
            await _tableStorage.InsertAndGenerateRowKeyAsDateTimeAsync(newEntity, newEntity.DateTime);
        }
    }
}