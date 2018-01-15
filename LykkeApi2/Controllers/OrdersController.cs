using System;
using System.Net;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.Settings;
using Lykke.MatchingEngine.Connector.Abstractions.Models;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.Service.Assets.Client;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.RateCalculator.Client;
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
        private readonly IRateCalculatorClient _rateCalculatorClient;
        private readonly double _deviation;

        public OrdersController(IRequestContext requestContext,
            IAssetsServiceWithCache assetsServiceWithCache,
            IMatchingEngineClient matchingEngineClient,
            IRateCalculatorClient rateCalculatorClient,
            OrdersSettings ordersSettings)
        {
            _requestContext = requestContext;
            _assetsServiceWithCache = assetsServiceWithCache;
            _matchingEngineClient = matchingEngineClient;
            _rateCalculatorClient = rateCalculatorClient;
            _deviation = ordersSettings.MaxLimitOrderDeviationPercent / 100;
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

        [HttpPost("limit")]
        [SwaggerOperation("PlaceLimitOrder")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> PlaceLimitOrder([FromBody] LimitOrderRequest order)
        {
            var clientId = _requestContext.ClientId;

            var pair = await _assetsServiceWithCache.TryGetAssetPairAsync(order.AssetPairId);

            if (pair == null)
            {
                return NotFound();
            }

            if (pair.IsDisabled)
            {
                return BadRequest();
            }

            var bestPrice =
                await _rateCalculatorClient.GetBestPriceAsync(order.AssetPairId,
                    order.OrderAction == Models.Orders.OrderAction.Buy);

            if (order.OrderAction == Models.Orders.OrderAction.Buy && bestPrice * (1 - _deviation) > order.Price
                || order.OrderAction == Models.Orders.OrderAction.Sell &&
                bestPrice * (1 + _deviation) < order.Price)
            {
                return BadRequest();
            }

            var asset = await _assetsServiceWithCache.TryGetAssetAsync(pair.BaseAssetId);
            if (asset == null || asset.IsDisabled)
            {
                return BadRequest();
            }

            var volume = order.Volume.TruncateDecimalPlaces(asset.Accuracy);
            if (Math.Abs(volume) < double.Epsilon)
            {
                return BadRequest();
            }

            var id = Guid.NewGuid().ToString();

            var response = await _matchingEngineClient.PlaceLimitOrderAsync(
                new LimitOrderModel
                {
                    AssetPairId = pair.Id,
                    ClientId = clientId,
                    Fee = null,
                    Id = id,
                    Price = order.Price.TruncateDecimalPlaces(pair.Accuracy),
                    Volume = volume
                });

            if (response.Status != MeStatusCodes.Ok)
            {
                return BadRequest();
            }

            return Ok(id);
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