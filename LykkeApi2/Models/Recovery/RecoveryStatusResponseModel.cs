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
        public string Challenge { get; set; }

        /// <summary>
        ///     Overall progress status of password recovery.
        /// </summary>
        public string OverallProgress { get; set; }

        /// <summary>
        ///     Information about challenge.
        /// </summary>
        public string ChallengeInfo { get; set; }
    }
}