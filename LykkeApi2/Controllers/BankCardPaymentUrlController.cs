using System;
using System.Net;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.Constants;
using Core.Identity;
using Core.PaymentSystem;
using Lykke.Service.Assets.Client;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.PersonalData.Contract;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models;
using LykkeApi2.Models.IsAlive;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

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
        private readonly ILog _log;
        private readonly IIdentityRepository _identityGenerator;
        private readonly IPaymentTransactionEventsLogRepository _paymentTransactionEventsLog;
        private readonly ILimitationsServiceClient _limitationsServiceClient;
        private readonly IPersonalDataService _personalDataService;
        private readonly IAssetsService _assetsService;
        private readonly IFeeCalculatorClient _feeCalculatorClient;
        private readonly IRequestContext _requestContext;

        public BankCardPaymentUrlController(
            IPaymentSystemFacade paymentSystemFacade,
            IPaymentTransactionsRepository paymentTransactionsRepository,
            ILog log,
            IIdentityRepository identityGenerator,
            IPaymentTransactionEventsLogRepository paymentTransactionEventsLog,
            ILimitationsServiceClient limitationsServiceClient,
            IPersonalDataService personalDataService,
            IAssetsService assetsService,
            IFeeCalculatorClient feeCalculatorClient,
            IRequestContext requestContext)
        {
            _paymentSystemFacade = paymentSystemFacade;
            _paymentTransactionsRepository = paymentTransactionsRepository;
            _log = log;
            _identityGenerator = identityGenerator;
            _paymentTransactionEventsLog = paymentTransactionEventsLog;
            _personalDataService = personalDataService;
            _limitationsServiceClient = limitationsServiceClient;
            _assetsService = assetsService;
            _feeCalculatorClient = feeCalculatorClient;
            _requestContext = requestContext;
        }
        
        [HttpPost]
        [SwaggerOperation("Post")]
        [ProducesResponseType(typeof(IsAliveResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Post([FromBody]BankCardPaymentUrlInputModel input)
        {
            string clientId = this.GetClientId();

            if (string.IsNullOrWhiteSpace(input.AssetId))
                input.AssetId = LykkeConstants.UsdAssetId;

            var phoneNumberE164 = input.Phone.PreparePhoneNum().ToE164Number();

            var pd = await _personalDataService.GetAsync(clientId);

            if (string.IsNullOrWhiteSpace(pd.PaymentSystem) || !Enum.TryParse(pd.PaymentSystem, out CashInPaymentSystem paymentSystem))
                paymentSystem = CashInPaymentSystem.Unknown;

            if (input.DepositOptionEnum == DepositOption.Other)
                paymentSystem = CashInPaymentSystem.CreditVoucher; // https://lykkex.atlassian.net/browse/LWDEV-4665

            var checkResult = await _limitationsServiceClient.CheckAsync(
                clientId,
                input.AssetId,
                input.Amount,
                CurrencyOperationType.CardCashIn);
            if (!checkResult.IsValid)
                return StatusCode(
                    (int)ErrorCodeType.LimitationCheckFailed,
                    checkResult.FailMessage);

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
                    return StatusCode(
                        (int)ErrorCodeType.InconsistentData,
                        ErrorResponse.Create(Phrases.OperationProhibited));
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
                return Ok(new BankCardPaymentUrlResponseModel()
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
                return StatusCode(
                    (int)ErrorCodeType.RuntimeProblem,
                    ErrorResponse.Create(Phrases.TechnicalProblems));
            }
        }
    }
}