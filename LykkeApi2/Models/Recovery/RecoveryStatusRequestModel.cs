namespace LykkeApi2.Models.Recovery
{
    /// <summary>
    ///     Request model for checking state of recovery process.
    /// </summary>
    public class RecoveryStatusRequestModel
    {
        /// <summary>
        ///     JWE token containing current state of recovery process.
        /// </summary>
        public string StateToken { get; set; }
    }
}