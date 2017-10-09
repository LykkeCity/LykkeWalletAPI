namespace Core.Accounts
{
    public interface IWallet
    {
        double Balance { get; }
        string AssetId { get; }
    }
}
