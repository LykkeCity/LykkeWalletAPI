using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.Constants;
using Core.Identity;
using Core.PaymentSystem;
using Core.Settings;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.PersonalData.Contract;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Produces("application/json")]
    [Authorize]
    //[ServiceFilter(typeof(DisableOnMaintenanceFilter))]
    [Route("api/[controller]")]
    public class BankCardPaymentUrlController : Controller
    {
        private readonly IPaymentSystemFacade _paymentSystemFacade;
        private readonly IPaymentTransactionsRepository _paymentTransactionsRepository;
        private readonly IClientAccountClient _clientAccountService;
        private readonly ILog _log;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly IPaymentTransactionEventsLog _paymentTransactionEventsLog;
        private readonly ILimitationsServiceClient _limitationsServiceClient;
        private readonly CreditVouchersSettings _creditVouchersSettings;
        private readonly ISrvBackup _srvBackup;
        private readonly ISrvKycForAsset _srvKycForAsset;
        private readonly IPersonalDataService _personalDataService;
        private readonly SrvDisabledOperations _srvDisabledOperations;
        private readonly IAssetsService _assetsService;
        private readonly IWalletCredentialsRepository _walletCredentialsRepository;
        private readonly IFeeCalculatorClient _feeCalculatorClient;
        private readonly IAssetDisclaimersClient _assetDisclaimersClient;
        private readonly CachedAssetsDictionary _cachedAssetsDictionary;

        public BankCardPaymentUrlController(
            IPaymentSystemFacade paymentSystemFacade,
            IPaymentTransactionsRepository paymentTransactionsRepository,
            IClientAccountClient clientAccountService,
            ILog log,
            IIdentityGenerator identityGenerator,
            IPaymentTransactionEventsLog paymentTransactionEventsLog,
            ILimitationsServiceClient limitationsServiceClient,
            BaseSettings baseSettings,
            ISrvBackup srvBackup,
            ISrvKycForAsset srvKycForAsset,
            IPersonalDataService personalDataService,
            SrvDisabledOperations srvDisabledOperations,
            IAssetsService assetsService,
            IWalletCredentialsRepository walletCredentialsRepository,
            IFeeCalculatorClient feeCalculatorClient,
            IAssetDisclaimersClient assetDisclaimersClient,
            CachedAssetsDictionary cachedAssetsDictionary)
        {
            _paymentSystemFacade = paymentSystemFacade;
            _paymentTransactionsRepository = paymentTransactionsRepository;
            _clientAccountService = clientAccountService;
            _log = log;
            _identityGenerator = identityGenerator;
            _paymentTransactionEventsLog = paymentTransactionEventsLog;
            _srvBackup = srvBackup;
            _srvKycForAsset = srvKycForAsset;
            _personalDataService = personalDataService;
            _limitationsServiceClient = limitationsServiceClient;
            _creditVouchersSettings = baseSettings.PaymentSystems.CreditVouchers;
            _srvDisabledOperations = srvDisabledOperations;
            _assetsService = assetsService;
            _walletCredentialsRepository = walletCredentialsRepository;
            _feeCalculatorClient = feeCalculatorClient;
            _assetDisclaimersClient = assetDisclaimersClient;
            _cachedAssetsDictionary = cachedAssetsDictionary;
        }

        [HttpPost]
        public async Task<ResponseModel<BankCardPaymentUrlResponceModel>> Post([FromBody]BankCardPaymentUrlInputModel input)
        {
            string clientId = this.GetClientId();

            if (string.IsNullOrWhiteSpace(input.AssetId))
                input.AssetId = LykkeConstants.UsdAssetId;

            // TODO: Replace with declarative validation
            #region Validation

            Asset depositAsset = await _cachedAssetsDictionary.GetItemAsync(input.AssetId);

            // Find not approved deposit disclaimers
            if (!string.IsNullOrEmpty(depositAsset.LykkeEntityId))
            {
                CheckResultModel checkDisclaimerResult =
                    await _assetDisclaimersClient.CheckDepositClientDisclaimerAsync(clientId, depositAsset.LykkeEntityId);

                if (checkDisclaimerResult.RequiresApproval)
                {
                    return ResponseModel<BankCardPaymentUrlResponceModel>
                        .CreateFail(ResponseModel.ErrorCodeType.PendingDisclaimer, Phrases.PendingDisclaimer);
                }
            }

            if (await _srvDisabledOperations.IsOperationForAssetDisabled(input.AssetId))
                return ResponseModel<BankCardPaymentUrlResponceModel>.CreateFail(ResponseModel.ErrorCodeType.MaintananceMode, Phrases.BtcDisabledMsg);

            if (input.Amount <= 0)
                return ResponseModel<BankCardPaymentUrlResponceModel>
                    .CreateInvalidFieldError(nameof(input.Amount), string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(input.Amount)));

            if (input.Amount < _creditVouchersSettings.MinAmount)
                return ResponseModel<BankCardPaymentUrlResponceModel>
                    .CreateInvalidFieldError(nameof(input.Amount), string.Format(Phrases.PaymentIsLessThanMinLimit, input.AssetId, _creditVouchersSettings.MinAmount));

            if (input.Amount > _creditVouchersSettings.MaxAmount)
                return ResponseModel<BankCardPaymentUrlResponceModel>
                    .CreateInvalidFieldError(nameof(input.Amount), string.Format(Phrases.MaxPaymentLimitExceeded, input.AssetId, _creditVouchersSettings.MaxAmount));

            if (string.IsNullOrEmpty(input.FirstName))
                return ResponseModel<BankCardPaymentUrlResponceModel>
                    .CreateInvalidFieldError(nameof(input.FirstName), string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(input.FirstName)));

            if (string.IsNullOrEmpty(input.LastName))
                return ResponseModel<BankCardPaymentUrlResponceModel>
                    .CreateInvalidFieldError(nameof(input.LastName), string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(input.LastName)));

            if (string.IsNullOrEmpty(input.City))
                return ResponseModel<BankCardPaymentUrlResponceModel>
                    .CreateInvalidFieldError(nameof(input.City), string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(input.City)));

            if (string.IsNullOrEmpty(input.Zip))
                return ResponseModel<BankCardPaymentUrlResponceModel>
                    .CreateInvalidFieldError(nameof(input.Zip), string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(input.Zip)));

            if (string.IsNullOrEmpty(input.Address))
                return ResponseModel<BankCardPaymentUrlResponceModel>
                    .CreateInvalidFieldError(nameof(input.Address), string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(input.Address)));

            if (string.IsNullOrEmpty(input.Country))
                return ResponseModel<BankCardPaymentUrlResponceModel>
                    .CreateInvalidFieldError(nameof(input.Country), string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(input.Country)));

            if (string.IsNullOrEmpty(input.Email))
                return ResponseModel<BankCardPaymentUrlResponceModel>
                    .CreateInvalidFieldError(nameof(input.Email), string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(input.Email)));

            if (!input.Email.IsValidEmailAndRowKey())
                return ResponseModel<BankCardPaymentUrlResponceModel>
                 .CreateInvalidFieldError(nameof(input.Email), Phrases.InvalidEmailFormat);

            if (string.IsNullOrEmpty(input.Phone))
                return ResponseModel<BankCardPaymentUrlResponceModel>
                    .CreateInvalidFieldError(nameof(input.Phone), string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(input.Phone)));

            var phoneNumberE164 = input.Phone.PreparePhoneNum().ToE164Number();

            if (phoneNumberE164 == null)
                return ResponseModel<BankCardPaymentUrlResponceModel>
                 .CreateInvalidFieldError(nameof(input.Phone), Phrases.InvalidNumberFormat);

            var pd = await _personalDataService.GetAsync(clientId);
            if (pd.Email != input.Email || pd.ContactPhone != input.Phone)
                return ResponseModel<BankCardPaymentUrlResponceModel>
                 .CreateFail(ResponseModel.ErrorCodeType.InconsistentData, Phrases.OperationProhibited);

            var paymentSystem = CashInPaymentSystem.Unknown;
            if (string.IsNullOrWhiteSpace(pd.PaymentSystem) || !Enum.TryParse(pd.PaymentSystem, out paymentSystem))
                paymentSystem = CashInPaymentSystem.Unknown;

            if (input.DepositOptionEnum == DepositOption.Other)
                paymentSystem = CashInPaymentSystem.CreditVoucher; // https://lykkex.atlassian.net/browse/LWDEV-4665

            if ((await _clientAccountService.GetDepositBlockAsync(clientId)).DepositViaCreditCardBlocked)
            {
                return ResponseModel<BankCardPaymentUrlResponceModel>
                    .CreateFail(ResponseModel.ErrorCodeType.InconsistentData, Phrases.OperationProhibited);
            }

            if (await _srvBackup.IsBackupRequired(clientId))
                return ResponseModel<BankCardPaymentUrlResponceModel>
                    .CreateFail(ResponseModel.ErrorCodeType.BackupRequired, Phrases.BackupErrorMsg);

            if (!(await _assetsService.ClientIsAllowedToCashInViaBankCardAsync(clientId, this.IsIosDevice())).Value)
            {
                return ResponseModel<BankCardPaymentUrlResponceModel>
                    .CreateFail(ResponseModel.ErrorCodeType.InconsistentData, Phrases.OperationProhibited);
            }

            if (await _srvKycForAsset.IsKycNeeded(clientId, input.AssetId))
                return ResponseModel<BankCardPaymentUrlResponceModel>
                    .CreateFail(ResponseModel.ErrorCodeType.InconsistentData, Phrases.KycNeeded);

            if (input.DepositOptionEnum == DepositOption.Other)
            {
                var asset = await _assetsService.AssetGetAsync(input.AssetId);
                if (!asset.OtherDepositOptionsEnabled)
                    return ResponseModel<BankCardPaymentUrlResponceModel>
                        .CreateFail(ResponseModel.ErrorCodeType.InconsistentData, $"Asset '{input.AssetId}' does not allow use DepositOption.Other");
            }

            var checkResult = await _limitationsServiceClient.CheckAsync(
                clientId,
                input.AssetId,
                input.Amount,
                CurrencyOperationType.CardCashIn);
            if (!checkResult.IsValid)
                return ResponseModel<BankCardPaymentUrlResponceModel>.CreateFail(
                ResponseModel.ErrorCodeType.LimitationCheckFailed, checkResult.FailMessage);

            var credentials = await _walletCredentialsRepository.GetAsync(clientId);
            if (string.IsNullOrWhiteSpace(credentials?.MultiSig))
                return ResponseModel<BankCardPaymentUrlResponceModel>.CreateFail(ResponseModel.ErrorCodeType.InconsistentData, Phrases.TradingWalletDoesNotExist);

            #endregion

            var transactionId = (await _identityGenerator.GenerateNewIdAsync()).ToString();

            var info = OtherPaymentInfo.Create(
                firstName: input.FirstName,
                lastName: input.LastName,
                city: input.City,
                zip: input.Zip,
                address: input.Address,
                country: input.Country,
                email: input.Email,
                contactPhone: phoneNumberE164,
                dateOfBirth: pd.DateOfBirth.HasValue ? pd.DateOfBirth.Value.ToString("yyyy-MM-dd") : null)
            .ToJson();

            var bankCardsFee = await _feeCalculatorClient.GetBankCardFees();

            try
            {
                var asset = await _assetsService.AssetGetAsync(input.AssetId);
                var feeAmount = Math.Round(input.Amount * bankCardsFee.Percentage, 15);
                var feeAmountTruncated = feeAmount.TruncateDecimalPlaces(asset.Accuracy, true);

                var pt = PaymentTransaction.Create(
                    transactionId,
                    paymentSystem,
                    clientId,
                    input.Amount,
                    feeAmountTruncated,
                    input.AssetId,
                    input.WalletId,
                    assetToDeposit: input.AssetId,
                    info: info);

                var urlData = await _paymentSystemFacade.GetUrlDataAsync(
                    paymentSystem.ToString(),
                    transactionId,
                    clientId,
                    input.Amount + feeAmountTruncated,
                    input.AssetId,
                    input.WalletId,
                    input.GetCountryIso3Code(),
                    info);

                if (!string.IsNullOrEmpty(urlData.ErrorMessage))
                {
                    await _log.WriteWarningAsync(
                        nameof(BankCardPaymentUrlController), nameof(Post), input.ToJson(), urlData.ErrorMessage, DateTime.UtcNow);
                    return ResponseModel<BankCardPaymentUrlResponceModel>.CreateFail(
                        ResponseModel.ErrorCodeType.InconsistentData, Phrases.OperationProhibited);
                }

                pt.PaymentSystem = urlData.PaymentSystem;

                await _paymentTransactionsRepository.CreateAsync(pt);
                await _paymentTransactionEventsLog.WriteAsync(
                    PaymentTransactionEventLog.Create(transactionId, "", "Registered", clientId));
                await _paymentTransactionEventsLog.WriteAsync(
                    PaymentTransactionEventLog.Create(transactionId, urlData.PaymentUrl, "Payment Url has created", clientId));

                // mode=iframe is for Mobile version 
                if (!string.IsNullOrWhiteSpace(urlData.PaymentUrl))
                    urlData.PaymentUrl = urlData.PaymentUrl
                        + (urlData.PaymentUrl.Contains("?") ? "&" : "?")
                        + "mode=iframe";

                var neverMatchUrlRegex = "^$";
                return ResponseModel<BankCardPaymentUrlResponceModel>.CreateOk(
                    new BankCardPaymentUrlResponceModel
                    {
                        Url = urlData.PaymentUrl,
                        OkUrl = urlData.OkUrl,
                        FailUrl = urlData.FailUrl,
                        ReloadRegex = neverMatchUrlRegex,
                        UrlsToFormatRegex = neverMatchUrlRegex,
                    });
            }
            catch (Exception e)
            {

                await _paymentTransactionEventsLog.WriteAsync(
                    PaymentTransactionEventLog.Create(transactionId, e.Message, "Payment Url creation fail", clientId));

                await _log.WriteErrorAsync("BankCardPaymentUrlController", "Post", input.ToJson(), e);

                return ResponseModel<BankCardPaymentUrlResponceModel>.CreateFail(
                    ResponseModel.ErrorCodeType.RuntimeProblem, Phrases.TechnicalProblems);
            }
        }
    }
}