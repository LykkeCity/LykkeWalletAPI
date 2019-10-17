using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Kyc.Abstractions.Services;
using LykkeApi2.Infrastructure;

namespace LykkeApi2.Services
{
    public class KycStatusValidator
    {
        private readonly IKycStatusService _kycStatusService;
        private readonly IRequestContext _requestContext;

        private static readonly List<KycStatus> AcceptedKycStatusesForPersonalDataUpdate = new List<KycStatus>
            {KycStatus.NeedToFillData, KycStatus.Pending};

        public KycStatusValidator(
            IKycStatusService kycStatusService,
            IRequestContext requestContext)
        {
            _kycStatusService = kycStatusService;
            _requestContext = requestContext;
        }

        public async Task<bool> ValidatePersonalDataUpdateAsync()
        {
            var kycStatus = await _kycStatusService.GetKycStatusAsync(_requestContext.ClientId);

            return AcceptedKycStatusesForPersonalDataUpdate.Contains(kycStatus);
        }

        public async Task<bool> ValidatePersonalDataUpdateForFieldAsync(string value)
        {
            var kycStatus = await _kycStatusService.GetKycStatusAsync(_requestContext.ClientId);

            return kycStatus == KycStatus.Ok && string.IsNullOrEmpty(value) ||
                   AcceptedKycStatusesForPersonalDataUpdate.Contains(kycStatus);
        }
    }
}
