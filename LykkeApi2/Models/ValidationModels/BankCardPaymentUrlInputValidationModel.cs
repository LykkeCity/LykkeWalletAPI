using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Core;
using Core.Constants;
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
    public class BankCardPaymentUrlInputValidationModel : AbstractValidator<BankCardPaymentUrlRequestModel>
    {
        private readonly CachedAssetsDictionary _cachedAssetsDictionary;
        private readonly IAssetDisclaimersClient _assetDisclaimersClient;
        private readonly PaymentLimitsResponse _paymentLimitsResponse;
        private readonly IPersonalData _personalData;
        private readonly IClientAccountClient _clientAccountService;
        private readonly IBalancesClient _balancesClient;
        private readonly IAssetsService _assetsService;
        private readonly CachedTradableAssetsDictionary _tradableAssetsDictionary;
        private readonly IKycStatusService _kycStatusService;
        private readonly ILimitationsServiceClient _limitationsServiceClient;
        private readonly string _clientId;
        private readonly bool _isIosDevice;
        private readonly PaymentMethodsResponse _paymentMethods;

        public BankCardPaymentUrlInputValidationModel(
            IHttpContextAccessor httpContextAccessor,
            CachedAssetsDictionary cachedAssetsDictionary,
            IAssetDisclaimersClient assetDisclaimersClient,
            IPaymentSystemClient paymentSystemClient,
            IPersonalDataService personalDataService,
            IClientAccountClient clientAccountService,
            IBalancesClient balancesClient,
            IAssetsService assetsService,
            CachedTradableAssetsDictionary tradableAssetsDictionary,
            IKycStatusService kycStatusService,
            ILimitationsServiceClient limitationsServiceClient)
        {
            _cachedAssetsDictionary = cachedAssetsDictionary;
            _assetDisclaimersClient = assetDisclaimersClient;
            _clientAccountService = clientAccountService;
            _balancesClient = balancesClient;
            _assetsService = assetsService;
            _tradableAssetsDictionary = tradableAssetsDictionary;
            _kycStatusService = kycStatusService;
            _limitationsServiceClient = limitationsServiceClient;

            _clientId = httpContextAccessor.HttpContext.User?.Identity?.Name;
            _paymentLimitsResponse = paymentSystemClient.GetPaymentLimitsAsync().GetAwaiter().GetResult();
            _personalData = personalDataService.GetAsync(_clientId).GetAwaiter().GetResult();

            _isIosDevice = IsIosDevice(httpContextAccessor.HttpContext);
            _paymentMethods = paymentSystemClient.GetPaymentMethodsAsync(_clientId).GetAwaiter().GetResult();

            RegisterRules();
        }
        private static bool IsIosDevice(HttpContext context)
        {
            var userAgentVariables =
                UserAgentHelper.ParseUserAgent(context.Request.GetUserAgent().ToLower());

            if (!userAgentVariables.ContainsKey(UserAgentVariablesLowercase.DeviceType))
            {
                return false;
            }

            return userAgentVariables[UserAgentVariablesLowercase.DeviceType] == DeviceTypesLowercase.IPad ||
                   userAgentVariables[UserAgentVariablesLowercase.DeviceType] == DeviceTypesLowercase.IPhone;
        }

        private void RegisterRules()
        {
            RuleFor(reg => reg.AssetId).MustAsync(IsApprovedDepositDisclaimers).WithMessage(Phrases.PendingDisclaimer);
            RuleFor(reg => reg.AssetId).MustAsync(IsKycNotNeeded).WithMessage(Phrases.KycNeeded);
            RuleFor(reg => reg.AssetId).Must(IsOtherDepositOptionsEnabled)
                .WithMessage(x => string.Format(Phrases.DepositOptionsNotAllowFormat, x.AssetId, x.DepositOption));
            RuleFor(reg => reg.AssetId).Must(IsBankCardDepositOptionsEnabled)
                .WithMessage(x => string.Format(Phrases.DepositOptionsNotAllowFormat, x.AssetId, x.DepositOption));

            RuleFor(reg => reg.Amount).Must(x => x > 0)
                .WithMessage(x => string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(x.Amount)));
            RuleFor(reg => reg.Amount).Must(IsMinAmount)
                .WithMessage(x => string.Format(Phrases.PaymentIsLessThanMinLimit, x.AssetId, GetLimitMinAmount(x.DepositOptionEnum)));
            RuleFor(reg => reg.Amount).Must(IsMaxAmount).
                WithMessage(x => string.Format(Phrases.PaymentIsMoreThanMaxLimit, x.AssetId, GetLimitMaxAmount(x.DepositOptionEnum)));
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

            RuleFor(reg => reg.WalletId).MustAsync(IsBackupNotRequired)
                .WithMessage(x => Phrases.BackupErrorMsg);

            RuleFor(reg => reg.WalletId).MustAsync(IsAllowedToCashInViaBankCardAsync)
                .WithMessage(x => Phrases.OperationProhibited);
        }

        private async Task<bool> IsApprovedDepositDisclaimers(string value, CancellationToken cancellationToken)
        {
            var depositAsset = await _cachedAssetsDictionary.GetItemAsync(value);

            if (!string.IsNullOrEmpty(depositAsset?.LykkeEntityId))
            {
                var checkDisclaimerResult =
                    await _assetDisclaimersClient.CheckDepositClientDisclaimerAsync(_clientId, depositAsset.LykkeEntityId);

                return !checkDisclaimerResult.RequiresApproval;
            }
            return true;
        }

        private bool IsMinAmount(BankCardPaymentUrlRequestModel model, double value)
        {
            return value >= GetLimitMinAmount(model.DepositOptionEnum);
        }

        private bool IsMaxAmount(BankCardPaymentUrlRequestModel model, double value)
        {
            return value <= GetLimitMaxAmount(model.DepositOptionEnum);
        }

        private double GetLimitMinAmount(DepositOption depositOption)
        {
            switch (depositOption)
            {
                case DepositOption.Other: return _paymentLimitsResponse.CreditVouchersMinValue;
                case DepositOption.BankCard: return _paymentLimitsResponse.FxpaygateMinValue;
            }
            return 0;
        }

        private double GetLimitMaxAmount(DepositOption depositOption)
        {
            switch (depositOption)
            {
                case DepositOption.Other: return _paymentLimitsResponse.CreditVouchersMaxValue;
                case DepositOption.BankCard: return _paymentLimitsResponse.FxpaygateMaxValue;
            }
            return double.MaxValue;
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
            return (await _assetsService.ClientIsAllowedToCashInViaBankCardAsync(_clientId, _isIosDevice, cancellationToken)).Value;
        }

        private async Task<bool> IsKycNotNeeded(string value, CancellationToken cancellationToken)
        {
            var asset = await _tradableAssetsDictionary.GetItemAsync(value);

            var userKycStatus = await _kycStatusService.GetKycStatusAsync(_clientId);

            return asset?.KycNeeded != true || userKycStatus.IsKycOkOrReviewDone();
        }

        private bool IsOtherDepositOptionsEnabled(BankCardPaymentUrlRequestModel model, string value)
        {
            var paymentMethod = _paymentMethods.PaymentMethods.FirstOrDefault(x => x.Name.Equals(CashInPaymentSystem.CreditVoucher.ToString(), StringComparison.InvariantCultureIgnoreCase));
            return model.DepositOptionEnum != DepositOption.Other ||
                   paymentMethod != null && paymentMethod.Available && paymentMethod.Assets.Contains(value);
        }

        private bool IsBankCardDepositOptionsEnabled(BankCardPaymentUrlRequestModel model, string value)
        {
            var paymentMethod = _paymentMethods.PaymentMethods.FirstOrDefault(x => x.Name.Equals(CashInPaymentSystem.Fxpaygate.ToString(), StringComparison.InvariantCultureIgnoreCase));
            return model.DepositOptionEnum != DepositOption.BankCard ||
                   paymentMethod != null && paymentMethod.Available && paymentMethod.Assets.Contains(value);
        }

        private async Task<bool> IsValidLimitation(BankCardPaymentUrlRequestModel model, double value,
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
