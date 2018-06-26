namespace LykkeApi2.Models.Recovery
{
    /// <summary>
    ///     Model for completing password recovery process.
    /// </summary>
    public class RecoveryCompleteRequestModel
    {
        /// <summary>
        ///     Password hash.
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        ///     JWE token containing current state of recovery process.
        /// </summary>
        public string StateToken { get; set; }

        /// <summary>
        ///     Pin.
        /// </summary>
        public string Pin { get; set; }

        /// <summary>
        ///     Hint for password.
        /// </summary>
        public string Hint { get; set; }
    }
}