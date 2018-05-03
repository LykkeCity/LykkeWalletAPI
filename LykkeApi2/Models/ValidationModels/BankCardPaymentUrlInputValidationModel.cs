using System.Threading;
using System.Threading.Tasks;
using Core;
using FluentValidation;
using LkeServices.Operations;
using Lykke.Service.AssetDisclaimers.Client;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Http;

namespace LykkeApi2.Models.ValidationModels
{
    public class BankCardPaymentUrlInputValidationModel : AbstractValidator<BankCardPaymentUrlRequestModel>
    {
        private readonly CachedAssetsDictionary _cachedAssetsDictionary;
        private readonly IAssetDisclaimersClient _assetDisclaimersClient;
        private readonly SrvDisabledOperations _srvDisabledOperations;
        private readonly string _clientId;

        public BankCardPaymentUrlInputValidationModel(
            IHttpContextAccessor httpContextAccessor,
            CachedAssetsDictionary cachedAssetsDictionary,
            IAssetDisclaimersClient assetDisclaimersClient,
            SrvDisabledOperations srvDisabledOperations)
        {
            _cachedAssetsDictionary = cachedAssetsDictionary;
            _assetDisclaimersClient = assetDisclaimersClient;
            _srvDisabledOperations = srvDisabledOperations;
            _clientId = httpContextAccessor.HttpContext.User?.Identity?.Name;

            RegisterRules();
        }

        private void RegisterRules()
        {
            RuleFor(reg => reg.AssetId).MustAsync(IsNotApprovedDepositDisclaimers).WithMessage(Phrases.PendingDisclaimer);
            RuleFor(reg => reg.AssetId).MustAsync(IsNotApprovedDepositDisclaimers).WithMessage(Phrases.BtcDisabledMsg);

            RuleFor(reg => reg.Amount).Must(x => x <= 0).WithMessage(x => string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(x.Amount)));

        }

        private async Task<bool> IsNotApprovedDepositDisclaimers(string value, CancellationToken cancellationToken)
        {
            var depositAsset = await _cachedAssetsDictionary.GetItemAsync(value);

            if (!string.IsNullOrEmpty(depositAsset.LykkeEntityId))
            {
                var checkDisclaimerResult =
                    await _assetDisclaimersClient.CheckDepositClientDisclaimerAsync(_clientId, depositAsset.LykkeEntityId);

                return !checkDisclaimerResult.RequiresApproval;
            }

            return true;
        }

        private async Task<bool> IsOperationForAssetDisabled(string value, CancellationToken cancellationToken)
        {
            return !await _srvDisabledOperations.IsOperationForAssetDisabled(value);
        }

        private async Task<bool> IsOperationForAssetDisabled(string value, CancellationToken cancellationToken)
        {
            return !await _srvDisabledOperations.IsOperationForAssetDisabled(value);
        }


    }
}