using System;

namespace LykkeApi2.Models.Operations
{
    public class CreateTransferCommand
    {
        public string AssetId { get; set; }
        public double Amount { get; set; }
        public Guid SourceWalletId { get; set; }
        public Guid WalletId { get; set; }
    }
}