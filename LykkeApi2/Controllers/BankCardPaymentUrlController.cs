using System;
using System.Net;
using System.Threading.Tasks;
using AzureRepositories.PaymentSystem;
using Common;
using Common.Log;
using Core.Constants;
using Core.Identity;
using Core.PaymentSystem;
using Lykke.Service.Assets.Client;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.Limitations.Client;
using Lykke.Service.PersonalData.Contract;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models;
using LykkeApi2.Models.IsAlive;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

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

        [HttpGet]
        [SwaggerOperation("Get")]
        public async Task<IActionResult> Get()
        {
            var clientId = _requestContext.ClientId;

            try
            {
                var lastOrder = await _paymentTransactionsRepository.GetLastByDate(clientId);
                var personalData = await _personalDataService.GetAsync(clientId);

                BankCardPaymentUrlRequestModel result;
                if (lastOrder != null
                    && (lastOrder.PaymentSystem == CashInPaymentSystem.CreditVoucher
                        || lastOrder.PaymentSystem == CashInPaymentSystem.Fxpaygate))
                {
                    result = BankCardPaymentUrlRequestModel.Create(lastOrder, personalData);
                }
                else
                {
                    result = BankCardPaymentUrlRequestModel.Create(personalData);
                }

                return Ok(result);
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync("BankCardPaymentUrlFormValuesController", "Get", string.Empty, e);

                return StatusCode(
                    (int)ErrorCodeType.RuntimeProblem,
                    ErrorResponse.Create(Phrases.TechnicalProblems));
            }
        }

        [HttpPost]
        [SwaggerOperation("Post")]
        [ProducesResponseType(typeof(IsAliveResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Post([FromBody]BankCardPaymentUrlRequestModel input)
        {
            var clientId = _requestContext.ClientId;

            if (string.IsNullOrWhiteSpace(input.AssetId))
                input.AssetId = LykkeConstants.UsdAssetId;

            var phoneNumberE164 = input.Phone.PreparePhoneNum().ToE164Number();
            var pd = await _personalDataService.GetAsync(clientId);

            if (string.IsNullOrWhiteSpace(pd.PaymentSystem) || !Enum.TryParse(pd.PaymentSystem, out CashInPaymentSystem paymentSystem))
                paymentSystem = CashInPaymentSystem.Unknown;

            if (input.DepositOptionEnum == DepositOption.Other)
                paymentSystem = CashInPaymentSystem.CreditVoucher; // https://lykkex.atlassian.net/browse/LWDEV-4665

            var checkResult = await _limitationsServiceClient.CheckAsync(clientId, input.AssetId, input.Amount, CurrencyOperationType.CardCashIn);

            if (!checkResult.IsValid)
                return StatusCode( (int)ErrorCodeType.LimitationCheckFailed, checkResult.FailMessage);

            var transactionId = (await _identityGenerator.GenerateNewIdAsync()).ToString();

            const string formatOfDateOfBirth = "yyyy-MM-dd";

            var info = OtherPaymentInfo.Create(
                input.FirstName,
                input.LastName,
                input.City,
                input.Zip,
                input.Address,
                input.Country,
                input.Email,
                phoneNumberE164,
                pd.DateOfBirth?.ToString(formatOfDateOfBirth),
                input.OkUrl,
                input.FailUrl)
            .ToJson();

            var bankCardsFee = await _feeCalculatorClient.GetBankCardFees();

            try
            {
                var asset = await _assetsService.AssetGetAsync(input.AssetId);
                var feeAmount = Math.Round(input.Amount * bankCardsFee.Percentage, 15);
                var feeAmountTruncated = feeAmount.TruncateDecimalPlaces(asset.Accuracy, true);

                var paymentTransaction = PaymentTransaction.Create(
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
                    return StatusCode(
                        (int)ErrorCodeType.InconsistentData,
                        ErrorResponse.Create(Phrases.OperationProhibited));
                }

                paymentTransaction.PaymentSystem = urlData.PaymentSystem;

                await _paymentTransactionsRepository.CreateAsync(paymentTransaction);
                await _paymentTransactionEventsLog.WriteAsync(PaymentTransactionEventLog.Create(transactionId, "", "Registered", clientId));
                await _paymentTransactionEventsLog.WriteAsync(PaymentTransactionEventLog.Create(transactionId, urlData.PaymentUrl, "Payment Url has created", clientId));

                // mode=iframe is for Mobile version 
                if (!string.IsNullOrWhiteSpace(urlData.PaymentUrl))
                    urlData.PaymentUrl = urlData.PaymentUrl
                        + (urlData.PaymentUrl.Contains("?") ? "&" : "?")
                        + "mode=iframe";

                return Ok(new BankCardPaymentUrlResponseModel
                {
                    Url = urlData.PaymentUrl,
                    OkUrl = urlData.OkUrl,
                    FailUrl = urlData.FailUrl
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