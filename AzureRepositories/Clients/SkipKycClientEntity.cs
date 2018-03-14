using Microsoft.WindowsAzure.Storage.Table;

namespace LkeServices.Clients
{
    public class SkipKycClientEntity : TableEntity
    {
        public static string GeneratePartition()
        {
            return "SkipKyc";
        }

        public static string GenerateRowKey(string clientId)
        {
            return clientId;
        }

        public static SkipKycClientEntity Create(string clientId)
        {
            return new SkipKycClientEntity
            {
                PartitionKey = GeneratePartition(),
                RowKey = GenerateRowKey(clientId)
            };
        }

        public string ClientId => RowKey;
    }
}