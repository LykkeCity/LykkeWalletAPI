namespace Core.Bitcoin
{
    public interface IWalletCredentialsRepository
    {
        Task SaveAsync(IWalletCredentials walletCredentials);
        Task MergeAsync(IWalletCredentials walletCredentials);
        Task<IWalletCredentials> GetAsync(string clientId);
        Task<IWalletCredentials> GetByEthConversionWalletAsync(string ethWallet);
        Task<IWalletCredentials> GetBySolarCoinWalletAsync(string address);
        Task<IWalletCredentials> GetByChronoBankContractAsync(string contract);
        Task<IWalletCredentials> GetByQuantaContractAsync(string contract);
        Task<string> GetClientIdByMultisig(string multisig);
        Task SetPreventTxDetection(string clientId, bool value);
        Task SetEncodedPrivateKey(string clientId, string encodedPrivateKey);
        Task SetEthConversionWallet(string clientId, string contract);
        Task SetEthFieldsWallet(string clientId, string contract, string address, string pubKey);
        Task SetSolarCoinWallet(string clientId, string address);
        Task SetChronoBankContract(string clientId, string contract);
        Task SetQuantaContract(string clientId, string contract);
        Task<IWalletCredentials> ScanAndFind(Func<IWalletCredentials, bool> item);
        Task ScanAllAsync(Func<IEnumerable<IWalletCredentials>, Task> chunk);
    }
}