using System;

namespace LykkeApi2.Models.Operations
{
    public class CreateTransferRequest
    {
        public string AssetId { get; set; }
        public decimal Amount { get; set; }
        public Guid SourceWalletId { get; set; }
        public Guid WalletId { get; set; }
    }
}