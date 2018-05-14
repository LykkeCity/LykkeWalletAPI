using System.Net;
using System.Threading.Tasks;
using Lykke.Service.PaymentSystem.Client;
using Lykke.Service.PaymentSystem.Client.AutorestClient.Models;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LykkeApi2.Controllers
{
    [Produces("application/json")]
    [Authorize]
    [Route("api/[controller]")]
    public class BankCardPaymentUrlController : Controller
    {
        private readonly IPaymentSystemClient _paymentSystemService;
        private readonly IRequestContext _requestContext;

        public BankCardPaymentUrlController(
            IPaymentSystemClient paymentSystemService,
            IRequestContext requestContext)
        {
            _paymentSystemService = paymentSystemService;
            _requestContext = requestContext;
        }

        /// <summary>
        /// Get last PaymentTransaction
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [SwaggerOperation("Get")]
        [ProducesResponseType(typeof(PaymentTransactionResponse), (int)HttpStatusCode.OK)]

        public async Task<IActionResult> Get()
        {
            var result = await _paymentSystemService.GetLastByDateAsync(_requestContext.ClientId);
            return Ok(result);
        }

        /// <summary>
        /// Get Url for PaymentSystem
        /// </summary>
        /// <param name="input"></param>
        /// <returns>Returns with Url for PaymentSystem</returns>
        [HttpPost]
        [SwaggerOperation("Post")]
        [ProducesResponseType(typeof(PaymentUrlDataResponse), (int)HttpStatusCode.OK)]
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