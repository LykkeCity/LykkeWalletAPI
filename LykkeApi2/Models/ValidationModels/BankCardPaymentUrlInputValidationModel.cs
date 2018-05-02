using System.Threading;
using System.Threading.Tasks;
using Core;
using FluentValidation;
using Lykke.Service.AssetDisclaimers.Client;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Http;

namespace LykkeApi2.Models.ValidationModels
{
    public class BankCardPaymentUrlInputValidationModel : AbstractValidator<BankCardPaymentUrlRequestModel>
    {
        private readonly CachedAssetsDictionary _cachedAssetsDictionary;
        private readonly IAssetDisclaimersClient _assetDisclaimersClient;
        private readonly string _clientId;

        public BankCardPaymentUrlInputValidationModel(
            IHttpContextAccessor httpContextAccessor,
            CachedAssetsDictionary cachedAssetsDictionary,
            IAssetDisclaimersClient assetDisclaimersClient)
        {
            _cachedAssetsDictionary = cachedAssetsDictionary;
            _assetDisclaimersClient = assetDisclaimersClient;

            _clientId = httpContextAccessor.HttpContext.User?.Identity?.Name;

            RegisterRules();
        }

        private void RegisterRules()
        {
            RuleFor(reg => reg.Email).MustAsync(IsNotApprovedDepositDisclaimers).WithMessage(Phrases.PendingDisclaimer);
        }

        private async Task<bool> IsNotApprovedDepositDisclaimers(string value, CancellationToken cancellationToken)
        {
            var depositAsset = await _cachedAssetsDictionary.GetItemAsync(value);

            if (string.IsNullOrEmpty(depositAsset.LykkeEntityId)) return true;
            var checkDisclaimerResult =
                await _assetDisclaimersClient.CheckDepositClientDisclaimerAsync(_clientId, depositAsset.LykkeEntityId);

            return !checkDisclaimerResult.RequiresApproval;
        }
    }
}