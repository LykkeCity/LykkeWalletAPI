namespace LykkeApi2.Models.Recovery
{
    /// <summary>
    ///     Response model for submitting a chalenge.
    /// </summary>
    public class RecoverySubmitChallengeResponseModel
    {
        /// <summary>
        ///     JWE token containing new state of recovery process.
        /// </summary>
        public string StateToken { get; set; }
    }
}