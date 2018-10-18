using System.ComponentModel.DataAnnotations;

namespace LykkeApi2.Models.Auth
{
    public class AuthRequestModel
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        public string ClientInfo { get; set; }
        [Required]
        public string PartnerId { get; set; }
    }
}
