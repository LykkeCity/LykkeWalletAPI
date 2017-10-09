using Core.Enumerators;

namespace Core.CashOperations
{
    public interface IBaseCashBlockchainOperation : IBaseCashOperation
    {
        string BlockChainHash { get; set; }

        string Multisig { get; }

        /// <summary>
        /// Bitcoin queue record id (BitCointTransaction)
        /// </summary>
        string TransactionId { get; }

        string AddressFrom { get; set; }

        string AddressTo { get; set; }

        bool? IsSettled { get; set; }

        TransactionStates State { get; set; }
    }
}
