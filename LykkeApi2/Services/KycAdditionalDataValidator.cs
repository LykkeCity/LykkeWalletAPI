using System.Threading.Tasks;
using FluentValidation.Results;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.PersonalData.Contract;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Kyc;
using LykkeApi2.Models.ValidationModels;

namespace LykkeApi2.Services
{
    public class KycAdditionalDataValidator
    {
        private readonly IPersonalDataService _personalDataService;
        private readonly IKycStatusService _kycStatusService;
        private readonly IRequestContext _requestContext;

        public KycAdditionalDataValidator(
            IKycStatusService kycStatusService, 
            IPersonalDataService personalDataService, 
            IRequestContext requestContext)
        {
            _kycStatusService = kycStatusService;
            _personalDataService = personalDataService;
            _requestContext = requestContext;
        }

        public async Task<ValidationResult> ValidateAsync(KycAdditionalInfoModel model)
        {
            var personalData = await _personalDataService.GetAsync(_requestContext.ClientId);

            var kycStatus = await _kycStatusService.GetKycStatusAsync(_requestContext.ClientId);

            var personalInfoModel = KycAdditionalPersonalInfoModel.Create(
                model,
                _requestContext.ClientId,
                personalData.Country,
                kycStatus);

            return new KycAdditionalPersonalInfoValidationModel().Validate(personalInfoModel);
        }
    }
}