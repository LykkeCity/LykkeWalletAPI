using System.Net;
using System.Threading.Tasks;
using Lykke.Service.PaymentSystem.Client;
using Lykke.Service.PaymentSystem.Client.AutorestClient.Models;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.ValidationModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LykkeApi2.Controllers
{
    [Route("api/[controller]")]
    [ValidateModel]
    [Authorize]
    public class PaymentMethodsController : Controller
    {
        private readonly IPaymentSystemClient _paymentSystemClient;
        private readonly IRequestContext _requestContext;

        public PaymentMethodsController(IPaymentSystemClient paymentSystemClient, IRequestContext requestContext)
        {
            _paymentSystemClient = paymentSystemClient;
            _requestContext = requestContext;
        }

        /// <summary>
        /// Get PaymentMethods
        /// </summary>
        /// <returns>Available PaymentMethods</returns>
        [HttpGet]
        [SwaggerOperation("GetPaymentMethods")]
        [ProducesResponseType(typeof(PaymentMethodsResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            var clientId = _requestContext.ClientId;
            var result = await _paymentSystemClient.GetPaymentMethodsAsync(clientId);
            return Ok(result);
        }
    }
}