using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.PaymentSystem
{
    public interface IPaymentTransactionsRepository
    {
        Task CreateAsync(IPaymentTransaction paymentTransaction);
        Task<IEnumerable<IPaymentTransaction>> GetAsync(DateTime from, DateTime to, Func<IPaymentTransaction, bool> filter);
        Task<IEnumerable<IPaymentTransaction>> GetByClientIdAsync(string clientId);
        Task<IPaymentTransaction> GetByTransactionIdAsync(string id);
        Task<IPaymentTransaction> TryCreateAsync(IPaymentTransaction paymentTransaction);
        /// <summary>
        /// Change transaction to process state and check if it's already processed or started being processed
        /// </summary>
        /// <param name="id">it of transaction</param>
        /// <param name="paymentAggregatorTransactionId">id of payment aggregator to update if transaction can be processed</param>
        /// <returns>null - transaction is not exists or can not be processed</returns>
        Task<IPaymentTransaction> StartProcessingTransactionAsync(string id, string paymentAggregatorTransactionId = null);
        Task<IPaymentTransaction> SetStatus(string id, PaymentStatus status);
        Task<IPaymentTransaction> SetAsOkAsync(string id, double depositedAmount, double? rate);
        Task<IPaymentTransaction> GetLastByDate(string clientId);
        Task<IPaymentTransaction> SetAggregatorTransactionId(string id, string aggregatorTransactionId);
        Task<IEnumerable<IPaymentTransaction>> ScanAndFindAsync(Func<IPaymentTransaction, bool> callback);
    }
}