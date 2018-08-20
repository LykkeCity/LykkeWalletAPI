namespace LykkeApi2.Models.Recovery
{
    /// <summary>
    ///     Response for starting recovery process.
    /// </summary>
    public class RecoveryStartResponseModel
    {
        /// <summary>
        ///     JWE token containing current state of recovery process.
        /// </summary>
        public string StateToken { get; set; }
    }
}