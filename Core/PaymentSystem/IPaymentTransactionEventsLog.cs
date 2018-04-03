using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.PaymentSystem
{
    public interface IPaymentTransactionEventsLogRepository
    {
        Task InsertAsync(IPaymentTransactionEventLog newEvent);
    }
}