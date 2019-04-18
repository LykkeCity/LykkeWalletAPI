using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Core.Constants;
using Core.Services;
using FluentValidation;
using Lykke.Service.AssetDisclaimers.Client;
using Lykke.Service.Assets.Client;
using Lykke.Service.Balances.Client;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Limitations.Client;
using Lykke.Service.PaymentSystem.Client;
using Lykke.Service.PaymentSystem.Client.AutorestClient.Models;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.PersonalData.Contract.Models;
using LykkeApi2.Infrastructure;
using LykkeApi2.Infrastructure.Extensions;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Http;

namespace LykkeApi2.Models.ValidationModels
{
    public class FxPaygatePaymentUrlInputValidationModel : AbstractValidator<FxPaygatePaymentUrlRequestModel>
    {
        private readonly IAssetsHelper _assetsHelper;
        private readonly IAssetDisclaimersClient _assetDisclaimersClient;
        private readonly PaymentLimitsResponse _paymentLimitsResponse;
        private readonly IPersonalData _personalData;
        private readonly IClientAccountClient _clientAccountService;
        private readonly IBalancesClient _balancesClient;
        private readonly IAssetsService _assetsService;
        private readonly IKycStatusService _kycStatusService;
        private readonly ILimitationsServiceClient _limitationsServiceClient;
        private readonly string _clientId;
        private readonly PaymentMethodsResponse _paymentMethods;

        public FxPaygatePaymentUrlInputValidationModel(
            IHttpContextAccessor httpContextAccessor,
            IAssetsHelper assetHelper,
            IAssetDisclaimersClient assetDisclaimersClient,
            IPaymentSystemClient paymentSystemClient,
            IPersonalDataService personalDataService,
            IClientAccountClient clientAccountService,
            IBalancesClient balancesClient,
            IAssetsService assetsService,
            IKycStatusService kycStatusService,
            ILimitationsServiceClient limitationsServiceClient)
        {
            _assetsHelper = assetHelper;
            _assetDisclaimersClient = assetDisclaimersClient;
            _clientAccountService = clientAccountService;
            _balancesClient = balancesClient;
            _assetsService = assetsService;
            _kycStatusService = kycStatusService;
            _limitationsServiceClient = limitationsServiceClient;

            _clientId = httpContextAccessor.HttpContext.User?.Identity?.Name;
            _paymentLimitsResponse = paymentSystemClient.GetPaymentLimitsAsync().GetAwaiter().GetResult();
            _personalData = personalDataService.GetAsync(_clientId).GetAwaiter().GetResult();
            
            _paymentMethods = paymentSystemClient.GetPaymentMethodsAsync(_clientId).GetAwaiter().GetResult();

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
            RuleFor(reg => reg.Amount).Must(x => x <= _paymentLimitsResponse.FxpaygateMaxValue)
                .WithMessage((x, y) =>
                {
                    var asset = _assetsHelper.GetAssetAsync(x.AssetId).GetAwaiter().GetResult();
                    
                    return string.Format(Phrases.PaymentIsMoreThanMaxLimit, _paymentLimitsResponse.FxpaygateMaxValue, asset.DisplayId ?? asset.Id);
                });
            RuleFor(reg => reg.Amount).MustAsync(IsValidLimitation).WithMessage(Phrases.LimitIsExceeded);

            RuleFor(reg => reg.FirstName).Must(x => !string.IsNullOrEmpty(x))
                .WithMessage(x => string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(x.FirstName)));
            RuleFor(reg => reg.FirstName).Must((x, y) => x.FirstName.Length + x.LastName.Length < LykkeConstants.MaxFullNameLength)
                .WithMessage(x => string.Format(Phrases.FullNameLengthFormat, LykkeConstants.MaxFullNameLength));

            RuleFor(reg => reg.LastName).Must(x => !string.IsNullOrEmpty(x))
                .WithMessage(x => string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(x.LastName)));

            RuleFor(reg => reg.City).Must(x => !string.IsNullOrEmpty(x))
                .WithMessage(x => string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(x.City)));
            RuleFor(reg => reg.City).MaximumLength(LykkeConstants.MaxCityLength)
                .WithMessage(x => string.Format(Phrases.MaxLength, LykkeConstants.MaxCityLength));

            RuleFor(reg => reg.Zip).Must(x => !string.IsNullOrEmpty(x))
                .WithMessage(x => string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(x.Zip)));
            RuleFor(reg => reg.Zip).MaximumLength(LykkeConstants.MaxZipLength)
                .WithMessage(x => string.Format(Phrases.MaxLength, LykkeConstants.MaxZipLength));

            RuleFor(reg => reg.Address).Must(x => !string.IsNullOrEmpty(x))
                .WithMessage(x => string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(x.Address)));
            RuleFor(reg => reg.Address).MaximumLength(LykkeConstants.MaxAddressLength)
                .WithMessage(x => string.Format(Phrases.MaxLength, LykkeConstants.MaxAddressLength));

            RuleFor(reg => reg.Country).Must(x => !string.IsNullOrEmpty(x))
                .WithMessage(x => string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(x.Country)));

            RuleFor(reg => reg.Email).Must(x => !string.IsNullOrEmpty(x))
                .WithMessage(x => string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(x.Email)));
            RuleFor(reg => reg.Email).MaximumLength(LykkeConstants.MaxEmailLength)
                .WithMessage(x => string.Format(Phrases.MaxLength, LykkeConstants.MaxEmailLength));
            RuleFor(reg => reg.Email).EmailAddress().Must(IsValidPartitionOrRowKey)
                .WithMessage(x => Phrases.InvalidEmailFormat);
            RuleFor(reg => reg.Email).Must(IsValidPersonalEmail)
                .WithMessage(x => string.Format(Phrases.NotMatchWithPersonalData, nameof(x.Email)));

            RuleFor(reg => reg.Phone).Must(x => !string.IsNullOrEmpty(x))
                .WithMessage(x => string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(x.Phone)));
            RuleFor(reg => reg.Phone).Must(IsValidPhoneNumberE164)
                .WithMessage(x => Phrases.InvalidNumberFormat);
            RuleFor(reg => reg.Phone).Must(IsValidPersonalContactPhone)
                .WithMessage(x => string.Format(Phrases.NotMatchWithPersonalData, nameof(x.Phone)));

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
            return !(await _clientAccountService.GetDepositBlockAsync(_clientId)).DepositViaCreditCardBlocked;
        }

        private async Task<bool> IsBackupNotRequired(string value, CancellationToken cancellationToken)
        {
            var backupSettings = await _clientAccountService.GetBackupAsync(_clientId);
            var wallets = await _balancesClient.GetClientBalances(_clientId);
            return wallets.All(x => x.Balance <= 0) || backupSettings.BackupDone;
        }

        private async Task<bool> IsAllowedToCashInViaBankCardAsync(string value, CancellationToken cancellationToken)
        {
            return (await _assetsService.ClientIsAllowedToCashInViaBankCardAsync(_clientId, false, cancellationToken)).Value;
        }

        private async Task<bool> IsKycNotNeeded(string value, CancellationToken cancellationToken)
        {
            var asset = await _assetsHelper.GetAssetAsync(value);

            var userKycStatus = await _kycStatusService.GetKycStatusAsync(_clientId);

            return asset?.KycNeeded != true || userKycStatus.IsKycOkOrReviewDone();
        }

        private async Task<bool> IsValidLimitation(FxPaygatePaymentUrlRequestModel model, double value,
            CancellationToken cancellationToken)
        {
            var checkResult = await _limitationsServiceClient.CheckAsync(
                _clientId,
                model.AssetId,
                value,
                CurrencyOperationType.CardCashIn);
            return checkResult.IsValid;
        }
    }
}
