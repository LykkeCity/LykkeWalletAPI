using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.Services;
using Lykke.Service.PaymentSystem.Client;
using Lykke.Service.PaymentSystem.Client.AutorestClient.Models;
using LykkeApi2.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentMethodsController : Controller
    {
        private readonly IPaymentSystemClient _paymentSystemClient;
        private readonly IRequestContext _requestContext;
        private readonly IAssetsHelper _assetsHelper;

        public PaymentMethodsController(
            IPaymentSystemClient paymentSystemClient,
            IRequestContext requestContext,
            IAssetsHelper assetsHelper)
        {
            _paymentSystemClient = paymentSystemClient;
            _requestContext = requestContext;
            _assetsHelper = assetsHelper;
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
            var partnerId = _requestContext.PartnerId;
            var result = await _paymentSystemClient.GetPaymentMethodsAsync(clientId);
            
            var cryptos = new PaymentMethod
            {
                Name = "Cryptos",
                Available = true,
                Assets = (await _assetsHelper.GetAllAssetsAsync())
                    .Where(x => x.BlockchainDepositEnabled)
                    .Select(x => x.Id)
                    .ToList()
            };
            
            var swift = new PaymentMethod
            {
                Name = "Swift",
                Available = true,
                Assets = (await _assetsHelper.GetAllAssetsAsync())
                    .Where(x => x.SwiftDepositEnabled)
                    .Select(x => x.Id)
                    .ToList()
            };
            
            result.PaymentMethods.Add(cryptos);
            result.PaymentMethods.Add(swift);

            var assetsAvailableToClient =
                await _assetsHelper.GetSetOfAssetsAvailableToClientAsync(clientId, partnerId, true);
            
            var model = new PaymentMethodsResponse
            {
                PaymentMethods = new List<PaymentMethod>()
            };

            foreach (var method in result.PaymentMethods)
            {
                var availableToClient = method.Assets.Where(assetsAvailableToClient.Contains);
                
                if(availableToClient.Any())
                    model.PaymentMethods.Add(new PaymentMethod
                    {
                        Assets = availableToClient.ToList(),
                        Available = method.Available,
                        Name = method.Name
                    });
            }
            
            return Ok(model);
        }
    }
}