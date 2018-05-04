using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LykkeApi2.Models.ClientAccountModels
{
    public class UserInfoResponseModel
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public KycStatus KycStatus { get; set; }
    }
}