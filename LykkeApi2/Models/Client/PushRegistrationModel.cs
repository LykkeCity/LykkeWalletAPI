using System.ComponentModel.DataAnnotations;
using Lykke.Service.PushNotifications.Contract.Enums;

namespace LykkeApi2.Models.Client
{
    public class PushRegistrationModel
    {
        public string InstallationId { get; set; }
        [Required]
        public MobileOs Platform { get; set; }
        [Required]
        public string PushChannel { get; set; }
    }
}
