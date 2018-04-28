using System;
using System.Net;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.Constants;
using Core.Identity;
using Lykke.Contracts.Payments;
using Lykke.Service.Assets.Client;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.Limitations.Client;
using Lykke.Service.PaymentSystem.Client;
using Lykke.Service.PersonalData.Contract;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using CashInPaymentSystem = Lykke.Service.PaymentSystem.Client.AutorestClient.Models.CashInPaymentSystem;
using ErrorResponse = LykkeApi2.Models.ErrorResponse;
using IsAliveResponse = LykkeApi2.Models.IsAlive.IsAliveResponse;
using PaymentStatus = Lykke.Service.PaymentSystem.Client.AutorestClient.Models.PaymentStatus;

namespace LykkeApi2.Controllers
{
    [Produces("application/json")]
    [Authorize]
    [ServiceFilter(typeof(DisableOnMaintenanceFilter))]
    [Route("api/[controller]")]
    public class BankCardPaymentUrlController : Controller
    {
        private readonly IPaymentSystemClient _paymentSystemService;
        private readonly ILog _log;
        private readonly IIdentityRepository _identityGenerator;
        private readonly ILimitationsServiceClient _limitationsServiceClient;
        private readonly IPersonalDataService _personalDataService;
        private readonly IAssetsService _assetsService;
        private readonly IFeeCalculatorClient _feeCalculatorClient;
        private readonly IRequestContext _requestContext;

        public BankCardPaymentUrlController(
            IPaymentSystemClient paymentSystemService,
            ILog log,
            IIdentityRepository identityGenerator,
            ILimitationsServiceClient limitationsServiceClient,
            IPersonalDataService personalDataService,
            IAssetsService assetsService,
            IFeeCalculatorClient feeCalculatorClient,
            IRequestContext requestContext)
        {
            _paymentSystemService = paymentSystemService;
            _log = log;
            _identityGenerator = identityGenerator;
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
                var lastOrder = await _paymentSystemService.GetLastByDateAsync(clientId);
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

            CashInPaymentSystem paymentSystem;

            switch (input.DepositOptionEnum)
            {
                case DepositOption.BankCard:
                    paymentSystem = CashInPaymentSystem.Fxpaygate;
                    break;
                case DepositOption.Other:
                    paymentSystem = CashInPaymentSystem.CreditVoucher;
                    break;
                default:
                    paymentSystem = CashInPaymentSystem.Unknown;
                    break;
            }

            var checkResult = await _limitationsServiceClient.CheckAsync(clientId, input.AssetId, input.Amount, CurrencyOperationType.CardCashIn);

            if (!checkResult.IsValid)
                return StatusCode((int)ErrorCodeType.LimitationCheckFailed, checkResult.FailMessage);

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

                var urlData = await _paymentSystemService.GetUrlDataAsync(
                    paymentSystem.ToString(),
                    transactionId,
                    clientId,
                    input.Amount + feeAmountTruncated,
                    input.AssetId,
                    input.WalletId,
                    input.GetCountryIso3Code(),
                    info);

                await _paymentSystemService.InsertPaymentTransactionEventLogAsync(
                    transactionId,
                    urlData.PaymentUrl,
                    "Payment Url has created",
                    clientId);

                if (!string.IsNullOrEmpty(urlData.ErrorMessage))
                {
                    await _log.WriteWarningAsync(
                        nameof(BankCardPaymentUrlController), nameof(Post), input.ToJson(), urlData.ErrorMessage, DateTime.UtcNow);
                    return StatusCode(
                        (int)ErrorCodeType.InconsistentData,
                        ErrorResponse.Create(Phrases.OperationProhibited));
                }

                await _paymentSystemService.InsertPaymentTransactionAsync(
                    input.Amount,
                    PaymentStatus.Created,
                    paymentSystem,
                    feeAmountTruncated,
                    transactionId,
                    clientId,
                    input.AssetId,
                    input.AssetId,
                    input.WalletId,
                    info
                );

                await _paymentSystemService.InsertPaymentTransactionEventLogAsync(
                    transactionId,
                    string.Empty,
                    "Registered",
                    clientId);

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
                await _paymentSystemService.InsertPaymentTransactionEventLogAsync(
                    transactionId,
                    e.Message,
                    "Payment Url creation fail",
                    clientId);

                await _log.WriteErrorAsync("BankCardPaymentUrlController", "Post", input.ToJson(), e);
                return StatusCode(
                    (int)ErrorCodeType.RuntimeProblem,
                    ErrorResponse.Create(Phrases.TechnicalProblems));
            }
        }
    }
}