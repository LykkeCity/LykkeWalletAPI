using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Antares.Service.Assets.Client;
using Common;
using Core.Constants;
using Core.Services;
using FluentValidation;
using FluentValidation.Validators;
using Lykke.Service.AssetDisclaimers.Client;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Assets.Core.Domain;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.PaymentSystem.Client;
using Lykke.Service.PaymentSystem.Client.AutorestClient.Models;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.PersonalData.Contract.Models;
using Lykke.Service.RateCalculator.Client;
using Lykke.Service.Tier.Client;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Http;

namespace LykkeApi2.Models.ValidationModels
{
    public class FxPaygatePaymentUrlInputValidationModel : AbstractValidator<PaymentUrlRequestModel>
    {
        private readonly IAssetsHelper _assetsHelper;
        private readonly IAssetDisclaimersClient _assetDisclaimersClient;
        private readonly PaymentLimitsResponse _paymentLimitsResponse;
        private readonly IPersonalData _personalData;
        private readonly IClientAccountClient _clientAccountService;
        private readonly IAssetsServiceClient _assetsServiceClient;
        private readonly IKycStatusService _kycStatusService;
        private readonly ITierClient _tierClient;
        private readonly IRateCalculatorClient _rateCalculatorClient;
        private readonly string _clientId;

        public FxPaygatePaymentUrlInputValidationModel(
            IHttpContextAccessor httpContextAccessor,
            IAssetsHelper assetHelper,
            IAssetDisclaimersClient assetDisclaimersClient,
            IPaymentSystemClient paymentSystemClient,
            IPersonalDataService personalDataService,
            IClientAccountClient clientAccountService,
            IAssetsServiceClient assetsServiceClient,
            IKycStatusService kycStatusService,
            ITierClient tierClient,
            IRateCalculatorClient rateCalculatorClient)
        {
            _assetsHelper = assetHelper;
            _assetDisclaimersClient = assetDisclaimersClient;
            _clientAccountService = clientAccountService;
            _assetsServiceClient = assetsServiceClient;
            _kycStatusService = kycStatusService;
            _tierClient = tierClient;
            _rateCalculatorClient = rateCalculatorClient;

            _clientId = httpContextAccessor.HttpContext.User?.Identity?.Name;
            var paymentLimitsTask = paymentSystemClient.GetPaymentLimitsAsync();
            var pdTask = personalDataService.GetAsync(_clientId);

            Task.WhenAll(paymentLimitsTask, pdTask).GetAwaiter().GetResult();

            _paymentLimitsResponse = paymentLimitsTask.Result;
            _personalData = pdTask.Result;
            RegisterRules();
        }

        private void RegisterRules()
        {
            RuleFor(reg => reg.AssetId).MustAsync(IsApprovedDepositDisclaimers).WithMessage(Phrases.PendingDisclaimer);
            RuleFor(reg => reg.AssetId).MustAsync(IsKycNotNeeded).WithMessage(Phrases.KycNeeded);

            RuleFor(reg => reg.Amount).Must(x => x > 0)
                .WithMessage(x => string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(x.Amount)));

            RuleFor(reg => reg.Amount).Must(x => x >= _paymentLimitsResponse.FxpaygateMinValue)
                .WithMessage((x, y) =>
                {
                    var asset = _assetsHelper.GetAssetAsync(x.AssetId).GetAwaiter().GetResult();

                    return string.Format(Phrases.PaymentIsLessThanMinLimit, _paymentLimitsResponse.FxpaygateMinValue, asset.DisplayId ?? asset.Id);
                });

            RuleFor(reg => reg).CustomAsync(IsMaxAmountValidAsync);

            RuleFor(reg => reg.WalletId).MustAsync(IsDepositViaCreditCardNotBlocked)
                .WithMessage(x => Phrases.OperationProhibited);

            RuleFor(reg => reg.WalletId).MustAsync(IsAllowedToCashInViaBankCardAsync)
                .WithMessage(x => Phrases.OperationProhibited);
        }

        private async Task<bool> IsApprovedDepositDisclaimers(string value, CancellationToken cancellationToken)
        {
            var depositAsset = await _assetsHelper.GetAssetAsync(value);

            if (!string.IsNullOrEmpty(depositAsset?.LykkeEntityId))
            {
                var checkDisclaimerResult =
                    await _assetDisclaimersClient.CheckDepositClientDisclaimerAsync(_clientId, depositAsset.LykkeEntityId);

                return !checkDisclaimerResult.RequiresApproval;
            }
            return true;
        }

        private bool IsValidPartitionOrRowKey(string value)
        {
            return !Regex.IsMatch(value, @"[\p{C}|/|\\|#|?]");
        }

        private bool IsValidPhoneNumberE164(string value)
        {
            var phoneNumberE164 = value.PreparePhoneNum().ToE164Number();
            return phoneNumberE164 != null && value.Length <= LykkeConstants.MaxPhoneLength;
        }

        private bool IsValidPersonalEmail(string value)
        {
            return _personalData.Email == value;
        }

        private bool IsValidPersonalContactPhone(string value)
        {
            return _personalData.ContactPhone == value;
        }

        private async Task<bool> IsDepositViaCreditCardNotBlocked(string value, CancellationToken cancellationToken)
        {
            return !(await _clientAccountService.ClientSettings.GetDepositBlockSettingsAsync(_clientId)).DepositViaCreditCardBlocked;
        }

        private async Task<bool> IsAllowedToCashInViaBankCardAsync(string value, CancellationToken cancellationToken)
        {
            return (await _assetsServiceClient.HttpClient.ClientIsAllowedToCashInViaBankCardAsync(_clientId, false, cancellationToken)).Value;
        }

        private async Task<bool> IsKycNotNeeded(string value, CancellationToken cancellationToken)
        {
            var isKycNeeded = await _kycStatusService.IsKycNeededAsync(_clientId);
            return !isKycNeeded;
        }

        private async Task IsMaxAmountValidAsync(PaymentUrlRequestModel model, CustomContext context, CancellationToken cancellationToken)
        {
            var tierInfo = await _tierClient.Tiers.GetClientTierInfoAsync(_clientId);

            var maxLimitTask = GetAmountInOperationAssetAsync(tierInfo.CurrentTier.Asset, model.AssetId, tierInfo.CurrentTier.MaxLimit);
            var currentDepositTask = GetAmountInOperationAssetAsync(tierInfo.CurrentTier.Asset, model.AssetId, tierInfo.CurrentTier.Current);
            var assetTask = _assetsHelper.GetAssetAsync(model.AssetId);

            await Task.WhenAll(maxLimitTask, currentDepositTask, assetTask);

            double maxLimit = maxLimitTask.Result;
            double currentDeposit = currentDepositTask.Result;
            IAsset asset = assetTask.Result;
            double allowedToMax = maxLimit - currentDeposit;
            double maxAmountPerTransaction = 0;

            maxAmountPerTransaction = maxLimit < _paymentLimitsResponse.CreditVouchersMaxValue
                ? maxLimit
                : _paymentLimitsResponse.CreditVouchersMaxValue;

            if (model.Amount > maxAmountPerTransaction)
            {
                context.AddFailure(nameof(model.Amount), $"Per transaction, you can deposit up to {maxAmountPerTransaction:F2} {asset.DisplayId ?? asset.Id}");
                return;
            }

            string upgradeAccountText = tierInfo.NextTier != null
                ? "If you wish to increase limit, just upgrade your account"
                : string.Empty;

            if (allowedToMax > 0 && model.Amount > allowedToMax)
            {
                context.AddFailure(nameof(model.Amount), $"You can deposit up to {allowedToMax:F2} {asset.DisplayId ?? asset.Id}. {upgradeAccountText}");
            }
        }

        private async Task<double> GetAmountInOperationAssetAsync(string tierAssetId, string assetId, double amount)
        {
            return tierAssetId == assetId
                ? amount
                : await _rateCalculatorClient.GetAmountInBaseAsync(tierAssetId, amount, assetId);
        }
    }
}
