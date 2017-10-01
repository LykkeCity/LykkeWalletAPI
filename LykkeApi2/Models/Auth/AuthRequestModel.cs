namespace LykkeApi2.Models.Auth
{
    public class AuthRequestModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string ClientInfo { get; set; }
        public string PartnerId { get; set; }
    }
}