using Lykke.Service.ClientAccountRecovery.Client.AutoRestClient.Models;

namespace Core.Domain.Recovery
{
    /// <summary>
    ///     Current state of password recovery process.
    /// </summary>
    public class RecoveryState
    {
        /// <summary>
        ///     Client email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        ///     Unique id identifying password recovery attempt.
        /// </summary>
        public string RecoveryId { get; set; }

        /// <summary>
        ///     Current challenge provided for client.
        /// </summary>
        public Challenge Challenge { get; set; }
    }
}