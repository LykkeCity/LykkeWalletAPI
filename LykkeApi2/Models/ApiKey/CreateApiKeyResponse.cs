namespace LykkeApi2.Models.ApiKey
{
    public class CreateApiKeyResponse
    {
        public string ApiKey { get; set; }
        public string WalletId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Apiv2Only { get; set; }
    }
}
