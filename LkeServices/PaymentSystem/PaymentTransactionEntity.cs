using System;
using Common;
using Core.PaymentSystem;
using Microsoft.WindowsAzure.Storage.Table;

namespace LkeServices.PaymentSystem
{
    public class PaymentTransactionEntity : TableEntity, IPaymentTransaction
    {
        public static string GeneratePartitionKey() => "BCO";
        public static string GeneratePartitionKey(string clientId) => clientId;
        public static string GenerateRowKey(string orderId) => orderId;
        public int Id { get; set; }
        public string TransactionId { get; set; }
        string IPaymentTransaction.Id => TransactionId ?? Id.ToString();
        public string ClientId { get; set; }
        public DateTime Created { get; set; }
        public string Info { get; set; }
        public double? Rate { get; set; }
        public string AggregatorTransactionId { get; set; }
        public double Amount { get; set; }
        public string AssetId { get; set; }
        public string WalletId { get; set; }
        public double? DepositedAmount { get; set; }
        public string DepositedAssetId { get; set; }
        public double FeeAmount { get; set; }
        public string MeTransactionId { get; set; }
        public string Status { get; set; }

        internal void SetPaymentStatus(PaymentStatus data)
        {
            Status = data.ToString();
        }

        internal PaymentStatus GetPaymentStatus()
        {
            return Status.ParseEnum(PaymentStatus.Created);
        }
        PaymentStatus IPaymentTransaction.Status => GetPaymentStatus();

        public string PaymentSystem { get; set; }
        CashInPaymentSystem IPaymentTransaction.PaymentSystem => GetPaymentSystem();

        internal void SetPaymentSystem(CashInPaymentSystem data)
        {
            PaymentSystem = data.ToString();
        }

        internal CashInPaymentSystem GetPaymentSystem()
        {
            return PaymentSystem.ParseEnum(CashInPaymentSystem.Unknown);
        }

        public static PaymentTransactionEntity Create(IPaymentTransaction src)
        {
            var result = new PaymentTransactionEntity
            {
                Created = src.Created,
                TransactionId = src.Id,
                Info = src.Info,
                ClientId = src.ClientId,
                AssetId = src.AssetId,
                WalletId = src.WalletId,
                Amount = src.Amount,
                FeeAmount = src.FeeAmount,
                AggregatorTransactionId = src.AggregatorTransactionId,
                DepositedAssetId = src.DepositedAssetId,
            };
            result.SetPaymentStatus(src.Status);
            result.SetPaymentSystem(src.PaymentSystem);
            return result;
        }

    }
}
