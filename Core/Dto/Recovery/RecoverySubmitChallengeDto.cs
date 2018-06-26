using Lykke.Service.ClientAccountRecovery.Client.AutoRestClient.Models;

namespace Core.Dto.Recovery
{
    public class RecoverySubmitChallengeDto
    {
        public Action Action { get; set; }
        public string Value { get; set; }
        public string StateToken { get; set; }
        public string Ip { get; set; }
        public string UserAgent { get; set; }
    }
}