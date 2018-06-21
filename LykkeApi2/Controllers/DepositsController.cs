using System;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.PaymentSystem.Client;
using Lykke.Service.PaymentSystem.Client.AutorestClient.Models;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models;
using LykkeApi2.Models.Fees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DepositsController : Controller
    {
        private readonly IPaymentSystemClient _paymentSystemService;
        private readonly IFeeCalculatorClient _feeCalculatorClient;
        private readonly IRequestContext _requestContext;

        public DepositsController(
            IPaymentSystemClient paymentSystemService,
            IFeeCalculatorClient feeCalculatorClient,
            IRequestContext requestContext)
        {
            _paymentSystemService = paymentSystemService;
            _feeCalculatorClient = feeCalculatorClient;
            _requestContext = requestContext;
        }

        /// <summary>
        /// Get last PaymentTransaction
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("fxpaygate/last")]
        [ProducesResponseType(typeof(PaymentTransactionResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLastFxPaygate()
        {
            var result = await _paymentSystemService.GetLastByDateAsync(_requestContext.ClientId);
            return Ok(result);
        }

        /// <summary>
        /// Get fee amount
        /// </summary>
        /// <returns>Fee amount</returns>
        [HttpGet]
        [Route("fxpaygate/fee")]
        [ProducesResponseType(typeof(FxPaygateFeeModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetFxPaygateFee()
        {
            return Ok(
                new FxPaygateFeeModel
                {
                    Amount = (await _feeCalculatorClient.GetBankCardFees()).Percentage
                });
        }

        /// <summary>
        /// Get Url for PaymentSystem
        /// </summary>
        /// <param name="input"></param>
        /// <returns>Returns with Url for PaymentSystem</returns>
        [HttpPost]
        [Route("fxpaygate")]
        [SwaggerOperation("Post")]
        [ProducesResponseType(typeof(FxPaygatePaymentUrlResponseModel), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> PostFxPaygate([FromBody] FxPaygatePaymentUrlRequestModel input)
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
                DepositOption.BankCard,
                input.OkUrl,
                input.FailUrl);

            var resp = new FxPaygatePaymentUrlResponseModel
            {
                Url = result.Url,
                CancelUrl = input.CancelUrl,
                FailUrl = result.FailUrl,
                OkUrl = result.OkUrl
            };

            return Ok(resp);
        }
    }
}