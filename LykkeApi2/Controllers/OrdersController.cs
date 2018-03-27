using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common;
using Core.Settings;
using Lykke.MatchingEngine.Connector.Abstractions.Models;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.FeeCalculator.Client;
using Lykke.Service.OperationsRepository.AutorestClient.Models;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using FeeType = Lykke.Service.FeeCalculator.AutorestClient.Models.FeeType;
using OrderAction = Lykke.MatchingEngine.Connector.Abstractions.Models.OrderAction;
using OrderStatus = Lykke.Service.OperationsRepository.AutorestClient.Models.OrderStatus;

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
        private readonly IFeeCalculatorClient _feeCalculatorClient;
        private readonly FeeSettings _feeSettings;
        private readonly BaseSettings _baseSettings;

        public OrdersController(IRequestContext requestContext,
            IAssetsServiceWithCache assetsServiceWithCache,
            IMatchingEngineClient matchingEngineClient,
            ILimitOrdersRepositoryClient limitOrdersRepository,
            IFeeCalculatorClient feeCalculatorClient,
            FeeSettings feeSettings,
            BaseSettings baseSettings)
        {
            _requestContext = requestContext;
            _assetsServiceWithCache = assetsServiceWithCache;
            _matchingEngineClient = matchingEngineClient;
            _limitOrdersRepository = limitOrdersRepository;
            _feeCalculatorClient = feeCalculatorClient;
            _feeSettings = feeSettings;
            _baseSettings = baseSettings;
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
                OrderAction = x.Volume > 0 ? OrderAction.Buy.ToString() : OrderAction.Sell.ToString(),
                Status = x.Status
            })
            .OrderByDescending(x => x.CreateDateTime));
        }

        [HttpPost("limit/{orderId}/cancel")]
        [SwaggerOperation("CancelLimitOrder")]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> CancelLimitOrder(string orderId)
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
                OrderAction = ToMeOrderAction(request.OrderAction)
            };

            if (_baseSettings.EnableFees)
                order.Fees = new[]
                    {await GetMarketOrderFee(clientId, request.AssetPairId, request.AssetId, request.OrderAction)};

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
                Straight = true,
            };
            
            await _limitOrdersRepository.AddAsync(request);

            try
            {
                var limitOrderModel = new LimitOrderModel
                {
                    AssetPairId = pair.Id,
                    ClientId = clientId,
                    Id = id,
                    Price = price,
                    Volume = Math.Abs(volume),
                    OrderAction = ToMeOrderAction(order.OrderAction),
                };

                if (_baseSettings.EnableFees)
                    limitOrderModel.Fee = await GetLimitOrderFee(clientId, pair, order.OrderAction);

                var response = await _matchingEngineClient.PlaceLimitOrderAsync(limitOrderModel);
                
                if (response == null)
                    throw new Exception("ME unavailable");

                if (response.Status != MeStatusCodes.Ok)
                {
                    var status = MeStatusCodeToOperationsRepositoryOrderStatus(response.Status);
                    
                    await _limitOrdersRepository.FinalizeAsync(
                        new LimitOrderFinalizeRequest
                        {
                            OrderId = id,
                            OrderStatus = status
                        });
                    
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
        
        private Lykke.Service.FeeCalculator.AutorestClient.Models.OrderAction ToFeeOrderAction(Models.Orders.OrderAction action)
        {
            Lykke.Service.FeeCalculator.AutorestClient.Models.OrderAction orderAction;
            switch (action)
            {
                case Models.Orders.OrderAction.Buy:
                    orderAction = Lykke.Service.FeeCalculator.AutorestClient.Models.OrderAction.Buy;
                    break;
                case Models.Orders.OrderAction.Sell:
                    orderAction = Lykke.Service.FeeCalculator.AutorestClient.Models.OrderAction.Sell;
                    break;
                default:
                    throw new Exception("Unknown order action");
            }

            return orderAction;
        }
        
        private async Task<MarketOrderFeeModel> GetMarketOrderFee(string clientId, string assetPairId, string assetId, Models.Orders.OrderAction orderAction)
        {
            var fee = await _feeCalculatorClient.GetMarketOrderAssetFee(clientId, assetPairId, assetId, ToFeeOrderAction(orderAction));

            return new MarketOrderFeeModel
            {
                Size = (double)fee.Amount,
                SizeType = fee.Type == FeeType.Absolute 
                    ? (int)FeeSizeType.ABSOLUTE 
                    : (int)FeeSizeType.PERCENTAGE,
                SourceClientId = clientId,
                TargetClientId = fee.TargetWalletId ?? _feeSettings.TargetClientId.WalletApi,
                Type = fee.Amount == 0m
                    ? (int)MarketOrderFeeType.NO_FEE
                    : (int)MarketOrderFeeType.CLIENT_FEE,
                AssetId = string.IsNullOrEmpty(fee.TargetAssetId)
                    ? Array.Empty<string>()
                    : new []{ fee.TargetAssetId }
            };
        }

        private async Task<LimitOrderFeeModel> GetLimitOrderFee(string clientId, AssetPair assetPair, Models.Orders.OrderAction orderAction)
        {
            var fee = await _feeCalculatorClient.GetLimitOrderFees(clientId, assetPair.Id, assetPair.BaseAssetId, ToFeeOrderAction(orderAction));

            return new LimitOrderFeeModel
            {
                MakerSize = (double)fee.MakerFeeSize,
                TakerSize = (double)fee.TakerFeeSize,
                SourceClientId = clientId,
                TargetClientId = _feeSettings.TargetClientId.WalletApi,
                Type = fee.MakerFeeSize == 0m && fee.TakerFeeSize == 0m ? (int)LimitOrderFeeType.NO_FEE : (int)LimitOrderFeeType.CLIENT_FEE
            };
        }

        private OrderStatus MeStatusCodeToOperationsRepositoryOrderStatus(MeStatusCodes code)
        {
            switch (code)
            {
                case MeStatusCodes.Ok:
                    return OrderStatus.InOrderBook;
                case MeStatusCodes.LowBalance:
                    return OrderStatus.NotEnoughFunds;
                case MeStatusCodes.UnknownAsset:
                    return OrderStatus.UnknownAsset;
                case MeStatusCodes.NoLiquidity:
                    return OrderStatus.NoLiquidity;
                case MeStatusCodes.NotEnoughFunds:
                    return OrderStatus.NotEnoughFunds;
                case MeStatusCodes.Dust:
                    return OrderStatus.Dust;
                case MeStatusCodes.ReservedVolumeHigherThanBalance:
                    return OrderStatus.ReservedVolumeGreaterThanBalance;
                case MeStatusCodes.NotFound:
                    return OrderStatus.UnknownAsset;
                case MeStatusCodes.LeadToNegativeSpread:
                    return OrderStatus.LeadToNegativeSpread;
                case MeStatusCodes.TooSmallVolume:
                    return OrderStatus.TooSmallVolume;
                case MeStatusCodes.InvalidFee:
                    return OrderStatus.InvalidFee;
                default:
                    throw new ArgumentOutOfRangeException(nameof(code), code, null);
            }
        }
    }
}
