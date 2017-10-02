using AzureRepositories.Email;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Core.Messages;
using System.Threading.Tasks;

namespace AzureRepositories.Repositories
{
    public class VerifiedEmailsRepository : IVerifiedEmailsRepository
    {
        private readonly INoSQLTableStorage<VerifiedEmailEntity> _tableStorage;

        public VerifiedEmailsRepository(INoSQLTableStorage<VerifiedEmailEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task AddOrReplaceAsync(string email, string partnerId)
        {
            var entity = VerifiedEmailEntity.Create(email, partnerId);
            return _tableStorage.InsertOrReplaceAsync(entity);
        }

        public async Task<bool> IsEmailVerified(string email, string partnerId)
        {
            var entity = await _tableStorage.GetDataAsync(VerifiedEmailEntity.GeneratePartion(partnerId),
                VerifiedEmailEntity.GenerateRowKey(email));
            return entity != null;
        }        
    }
}
