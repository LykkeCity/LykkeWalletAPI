namespace LykkeApi2.Models.ApiContractModels
{
    public class ApiCashOutAttempt
    {
        public string Id { get; set; }
        public string DateTime { get; set; }
        public string Asset { get; set; }
        public double Volume { get; set; }
        public string IconId { get; set; }
        public string ClientId { get; set; }
    }
}
