using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.PaymentSystem
{
    public interface IPaymentTransactionsRepository
    {
        Task InsertAsync(IPaymentTransaction paymentTransaction);
        Task<IPaymentTransaction> GetLastByDate(string clientId);
    }
}