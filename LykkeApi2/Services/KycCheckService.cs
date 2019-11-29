using System.Threading.Tasks;
using Core.Services;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Tier.Client;

namespace LykkeApi2.Services
{
    public class KycCheckService : IKycCheckService
    {
        private readonly IKycStatusService _kycStatusService;
        private readonly ITierClient _tierClient;

        public KycCheckService(
            IKycStatusService kycStatusService,
            ITierClient tierClient
        )
        {
            _kycStatusService = kycStatusService;
            _tierClient = tierClient;
        }
        public async Task<bool> IsKycNeededAsync(string clientId)
        {
            KycStatus kycStatus = await _kycStatusService.GetKycStatusAsync(clientId);

            switch (kycStatus)
            {
                case KycStatus.NeedToFillData:
                case KycStatus.RestrictedArea:
                case KycStatus.Rejected:
                case KycStatus.Complicated:
                case KycStatus.JumioInProgress:
                case KycStatus.JumioOk:
                case KycStatus.JumioFailed:
                case KycStatus.ReviewDone:
                    return true;
                case KycStatus.Pending:
                    var tierInfo = await _tierClient.Tiers.GetClientTierInfoAsync(clientId);
                    return tierInfo.CurrentTier.Current > tierInfo.CurrentTier.MaxLimit;
                case KycStatus.Ok:
                    return false;
                default:
                    return false;
            }
        }
    }
}
