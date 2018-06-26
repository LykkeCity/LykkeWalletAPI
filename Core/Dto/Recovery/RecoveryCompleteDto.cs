namespace Core.Dto.Recovery
{
    /// <summary>
    /// DTO for completing password recovery process.
    /// </summary>
    public class RecoveryCompleteDto
    {
        /// <summary>
        /// Password hash.
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string StateToken { get; set; }
        public string Pin { get; set; }        
        public string Hint { get; set; }
        public string Ip { get; set; }
        public string UserAgent { get; set; }
    }
}