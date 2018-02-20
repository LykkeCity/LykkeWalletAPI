using System.ComponentModel.DataAnnotations;

namespace LykkeApi2.Models.ClientAccountModels
{

    public class AccountRegistrationModel
    {
        [Required]
        public string Email { get; set; }

        public string FullName { get; set; }

        public string ContactPhone { get; set; }

        [Required]
        public string Password { get; set; }

        public string Hint { get; set; }

        public string ClientInfo { get; set; }

        public string PartnerId { get; set; }
    }
}
