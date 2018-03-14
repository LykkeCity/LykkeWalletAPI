using System;
using Core.PaymentSystem;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.PaymentSystem
{
    public class PaymentTransactionEventLogEntity : TableEntity, IPaymentTransactionEventLog
    {
        internal static string GeneratePartitionKey(string transactionId) => transactionId;
        public string PaymentTransactionId => PartitionKey;
        public DateTime DateTime { get; set; }
        public string TechData { get; set; }
        public string Message { get; set; }
        public string Who { get; set; }

        public static PaymentTransactionEventLogEntity Create(IPaymentTransactionEventLog src)
        {
            return new PaymentTransactionEventLogEntity
            {
                PartitionKey = GeneratePartitionKey(src.PaymentTransactionId),
                DateTime = src.DateTime,
                Message = src.Message,
                TechData = src.TechData,
                Who = src.Who
            };
        }
    }
}