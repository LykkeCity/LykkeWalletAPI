using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.Services;
using Google.Protobuf.WellKnownTypes;
using Lykke.Payments.Link4Pay;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.PaymentSystem.Client.AutorestClient.Models;
using LykkeApi2.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentMethodsController : Controller
    {
        private readonly Link4PayService.Link4PayServiceClient _link4PayServiceClient;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IRequestContext _requestContext;
        private readonly IAssetsHelper _assetsHelper;

        public PaymentMethodsController(
            Link4PayService.Link4PayServiceClient link4PayServiceClient,
            IClientAccountClient clientAccountClient,
            IRequestContext requestContext,
            IAssetsHelper assetsHelper)
        {
            _link4PayServiceClient = link4PayServiceClient;
            _clientAccountClient = clientAccountClient;
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

            var supportedCurrenciesTask = _link4PayServiceClient.GetSupportedCurrenciesAsync(new Empty()).ResponseAsync;
            var allAssetsTask = _assetsHelper.GetAllAssetsAsync();
            var assetsAvailableToClientTask = _assetsHelper.GetSetOfAssetsAvailableToClientAsync(clientId, partnerId, true);
            var depositBlockedTask = _clientAccountClient.ClientSettings.GetDepositBlockSettingsAsync(clientId);

            await Task.WhenAll(supportedCurrenciesTask, allAssetsTask, assetsAvailableToClientTask, depositBlockedTask);

            IReadOnlyCollection<Asset> allAssts = allAssetsTask.Result;
            HashSet<string> assetsAvailableToClient = assetsAvailableToClientTask.Result;

            var result = new PaymentMethodsResponse
            {
                PaymentMethods = new List<PaymentMethod>
                {
                    //TODO: remove after web wallet release
                    new PaymentMethod
                    {
                        Name = "Fxpaygate",
                        Available = !depositBlockedTask.Result.DepositViaCreditCardBlocked,
                        Assets = supportedCurrenciesTask.Result.Currencies
                    },
                    new PaymentMethod
                    {
                        Name = "Link4Pay",
                        Available = !depositBlockedTask.Result.DepositViaCreditCardBlocked,
                        Assets = supportedCurrenciesTask.Result.Currencies
                    },
                    new PaymentMethod
                    {
                        Name = "Cryptos",
                        Available = true,
                        Assets = allAssts
                            .Where(x => x.BlockchainDepositEnabled)
                            .Select(x => x.Id)
                            .ToList()
                    },
                    new PaymentMethod
                    {
                        Name = "Swift",
                        Available = true,
                        Assets = allAssts
                            .Where(x => x.SwiftDepositEnabled)
                            .Select(x => x.Id)
                            .ToList()
                    }
                }
            };

            var model = new PaymentMethodsResponse
            {
                PaymentMethods = new List<PaymentMethod>()
            };

            foreach (var method in result.PaymentMethods)
            {
                var availableToClient = method.Assets.Where(assetsAvailableToClient.Contains).ToList();

                if (availableToClient.Any())
                {
                    model.PaymentMethods.Add(new PaymentMethod
                     {
                         Assets = availableToClient,
                         Available = method.Available,
                         Name = method.Name
                     });
                }
            }

            return Ok(model);
        }
    }
}
