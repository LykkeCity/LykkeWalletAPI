using System;
using Core.PaymentSystem;

namespace AzureRepositories.PaymentSystem
{
    public class PaymentTransaction : IPaymentTransaction
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public double Amount { get; set; }
        public string AssetId { get; set; }
        public string WalletId { get; set; }
        public double? DepositedAmount { get; set; }
        public string DepositedAssetId { get; set; }
        public double? Rate { get; set; }
        public string AggregatorTransactionId { get; set; }
        public DateTime Created { get; set; }
        public PaymentStatus Status { get; set; }
        public CashInPaymentSystem PaymentSystem { get; set; }
        public string Info { get; set; }
        public string OtherData { get; set; }
        public double FeeAmount { get; set; }
        public string MeTransactionId { get; set; }
    }

    public static class PaymentTransactionExt
    {
        public static object GetInfo(this IPaymentTransaction src, Type expectedType = null, bool throwExeption = false)
        {
            if (!PaymentSystemsAndOtherInfo.PsAndOtherInfoLinks.ContainsKey(src.PaymentSystem))
            {
                if (throwExeption)
                    throw new Exception("Unsupported payment system for reading other info: transactionId:" + src.Id);

                return null;
            }

            var type = PaymentSystemsAndOtherInfo.PsAndOtherInfoLinks[src.PaymentSystem];

            if (expectedType != null)
            {
                if (type != expectedType)
                    throw new Exception("Payment system and Other info does not match for transactionId:" + src.Id);
            }

            return Newtonsoft.Json.JsonConvert.DeserializeObject(src.Info, type);
        }

        public static T GetInfo<T>(this IPaymentTransaction src)
        {
            return (T)GetInfo(src, typeof(T), true);
        }

        public static bool AreMoneyOnOurAccount(this IPaymentTransaction src)
        {
            return src.Status == PaymentStatus.NotifyProcessed;
        }
    }
}