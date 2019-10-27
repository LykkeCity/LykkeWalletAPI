using Lykke.Service.ClientAccount.Client;
using System.Threading.Tasks;

namespace LykkeApi2.Credentials
{
    public class ClientAccountLogic
    {
        private readonly IClientAccountClient _clientAccountService;

        public ClientAccountLogic(IClientAccountClient clientAccountService)
        {
            _clientAccountService = clientAccountService;
        }

        public async Task<bool> IsTraderWithEmailExistsForPartnerAsync(string email, string partnerId = null)
        {
            string partnerIdAccordingToPolicy = await GetPartnerIdAccordingToSettings(partnerId);
            var result = await _clientAccountService.ClientAccountInformation.GetClientByEmailAndPartnerIdAsync(email, partnerIdAccordingToPolicy);
            return result != null;
        }

        #region PrivateMethods

        /// <summary>
        /// Method returns true if we use different from LykkeWallet credentials else returns false
        /// </summary>
        public async Task<bool> UsePartnerCredentials(string partnerPublicId)
        {
            bool usePartnerCredentials = false;
            if (!string.IsNullOrEmpty(partnerPublicId))
            {
                var policy = await _clientAccountService.PartnerAccountPolicy.GetPartnerAccountPolicyAsync(partnerPublicId);
                usePartnerCredentials = policy?.UseDifferentCredentials ?? false;
            }

            return usePartnerCredentials;
        }

        private async Task<string> GetPartnerIdAccordingToSettings(string partnerPublicId)
        {
            bool usePartnerCredentials = await UsePartnerCredentials(partnerPublicId);
            //Depends on partner settings
            string publicId = !usePartnerCredentials ? null : partnerPublicId;

            return publicId;
        }

        #endregion PrivateMethods
    }
}
