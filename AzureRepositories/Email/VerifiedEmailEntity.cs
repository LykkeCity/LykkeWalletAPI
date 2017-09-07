using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Email
{
    public class VerifiedEmailEntity : TableEntity
    {
        public static string GeneratePartion(string partnerId)
        {
            string partition = string.IsNullOrEmpty(partnerId) ? "VerifiedEmail" : $"VerifiedEmail_{partnerId}";
            return partition;
        }

        public static string GenerateRowKey(string email)
        {
            return email;
        }

        public static VerifiedEmailEntity Create(string email, string partnerId)
        {
            return new VerifiedEmailEntity()
            {
                PartitionKey = GeneratePartion(partnerId),
                RowKey = GenerateRowKey(email)
            };
        }
    }
}
