using Lykke.Service.ClientAccountRecovery.Client.AutoRestClient.Models;

namespace LykkeApi2.Models.Recovery
{
    /// <summary>
    ///     Request model for submitting data to complete current challenge.
    /// </summary>
    public class RecoverySubmitChallengeRequestModel
    {
        /// <summary>
        ///     JWE token containing current state of recovery process.
        /// </summary>
        public string StateToken { get; set; }

        /// <summary>
        ///     What action to perform on challenge.
        /// </summary>
        public Action Action { get; set; }

        /// <summary>
        ///     Value for submitting the challenge.
        /// </summary>
        public string Value { get; set; }
    }
}