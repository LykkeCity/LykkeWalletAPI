namespace Core.Dto.Recovery
{
    public class RecoveryStartDto
    {
        public string Email { get; set; }
        public string PartnerId { get; set; }
        public string Ip { get; set; }
        public string UserAgent { get; set; }
    }
}
