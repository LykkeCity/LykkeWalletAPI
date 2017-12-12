namespace LykkeApi2.Models.History
{
    public enum TransactionStates
    {
        InProcessOnchain,
        SettledOnchain,
        InProcessOffchain,
        SettledOffchain,
        SettledNoChain
    }
}