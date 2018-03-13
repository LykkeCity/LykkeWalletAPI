using System;

namespace Core.PaymentSystem
{
    public interface IPaymentTransaction
    {
        string Id { get; }
        string ClientId { get; }
        double Amount { get; }
        string AssetId { get; }
        string WalletId { get; }
        /// <summary>
        /// Amount of asset we deposit account
        /// </summary>
        double? DepositedAmount { get; }
        string DepositedAssetId { get; }
        double? Rate { get; }
        string AggregatorTransactionId { get; }
        DateTime Created { get; }
        PaymentStatus Status { get; }
        CashInPaymentSystem PaymentSystem { get; }
        string Info { get; }
        double FeeAmount { get; }
        string MeTransactionId { get; set; }
    }
}