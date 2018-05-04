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
using Lykke.Service.PaymentSystem.Client.AutorestClient.Models;
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
        [ProducesResponseType(typeof(PaymentTransactionResponse), (int)HttpStatusCode.OK)]

        public async Task<IActionResult> Get()
        {
            var result = await _paymentSystemService.GetLastByDateAsync(_requestContext.ClientId);
            return Ok(result);
        }
        
        [HttpPost]
        [SwaggerOperation("Post")]
        [ProducesResponseType(typeof(PaymentUrlDataResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Post([FromBody]BankCardPaymentUrlRequestModel input)
        {
            var result = await _paymentSystemService.GetUrlDataAsync(
                _requestContext.ClientId,
                input.Amount,
                input.AssetId,
                input.WalletId,
                input.FirstName,
                input.LastName,
                input.City,
                input.Zip,
                input.Address,
                input.Country,
                input.Email,
                input.Phone,
                input.DepositOptionEnum,
                input.OkUrl,
                input.FailUrl);

            return Ok(result);
        }
    }
}