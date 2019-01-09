using Lykke.Service.PersonalData.Contract;
using LykkeApi2.Infrastructure;

namespace LykkeApi2.Services
{
    public class KycCountryValidator
    {
        private readonly IRequestContext _requestContext;
        private readonly IPersonalDataService _personalDataService;

        private const string UnitedKingdomIso3Code = "GBR";

        public KycCountryValidator(
            IRequestContext requestContext, 
            IPersonalDataService personalDataService)
        {
            _requestContext = requestContext;
            _personalDataService = personalDataService;
        }

        public bool IsUnitedKingdom()
        {
            var clientId = _requestContext.ClientId;

            var personalData = _personalDataService.GetAsync(clientId).Result;

            return personalData.CountryFromPOA == UnitedKingdomIso3Code;
        }
    }
}
