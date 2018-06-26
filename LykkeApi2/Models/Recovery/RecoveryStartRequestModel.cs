namespace LykkeApi2.Models.Recovery
{
    /// <summary>
    ///     Model for starting password recovery process.
    /// </summary>
    public class RecoveryStartRequestModel
    {
        /// <summary>
        ///     Client email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        ///     Client partner id.
        /// </summary>
        public string PartnerId { get; set; }
    }
}