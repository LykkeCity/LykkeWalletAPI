using System.Threading.Tasks;
using Core.Wallet;

namespace Core.PaymentSystem
{
    public interface IPaymentSystemFacade
    {
        bool IsAssetIdSupported(
            string assetId,
            string isoCountryCode,
            string clientPaymentSystem,
            OwnerType owner);

        Task<PaymentUrlData> GetUrlDataAsync(
            string clientPaymentSystem,
            string orderId,
            string clientId,
            double amount,
            string assetId,
            string walletId,
            string isoCountryCode,
            string otherInfoJson);

        Task<string> GetSourceClientIdAsync(CashInPaymentSystem paymentSystem, OwnerType owner);
    }
}