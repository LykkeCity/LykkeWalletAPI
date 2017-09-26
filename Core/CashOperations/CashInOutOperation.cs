using Core.Enumerators;
using System;

namespace Core.CashOperations
{
    public class CashInOutOperation : ICashInOutOperation
    {
        public string Id { get; set; }
        public DateTime DateTime { get; set; }
        public bool IsHidden { get; set; }
        public string AssetId { get; set; }
        public string ClientId { get; set; }
        public double Amount { get; set; }
        public string BlockChainHash { get; set; }
        public string Multisig { get; set; }
        public string TransactionId { get; set; }
        public string AddressFrom { get; set; }
        public string AddressTo { get; set; }
        public bool? IsSettled { get; set; }
        public TransactionStates State { get; set; }
        public bool IsRefund { get; set; }
        public CashOperationType Type { get; set; }

        public static CashInOutOperation CreateNew(string assetId, double amount)
        {
            return new CashInOutOperation
            {
                DateTime = DateTime.UtcNow,
                Amount = amount,
                AssetId = assetId
            };
        }
    }
}
