using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.PaymentSystem
{
    public interface IPaymentTransactionEventsLog
    {
        Task WriteAsync(IPaymentTransactionEventLog newEvent);
        Task<IEnumerable<IPaymentTransactionEventLog>> GetAsync(string id);
    }
}