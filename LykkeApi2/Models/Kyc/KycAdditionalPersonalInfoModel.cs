using Lykke.Service.Kyc.Abstractions.Domain.Verification;

namespace LykkeApi2.Models.Kyc
{
    public class KycAdditionalPersonalInfoModel : KycAdditionalInfoModel
    {
        public string ClientId { get; private set; }
        public string Country { get; private set; }
        public KycStatus KycStatus { get; private set; }

        public static KycAdditionalPersonalInfoModel Create(KycAdditionalInfoModel src, string clientId, string country, KycStatus kycStatus)
        {
            return new KycAdditionalPersonalInfoModel
            {
                ClientId = clientId,
                DateOfBirth = src.DateOfBirth,
                Zip = src.Zip,
                Address = src.Address,
                Country = country,
                KycStatus = kycStatus
            };
        }
    }
}