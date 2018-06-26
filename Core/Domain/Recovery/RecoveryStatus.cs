using Lykke.Service.ClientAccountRecovery.Client.AutoRestClient.Models;

namespace Core.Domain.Recovery
{
    /// <summary>
    ///     Information about current password recovery state.
    /// </summary>
    public class RecoveryStatus
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