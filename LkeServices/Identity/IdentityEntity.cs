using Microsoft.WindowsAzure.Storage.Table;

namespace LkeServices.Identity
{
    public class IdentityEntity : TableEntity
    {
        public const string GeneratePartitionKey = "Id";
        public const string GenerateRowKey = "Id";
        public int Value { get; set; }

        public static IdentityEntity Create()
        {
            return new IdentityEntity
            {
                PartitionKey = GeneratePartitionKey,
                RowKey = GenerateRowKey,
                Value = 0
            };
        }
    }
}