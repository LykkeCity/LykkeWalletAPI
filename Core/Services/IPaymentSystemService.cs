using System.Threading.Tasks;
using Core.PaymentSystem;

namespace Core.Services
{
    public interface IPaymentSystemService
    {
        Task<PaymentUrlData> GetUrlDataAsync(
            string clientPaymentSystem,
            string orderId,
            string clientId,
            double amount,
            string assetId,
            string walletId,
            string isoCountryCode,
            string otherInfoJson);

        Task InsertPaymentTransactionAsync(IPaymentTransaction src);

        Task<IPaymentTransaction> GetLastPaymentTransactionByDate(string clientId);

        Task InsertPaymentTransactionEventLogAsync(IPaymentTransactionEventLog newEvent);

        IPaymentTransactionEventLog CreatePaymentTransactionEventLog(string transactionId, string techData,
            string message, string who);

        IPaymentTransaction CreatePaymentTransaction(string id,
            CashInPaymentSystem paymentSystem,
            string clientId,
            double amount,
            double feeAmount,
            string assetId,
            string walletId,
            string assetToDeposit = null,
            string info = "",
            PaymentStatus status = PaymentStatus.Created);
    }
}