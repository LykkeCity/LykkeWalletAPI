using System;
using System.Net;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.MatchingEngine.Connector.Abstractions.Models;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.Service.Assets.Client;
using LykkeApi2.Attributes;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using OrderAction = Lykke.MatchingEngine.Connector.Abstractions.Models.OrderAction;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [SignatureVerification]
    [Route("api/[controller]")]
    public class OrdersController : Controller
    {
        private readonly IRequestContext _requestContext;
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;
        private readonly IMatchingEngineClient _matchingEngineClient;

        public OrdersController(IRequestContext requestContext,
            IAssetsServiceWithCache assetsServiceWithCache,
            IMatchingEngineClient matchingEngineClient)
        {
            _requestContext = requestContext;
            _assetsServiceWithCache = assetsServiceWithCache;
            _matchingEngineClient = matchingEngineClient;
        }

        [HttpPost("market")]
        [SwaggerOperation("PlaceMarketOrder")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> PlaceMarketOrder([FromBody] MarketOrderRequest request)
        {
            var clientId = _requestContext.ClientId;

            var pair = await _assetsServiceWithCache.TryGetAssetPairAsync(request.AssetPairId);

            if (pair == null)
            {
                return NotFound();
            }

            if (pair.IsDisabled)
            {
                return BadRequest();
            }

            var baseAsset = await _assetsServiceWithCache.TryGetAssetAsync(pair.BaseAssetId);

            if (baseAsset == null)
            {
                return NotFound();
            }

            var quotingAsset = await _assetsServiceWithCache.TryGetAssetAsync(pair.QuotingAssetId);

            if (quotingAsset == null)
            {
                return BadRequest();
            }

            if (request.AssetId != baseAsset.Id && request.AssetId != baseAsset.Name &&
                request.AssetId != quotingAsset.Id && request.AssetId != quotingAsset.Name)
            {
                return BadRequest();
            }

            var straight = request.AssetId == baseAsset.Id || request.AssetId == baseAsset.Name;
            var volume =
                request.Volume.TruncateDecimalPlaces(straight ? baseAsset.Accuracy : quotingAsset.Accuracy);
            if (Math.Abs(volume) < double.Epsilon)
            {
                return BadRequest(new {message = $"Required volume is less than asset accuracy."});
            }

            var order = new MarketOrderModel
            {
                Id = Guid.NewGuid().ToString(),
                AssetPairId = pair.Id,
                ClientId = clientId,
                ReservedLimitVolume = null,
                Straight = straight,
                Volume = Math.Abs(volume),
                OrderAction = ToMeOrderAction(request.OrderAction),
                Fee = null
            };

            var response = await _matchingEngineClient.HandleMarketOrderAsync(order);

            if (response == null)
                throw new Exception("ME unavailable");

            if (response.Status != MeStatusCodes.Ok)
                return BadRequest(new {message = $"ME result: {response.Status}"});

            return Ok(order.Id);
        }

        private OrderAction ToMeOrderAction(Models.Orders.OrderAction action)
        {
            OrderAction orderAction;
            switch (action)
            {
                case Models.Orders.OrderAction.Buy:
                    orderAction = OrderAction.Buy;
                    break;
                case Models.Orders.OrderAction.Sell:
                    orderAction = OrderAction.Sell;
                    break;
                default:
                    throw new Exception("Unknown order action");
            }

            return orderAction;
        }
    }
}