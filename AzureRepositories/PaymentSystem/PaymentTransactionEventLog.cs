using System;
using Core.PaymentSystem;

namespace AzureRepositories.PaymentSystem
{
    public class PaymentTransactionEventLog : IPaymentTransactionEventLog
    {
        public string PaymentTransactionId { get; set; }
        public DateTime DateTime { get; set; }
        public string TechData { get; set; }
        public string Message { get; set; }
        public string Who { get; set; }
        
        public static PaymentTransactionEventLog Create(string transactionId, string techData, string message, string who)
        {
            return new PaymentTransactionEventLog
            {
                PaymentTransactionId = transactionId,
                DateTime = DateTime.UtcNow,
                Message = message,
                TechData = techData,
                Who = who
            };
        }
    }
}