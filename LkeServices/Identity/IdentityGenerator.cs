using System.Threading.Tasks;
using AzureStorage;
using Core.Identity;

namespace LkeServices.Identity
{
    public class IdentityGenerator : IIdentityGenerator
    {
        private readonly INoSQLTableStorage<IdentityEntity> _tableStorage;

        public IdentityGenerator(INoSQLTableStorage<IdentityEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<int> GenerateNewIdAsync()
        {
            var entity =
                await
                    _tableStorage
                        .InsertOrModifyAsync(
                            IdentityEntity.GeneratePartitionKey, 
                            IdentityEntity.GenerateRowKey,
                            IdentityEntity.Create,
                        itm =>
                        {
                            itm.Value++;
                            return itm;
                        }
                    );
            return entity.Value;
        }
    }
}