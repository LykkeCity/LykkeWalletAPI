using System.Threading.Tasks;
using AzureStorage;
using Core.Domain.BitCoin;
using Core.Repositories;

namespace AzureRepositories.Bitcoin
{
    public class WalletCredentialsRepository : IWalletCredentialsRepository
    {
        private readonly INoSQLTableStorage<WalletCredentialsEntity> _tableStorage;

        public WalletCredentialsRepository(INoSQLTableStorage<WalletCredentialsEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IWalletCredentials> GetAsync(string clientId)
        {
            var partitionKey = WalletCredentialsEntity.ByClientId.GeneratePartitionKey();
            var rowKey = WalletCredentialsEntity.ByClientId.GenerateRowKey(clientId);

            var entity = await _tableStorage.GetDataAsync(partitionKey, rowKey);

            if (entity == null)
                return null;

            return string.IsNullOrEmpty(entity.MultiSig) ? null : entity;
        }
    }
}