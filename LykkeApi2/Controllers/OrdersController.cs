using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common;
using Lykke.MatchingEngine.Connector.Abstractions.Models;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.Service.Assets.Client;
using Lykke.Service.OperationsRepository.AutorestClient.Models;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using OrderAction = Lykke.MatchingEngine.Connector.Abstractions.Models.OrderAction;

namespace LykkeApi2.Controllers
{
    [Authorize]
    [Route("api/orders")]
    public class OrdersController : Controller
    {
        private readonly IRequestContext _requestContext;
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;
        private readonly IMatchingEngineClient _matchingEngineClient;
        private readonly ILimitOrdersRepositoryClient _limitOrdersRepository;

        public OrdersController(IRequestContext requestContext,
            IAssetsServiceWithCache assetsServiceWithCache,
            IMatchingEngineClient matchingEngineClient,
            ILimitOrdersRepositoryClient limitOrdersRepository)
        {
            _requestContext = requestContext;
            _assetsServiceWithCache = assetsServiceWithCache;
            _matchingEngineClient = matchingEngineClient;
            _limitOrdersRepository = limitOrdersRepository;
        }
        
        [HttpGet]
        [SwaggerOperation("GetActiveLimitOrders")]
        [ProducesResponseType(typeof(IEnumerable<LimitOrderResponseModel>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetLimitOrders()
        {
            var clientId = _requestContext.ClientId;

            var orders = await _limitOrdersRepository.GetActiveByClientIdAsync(clientId);
            
            return Ok(orders.Select(x => new LimitOrderResponseModel
            {
                AssetPairId = x.AssetPairId,
                CreateDateTime = x.CreatedAt,
                Id = x.Id,
                Price = (decimal)x.Price,
                Voume = Math.Abs((decimal)x.Volume),
                OrderAction = x.Volume > 0 ? "Buy" : "Sell",
                Status = x.Status
            }));
        }

        [HttpPost("limit/{orderId}/cancel")]
        [SwaggerOperation("CancelMarketOrder")]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> CancelMarketOrder(string orderId)
        {
            var clientId = _requestContext.ClientId;

            var activeOrders = await _limitOrdersRepository.GetActiveByClientIdAsync(clientId);

            if (activeOrders.All(x => x.Id != orderId))
                return NotFound();
			
            await _limitOrdersRepository.CancelByIdAsync(orderId);
            await _matchingEngineClient.CancelLimitOrderAsync(orderId);
            
            return Ok();
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
                return BadRequest(CreateErrorMessage("Required volume is less than asset accuracy"));
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
                return BadRequest(CreateErrorMessage($"ME responded: {response.Status}"));

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

            var price = order.Price.TruncateDecimalPlaces(pair.Accuracy);

            var request = new LimitOrderCreateRequest
            {
                AssetPairId = pair.Id,
                ClientId = clientId,
                CreatedAt = DateTime.Now,
                Id = id,
                Price = price,
                Volume = Math.Abs(volume) * (order.OrderAction == Models.Orders.OrderAction.Buy ? 1 : -1),
                Straight = true
            };
            
            await _limitOrdersRepository.AddAsync(request);
			
            try
            {
                var response = await _matchingEngineClient.PlaceLimitOrderAsync(
                    new LimitOrderModel
                    {
                        AssetPairId = pair.Id,
                        ClientId = clientId,
                        Fee = null,
                        Id = id,
                        Price = price,
                        Volume = Math.Abs(volume),
                        OrderAction = ToMeOrderAction(order.OrderAction)
                    });
                
                if (response == null)
                    throw new Exception("ME unavailable");

                if (response.Status != MeStatusCodes.Ok)
                {
                    return BadRequest(CreateErrorMessage($"ME responded: {response.Status}"));
                }
            }
            catch (Exception)
            {
                await _limitOrdersRepository.RemoveAsync(clientId, id);
                throw;
            }
            
            return Ok(id);
        }

        private object CreateErrorMessage(string errorMessage)
        {
            return new {message = errorMessage};
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