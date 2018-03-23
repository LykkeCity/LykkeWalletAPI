using System;
using System.Threading.Tasks;
using AzureStorage;
using Core.Identity;

namespace LkeServices.Identity
{
    public class IdentityRepository : IIdentityRepository
    {
        private readonly INoSQLTableStorage<IdentityEntity> _tableStorage;

        public IdentityRepository(INoSQLTableStorage<IdentityEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<int> GenerateNewIdAsync()
        {
            var identityEntity = IdentityEntity.Create();
            await
                _tableStorage
                    .InsertOrModifyAsync(
                        IdentityEntity.GeneratePartitionKey, 
                        IdentityEntity.GenerateRowKey,
                        () => identityEntity, indEnt => 
                        {
                            indEnt.Value++;
                            return true;
                        }
                    );
            return identityEntity.Value;
        }
    }
}