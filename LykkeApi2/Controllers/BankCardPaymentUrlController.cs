using System;
using System.Threading.Tasks;
using AzureRepositories.PaymentSystem;
using Common;
using Common.Log;
using Core;
using Core.Bitcoin;
using Core.Clients;
using Core.Constants;
using Core.Identity;
using Core.Kyc;
using Core.PaymentSystem;
using Core.Settings;
using LkeServices.Operations;
using Lykke.Service.AssetDisclaimers.Client;
using Lykke.Service.Assets.Client;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.Limitations.Client;
using Lykke.Service.PersonalData.Contract;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Produces("application/json")]
    [Authorize]
    [ServiceFilter(typeof(DisableOnMaintenanceFilter))]
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
        private readonly IRequestContext _requestContext;


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
            CachedAssetsDictionary cachedAssetsDictionary, 
            IRequestContext requestContext)
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
            _requestContext = requestContext;
        }

        [HttpPost]
        public async Task<ResponseModel<BankCardPaymentUrlResponseModel>> Post([FromBody]BankCardPaymentUrlInputModel input)
        {
            var clientId = _requestContext.ClientId;

            if (string.IsNullOrWhiteSpace(input.AssetId))
                input.AssetId = LykkeConstants.UsdAssetId;

            // TODO: Replace with declarative validation
            #region Validation

            var depositAsset = await _cachedAssetsDictionary.GetItemAsync(input.AssetId);

            // Find not approved deposit disclaimers
            if (!string.IsNullOrEmpty(depositAsset.LykkeEntityId))
            {
                var checkDisclaimerResult =
                    await _assetDisclaimersClient.CheckDepositClientDisclaimerAsync(clientId, depositAsset.LykkeEntityId);

                if (checkDisclaimerResult.RequiresApproval)
                {
                    return ResponseModel<BankCardPaymentUrlResponseModel>
                        .CreateFail(ErrorCodeType.PendingDisclaimer, Phrases.PendingDisclaimer);
                }
            }

            if (await _srvDisabledOperations.IsOperationForAssetDisabled(input.AssetId))
                return ResponseModel<BankCardPaymentUrlResponseModel>.CreateFail(ErrorCodeType.MaintananceMode, Phrases.BtcDisabledMsg);

            if (input.Amount <= 0)
                return ResponseModel<BankCardPaymentUrlResponseModel>
                    .CreateInvalidFieldError(nameof(input.Amount), string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(input.Amount)));

            if (input.Amount < _creditVouchersSettings.MinAmount)
                return ResponseModel<BankCardPaymentUrlResponseModel>
                    .CreateInvalidFieldError(nameof(input.Amount), string.Format(Phrases.PaymentIsLessThanMinLimit, input.AssetId, _creditVouchersSettings.MinAmount));

            if (input.Amount > _creditVouchersSettings.MaxAmount)
                return ResponseModel<BankCardPaymentUrlResponseModel>
                    .CreateInvalidFieldError(nameof(input.Amount), string.Format(Phrases.MaxPaymentLimitExceeded, input.AssetId, _creditVouchersSettings.MaxAmount));

            if (string.IsNullOrEmpty(input.FirstName))
                return ResponseModel<BankCardPaymentUrlResponseModel>
                    .CreateInvalidFieldError(nameof(input.FirstName), string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(input.FirstName)));

            if (string.IsNullOrEmpty(input.LastName))
                return ResponseModel<BankCardPaymentUrlResponseModel>
                    .CreateInvalidFieldError(nameof(input.LastName), string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(input.LastName)));

            if (string.IsNullOrEmpty(input.City))
                return ResponseModel<BankCardPaymentUrlResponseModel>
                    .CreateInvalidFieldError(nameof(input.City), string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(input.City)));

            if (string.IsNullOrEmpty(input.Zip))
                return ResponseModel<BankCardPaymentUrlResponseModel>
                    .CreateInvalidFieldError(nameof(input.Zip), string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(input.Zip)));

            if (string.IsNullOrEmpty(input.Address))
                return ResponseModel<BankCardPaymentUrlResponseModel>
                    .CreateInvalidFieldError(nameof(input.Address), string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(input.Address)));

            if (string.IsNullOrEmpty(input.Country))
                return ResponseModel<BankCardPaymentUrlResponseModel>
                    .CreateInvalidFieldError(nameof(input.Country), string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(input.Country)));

            if (string.IsNullOrEmpty(input.Email))
                return ResponseModel<BankCardPaymentUrlResponseModel>
                    .CreateInvalidFieldError(nameof(input.Email), string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(input.Email)));

            if (!input.Email.IsValidEmailAndRowKey())
                return ResponseModel<BankCardPaymentUrlResponseModel>
                 .CreateInvalidFieldError(nameof(input.Email), Phrases.InvalidEmailFormat);

            if (string.IsNullOrEmpty(input.Phone))
                return ResponseModel<BankCardPaymentUrlResponseModel>
                    .CreateInvalidFieldError(nameof(input.Phone), string.Format(Phrases.FieldShouldNotBeEmptyFormat, nameof(input.Phone)));

            var phoneNumberE164 = input.Phone.PreparePhoneNum().ToE164Number();

            if (phoneNumberE164 == null)
                return ResponseModel<BankCardPaymentUrlResponseModel>
                 .CreateInvalidFieldError(nameof(input.Phone), Phrases.InvalidNumberFormat);

            var pd = await _personalDataService.GetAsync(clientId);
            if (pd.Email != input.Email || pd.ContactPhone != input.Phone)
                return ResponseModel<BankCardPaymentUrlResponseModel>
                 .CreateFail(ErrorCodeType.InconsistentData, Phrases.OperationProhibited);

            var paymentSystem = CashInPaymentSystem.Unknown;
            if (string.IsNullOrWhiteSpace(pd.PaymentSystem) || !Enum.TryParse(pd.PaymentSystem, out paymentSystem))
                paymentSystem = CashInPaymentSystem.Unknown;

            if (input.DepositOptionEnum == DepositOption.Other)
                paymentSystem = CashInPaymentSystem.CreditVoucher; // https://lykkex.atlassian.net/browse/LWDEV-4665

            if ((await _clientAccountService.GetDepositBlockAsync(clientId)).DepositViaCreditCardBlocked)
            {
                return ResponseModel<BankCardPaymentUrlResponseModel>
                    .CreateFail(ErrorCodeType.InconsistentData, Phrases.OperationProhibited);
            }

            if (await _srvBackup.IsBackupRequired(clientId))
                return ResponseModel<BankCardPaymentUrlResponseModel>
                    .CreateFail(ErrorCodeType.BackupRequired, Phrases.BackupErrorMsg);

            if (!(await _assetsService.ClientIsAllowedToCashInViaBankCardAsync(clientId, _requestContext.IsIosDevice)).Value)
            {
                return ResponseModel<BankCardPaymentUrlResponseModel>
                    .CreateFail(ErrorCodeType.InconsistentData, Phrases.OperationProhibited);
            }

            if (await _srvKycForAsset.IsKycNeeded(clientId, input.AssetId))
                return ResponseModel<BankCardPaymentUrlResponseModel>
                    .CreateFail(ErrorCodeType.InconsistentData, Phrases.KycNeeded);

            if (input.DepositOptionEnum == DepositOption.Other)
            {
                var asset = await _assetsService.AssetGetAsync(input.AssetId);
                if (!asset.OtherDepositOptionsEnabled)
                    return ResponseModel<BankCardPaymentUrlResponseModel>
                        .CreateFail(ErrorCodeType.InconsistentData, $"Asset '{input.AssetId}' does not allow use DepositOption.Other");
            }

            var checkResult = await _limitationsServiceClient.CheckAsync(
                clientId,
                input.AssetId,
                input.Amount,
                CurrencyOperationType.CardCashIn);
            if (!checkResult.IsValid)
                return ResponseModel<BankCardPaymentUrlResponseModel>.CreateFail(
                ErrorCodeType.LimitationCheckFailed, checkResult.FailMessage);

            var credentials = await _walletCredentialsRepository.GetAsync(clientId);
            if (string.IsNullOrWhiteSpace(credentials?.MultiSig))
                return ResponseModel<BankCardPaymentUrlResponseModel>.CreateFail(ErrorCodeType.InconsistentData, Phrases.TradingWalletDoesNotExist);

            #endregion

            var transactionId = (await _identityGenerator.GenerateNewIdAsync()).ToString();

            var info = OtherPaymentInfo.Create(
                input.FirstName,
                input.LastName,
                input.City,
                input.Zip,
                input.Address,
                input.Country,
                input.Email,
                phoneNumberE164,
                pd.DateOfBirth?.ToString("yyyy-MM-dd"))
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
                    input.AssetId,
                    info);

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
                    return ResponseModel<BankCardPaymentUrlResponseModel>.CreateFail(
                        ErrorCodeType.InconsistentData, Phrases.OperationProhibited);
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

                const string neverMatchUrlRegex = "^$";
                return ResponseModel<BankCardPaymentUrlResponseModel>.CreateOk(
                    new BankCardPaymentUrlResponseModel
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

                return ResponseModel<BankCardPaymentUrlResponseModel>.CreateFail(
                    ErrorCodeType.RuntimeProblem, Phrases.TechnicalProblems);
            }
        }
    }
}