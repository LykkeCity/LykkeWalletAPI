using System;

namespace LykkeApi2.Models.Operations
{
    public class CreateTransferCommand
    {
        public Guid ClientId { get; set; }
        public string AssetId { get; set; }
        public decimal Amount { get; set; }
        public Guid WalletId { get; set; }
    }
}