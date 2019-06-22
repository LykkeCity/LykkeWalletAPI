using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LykkeApi2.Models.ClientAccountModels
{
    public class UserInfoResponseModel
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public ApiKycStatus KycStatus { get; set; }
    }
    
    public enum ApiKycStatus
    {
        NeedToFillData,
        Pending,
        Ok,
        Rejected,
        RestrictedArea
    }
}