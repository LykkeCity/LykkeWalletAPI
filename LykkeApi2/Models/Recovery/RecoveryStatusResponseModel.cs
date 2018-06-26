using Lykke.Service.ClientAccountRecovery.Client.AutoRestClient.Models;

namespace LykkeApi2.Models.Recovery
{
    /// <summary>
    ///     Response model containing information about current password recovery state.
    /// </summary>
    public class RecoveryStatusResponseModel
    {
        /// <summary>
        ///     Current challenge provided for client.
        /// </summary>
        public Challenge Challenge { get; set; }

        /// <summary>
        ///     Overall progress status of password recovery.
        /// </summary>
        public Progress OverallProgress { get; set; }

        /// <summary>
        ///     Information about challenge.
        /// </summary>
        public string ChallengeInfo { get; set; }
    }
}