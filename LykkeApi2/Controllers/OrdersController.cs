using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common;
using Core.Identity;
using Core.Settings;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.History.Client;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Operations.Client;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.Operations.Contracts.Commands;
using Lykke.Service.Operations.Contracts.Orders;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.Session.Client;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Rest;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;
using LimitOrderCancelMultipleRequest = LykkeApi2.Models.Orders.LimitOrderCancelMultipleRequest;
using OrderAction = LykkeApi2.Models.Orders.OrderAction;

namespace LykkeApi2.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    [Route("api/orders")]
    [ApiController]
    public class OrdersController : Controller
    {
        private readonly IRequestContext _requestContext;
        private readonly ILykkePrincipal _lykkePrincipal;
        private readonly IClientSessionsClient _clientSessionsClient;
        private readonly IPersonalDataService _personalDataService;
        private readonly IKycStatusService _kycStatusService;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;
        private readonly IMatchingEngineClient _matchingEngineClient;
        private readonly FeeSettings _feeSettings;
        private readonly IOperationsClient _operationsClient;
        private readonly BaseSettings _baseSettings;
        private readonly IcoSettings _icoSettings;
        private readonly GlobalSettings _globalSettings;
        private readonly IHistoryClient _historyClient;

        public OrdersController(
            IRequestContext requestContext,
            ILykkePrincipal lykkePrincipal,
            IClientSessionsClient clientSessionsClient,
            IPersonalDataService personalDataService,
            IKycStatusService kycStatusService,
            IClientAccountClient clientAccountClient,
            IAssetsServiceWithCache assetsServiceWithCache,
            IMatchingEngineClient matchingEngineClient,
            FeeSettings feeSettings,
            IOperationsClient operationsClient,
            BaseSettings baseSettings,
            IcoSettings icoSettings,
            GlobalSettings globalSettings, 
            IHistoryClient historyClient)
        {
            _requestContext = requestContext;
            _lykkePrincipal = lykkePrincipal;
            _clientSessionsClient = clientSessionsClient;
            _personalDataService = personalDataService;
            _kycStatusService = kycStatusService;
            _clientAccountClient = clientAccountClient;
            _assetsServiceWithCache = assetsServiceWithCache;
            _matchingEngineClient = matchingEngineClient;
            _feeSettings = feeSettings;
            _operationsClient = operationsClient;
            _baseSettings = baseSettings;
            _icoSettings = icoSettings;
            _globalSettings = globalSettings;
            _historyClient = historyClient;
        }

        [HttpGet]
        [SwaggerOperation("GetActiveLimitOrders")]
        [ProducesResponseType(typeof(IEnumerable<LimitOrderResponseModel>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetLimitOrders(int offset = 0, int limit = 1000)
        {
            var clientId = _requestContext.ClientId;

            var orders = await _historyClient.OrdersApi.GetActiveOrdersByWalletAsync(Guid.Parse(clientId), offset, limit);

            return Ok(orders.Select(x => new LimitOrderResponseModel
            {
                AssetPairId = x.AssetPairId,
                CreateDateTime = x.CreateDt,
                Id = x.Id,
                Price = x.Price.GetValueOrDefault(0),
                LowerLimitPrice = x.LowerLimitPrice,
                LowerPrice = x.LowerPrice,
                UpperLimitPrice = x.UpperLimitPrice,
                UpperPrice = x.UpperPrice,
                Volume = Math.Abs(x.Volume),
                RemainingVolume = Math.Abs(x.RemainingVolume),
                OrderAction = x.Side.ToString(),
                Status = x.Status.ToString(),
                Type = x.Type.ToString()
            }));
        }

        [HttpPost("limit/{orderId}/cancel")]
        [SwaggerOperation("CancelLimitOrder")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CancelLimitOrder(string orderId)
        {
            var tradingSession = await _clientSessionsClient.GetTradingSession(_lykkePrincipal.GetToken());

            var confirmationRequired = _baseSettings.EnableSessionValidation && !(tradingSession?.Confirmed ?? false);
            if (confirmationRequired)
            {
                return BadRequest("Session confirmation is required");
            }

            if (!Guid.TryParse(orderId, out var id))
                return BadRequest();

            var clientId = _requestContext.ClientId;

            var order = await _historyClient.OrdersApi.GetOrderAsync(id);

            if (order.WalletId != Guid.Parse(clientId))
                return NotFound();

            var meResult = await _matchingEngineClient.CancelLimitOrderAsync(orderId);

            if (meResult.Status != MeStatusCodes.Ok)
                throw new Exception($"{orderId} order cancelation failed with {meResult.ToJson()}");

            return Ok();
        }

        [HttpPost("market")]
        [SwaggerOperation("PlaceMarketOrder")]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> PlaceMarketOrder([FromBody] MarketOrderRequest request)
        {
            var id = Guid.NewGuid();

            var asset = await _assetsServiceWithCache.TryGetAssetAsync(request.AssetId);
            var pair = await _assetsServiceWithCache.TryGetAssetPairAsync(request.AssetPairId);

            if (asset == null)
            {
                return NotFound($"Asset '{request.AssetId}' not found.");
            }

            if (pair == null)
            {
                return NotFound($"Asset pair '{request.AssetPairId}' not found.");
            }

            if (pair.IsDisabled)
            {
                return BadRequest($"Asset pair '{request.AssetPairId}' disabled.");
            }

            if (request.AssetId != pair.BaseAssetId && request.AssetId != pair.QuotingAssetId)
            {
                return BadRequest();
            }

            var baseAsset = await _assetsServiceWithCache.TryGetAssetAsync(pair.BaseAssetId);
            var quotingAsset = await _assetsServiceWithCache.TryGetAssetAsync(pair.QuotingAssetId);

            var tradingSession = await _clientSessionsClient.GetTradingSession(_lykkePrincipal.GetToken());

            var confirmationRequired = _baseSettings.EnableSessionValidation && !(tradingSession?.Confirmed ?? false);
            if (confirmationRequired)
            {
                return BadRequest("Session confirmation is required");
            }

            var command = new CreateMarketOrderCommand
            {
                AssetId = request.AssetId,
                AssetPair = new AssetPairModel
                {
                    Id = request.AssetPairId,
                    BaseAsset = ConvertAssetToAssetModel(baseAsset),
                    QuotingAsset = ConvertAssetToAssetModel(quotingAsset),
                    MinVolume = pair.MinVolume,
                    MinInvertedVolume = pair.MinInvertedVolume
                },
                Volume = Math.Abs(request.Volume),
                OrderAction = request.OrderAction == OrderAction.Buy
                     ? Lykke.Service.Operations.Contracts.Orders.OrderAction.Buy
                     : Lykke.Service.Operations.Contracts.Orders.OrderAction.Sell,
                Client = await GetClientModel(),
                GlobalSettings = GetGlobalSettings()
            };

            try
            {
                await _operationsClient.PlaceMarketOrder(id, command);
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == HttpStatusCode.BadRequest)
                    return BadRequest(JObject.Parse(e.Response.Content));

                throw;
            }

            return Created(Url.Action("Get", "Operations", new { id }), id);
        }

        [HttpPost("limit")]
        [SwaggerOperation("PlaceLimitOrder")]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> PlaceLimitOrder([FromBody] LimitOrderRequest request)
        {
            var id = Guid.NewGuid();

            var pair = await _assetsServiceWithCache.TryGetAssetPairAsync(request.AssetPairId);

            if (pair == null)
            {
                return NotFound($"Asset pair '{request.AssetPairId}' not found.");
            }

            if (pair.IsDisabled)
            {
                return BadRequest($"Asset pair '{request.AssetPairId}' disabled.");
            }

            var baseAsset = await _assetsServiceWithCache.TryGetAssetAsync(pair.BaseAssetId);
            var quotingAsset = await _assetsServiceWithCache.TryGetAssetAsync(pair.QuotingAssetId);

            var tradingSession = await _clientSessionsClient.GetTradingSession(_lykkePrincipal.GetToken());
            var confirmationRequired = _baseSettings.EnableSessionValidation && !(tradingSession?.Confirmed ?? false);
            if (confirmationRequired)
            {
                return BadRequest("Session confirmation is required");
            }

            var command = new CreateLimitOrderCommand
            {
                AssetPair = new AssetPairModel
                {
                    Id = request.AssetPairId,
                    BaseAsset = ConvertAssetToAssetModel(baseAsset),
                    QuotingAsset = ConvertAssetToAssetModel(quotingAsset),
                    MinVolume = pair.MinVolume,
                    MinInvertedVolume = pair.MinInvertedVolume
                },
                Volume = Math.Abs(request.Volume),
                Price = request.Price,
                OrderAction = request.OrderAction == OrderAction.Buy 
                    ? Lykke.Service.Operations.Contracts.Orders.OrderAction.Buy 
                    : Lykke.Service.Operations.Contracts.Orders.OrderAction.Sell,
                Client = await GetClientModel(),
                GlobalSettings = GetGlobalSettings()
            };

            try
            {
                await _operationsClient.PlaceLimitOrder(id, command);
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == HttpStatusCode.BadRequest)
                    return BadRequest(JObject.Parse(e.Response.Content));

                throw;
            }

            return Created(Url.Action("Get", "Operations", new { id }), id);                  
        }
        
        [HttpPost("stoplimit")]
        [SwaggerOperation("PlaceStopLimitOrder")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> PlaceStopLimitOrder([FromBody] StopLimitOrderRequest request)
        {
            var id = Guid.NewGuid();

            var pair = await _assetsServiceWithCache.TryGetAssetPairAsync(request.AssetPairId);

            if (pair == null)
                return NotFound($"Asset pair '{request.AssetPairId}' not found.");

            if (pair.IsDisabled)
                return BadRequest($"Asset pair '{request.AssetPairId}' disabled.");
            
            if (request.LowerLimitPrice.HasValue && request.LowerLimitPrice.Value <= 0 )
                return BadRequest($"{nameof(request.LowerLimitPrice)} must be > 0");
            
            if (request.LowerPrice.HasValue && request.LowerPrice.Value <= 0 )
                return BadRequest($"{nameof(request.LowerPrice)} must be > 0");
            
            if (request.UpperLimitPrice.HasValue && request.UpperLimitPrice.Value <= 0 )
                return BadRequest($"{nameof(request.UpperLimitPrice)} must be > 0");
            
            if (request.UpperPrice.HasValue && request.UpperPrice.Value <= 0 )
                return BadRequest($"{nameof(request.UpperPrice)} must be > 0");
            
            if (request.LowerLimitPrice.HasValue && !request.LowerPrice.HasValue)
                return BadRequest($"{nameof(request.LowerPrice)} must be set with {nameof(request.LowerLimitPrice)}");
            
            if (request.LowerPrice.HasValue && !request.LowerLimitPrice.HasValue)
                return BadRequest($"{nameof(request.LowerLimitPrice)} must be set with {nameof(request.LowerPrice)}");
            
            if (request.UpperLimitPrice.HasValue && !request.UpperPrice.HasValue)
                return BadRequest($"{nameof(request.UpperPrice)} must be set with {nameof(request.UpperLimitPrice)}");
            
            if (request.UpperPrice.HasValue && !request.UpperLimitPrice.HasValue)
                return BadRequest($"{nameof(request.UpperLimitPrice)} must be set with {nameof(request.UpperPrice)}");
            
            if (!request.LowerLimitPrice.HasValue && !request.LowerPrice.HasValue || 
                !request.UpperLimitPrice.HasValue && !request.UpperPrice.HasValue)
                return BadRequest("upper or lower prices must be set");
            
            if (request.LowerLimitPrice.HasValue && request.UpperLimitPrice.HasValue &&
                request.UpperLimitPrice <= request.LowerLimitPrice)
                return BadRequest($"{nameof(request.UpperLimitPrice)} must be greater than {nameof(request.LowerLimitPrice)}");
            
            var baseAsset = await _assetsServiceWithCache.TryGetAssetAsync(pair.BaseAssetId);
            var quotingAsset = await _assetsServiceWithCache.TryGetAssetAsync(pair.QuotingAssetId);

            var tradingSession = await _clientSessionsClient.GetTradingSession(_lykkePrincipal.GetToken());
            var confirmationRequired = _baseSettings.EnableSessionValidation && !(tradingSession?.Confirmed ?? false);
            if (confirmationRequired)
            {
                return BadRequest("Session confirmation is required");
            }

            var command = new CreateStopLimitOrderCommand
            {
                AssetPair = new AssetPairModel
                {
                    Id = request.AssetPairId,
                    BaseAsset = ConvertAssetToAssetModel(baseAsset),
                    QuotingAsset = ConvertAssetToAssetModel(quotingAsset),
                    MinVolume = pair.MinVolume,
                    MinInvertedVolume = pair.MinInvertedVolume
                },
                Volume = Math.Abs(request.Volume),
                LowerLimitPrice = request.LowerLimitPrice,
                LowerPrice = request.LowerPrice,
                UpperLimitPrice = request.UpperLimitPrice,
                UpperPrice = request.UpperPrice,
                OrderAction = request.OrderAction == OrderAction.Buy ? Lykke.Service.Operations.Contracts.Orders.OrderAction.Buy : Lykke.Service.Operations.Contracts.Orders.OrderAction.Sell,
                Client = await GetClientModel(),
                GlobalSettings = GetGlobalSettings()
            };

            try
            {
                await _operationsClient.PlaceStopLimitOrder(id, command);
            }
            catch (HttpOperationException e)
            {
                if (e.Response.StatusCode == HttpStatusCode.BadRequest)
                    return BadRequest(JObject.Parse(e.Response.Content));

                throw;
            }

            return Created(Url.Action("Get", "Operations", new { id }), id);
        }

        [HttpDelete("limit/{orderId}")]
        [SwaggerOperation("CancelLimitOrderNew")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        public Task<IActionResult> CancelLimitOrderNew(string orderId)
        {
            return CancelLimitOrder(orderId);
        }

        [HttpDelete("limit")]
        [SwaggerOperation("CancelMultipleLimitOrders")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CancelMultipleLimitOrders([FromBody]LimitOrderCancelMultipleRequest model)
        {
            var tradingSession = await _clientSessionsClient.GetTradingSession(_lykkePrincipal.GetToken());

            var confirmationRequired = _baseSettings.EnableSessionValidation && !(tradingSession?.Confirmed ?? false);
            if (confirmationRequired)
            {
                return BadRequest("Session confirmation is required");
            }

            if (!string.IsNullOrEmpty(model.AssetPairId))
            {
                var pair = await _assetsServiceWithCache.TryGetAssetPairAsync(model.AssetPairId);

                if (pair == null)
                {
                    return BadRequest($"Asset pair '{model.AssetPairId}' not found.");
                }
            }

            var clientId = _requestContext.ClientId;

            var activeOrders = await _historyClient.OrdersApi.GetActiveOrdersByWalletAsync(Guid.Parse(clientId), 0, 1);

            if (!activeOrders.Any())
                return Ok();

            var cancelModel = new LimitOrderMassCancelModel
            {
                AssetPairId = model.AssetPairId,
                ClientId = clientId,
                Id = Guid.NewGuid().ToString(),
                IsBuy = null
            };

            var meResult = await _matchingEngineClient.MassCancelLimitOrdersAsync(cancelModel);

            if (meResult.Status != MeStatusCodes.Ok)
                throw new Exception($"{cancelModel.ToJson()} request failed with {meResult.ToJson()}");

            return Ok();
        }

        private AssetModel ConvertAssetToAssetModel(Asset asset)
        {
            return new AssetModel
            {
                Id = asset.Id,
                DisplayId = asset.DisplayId ?? asset.Id,
                IsTradable = asset.IsTradable,
                IsTrusted = asset.IsTrusted,
                Accuracy = asset.Accuracy,
                Blockchain = asset.Blockchain.ToString(),
                KycNeeded = asset.KycNeeded,
                LykkeEntityId = asset.LykkeEntityId
            };
        }

        private async Task<ClientModel> GetClientModel()
        {
            var personalData = await _personalDataService.GetAsync(_requestContext.ClientId);

            return new ClientModel
            {
                Id = new Guid(_requestContext.ClientId),
                TradesBlocked = (await _clientAccountClient.GetCashOutBlockAsync(personalData.Id)).TradesBlocked,
                BackupDone = (await _clientAccountClient.GetBackupAsync(personalData.Id)).BackupDone,
                KycStatus = (await _kycStatusService.GetKycStatusAsync(personalData.Id)).ToString(),
                PersonalData = new PersonalDataModel
                {
                    Country = personalData.Country,
                    CountryFromID = personalData.CountryFromID,
                    CountryFromPOA = personalData.CountryFromPOA
                }
            };
        }

        private GlobalSettingsModel GetGlobalSettings()
        {
            return new GlobalSettingsModel
            {
                BlockedAssetPairs = _globalSettings.BlockedAssetPairs,
                BitcoinBlockchainOperationsDisabled = _globalSettings.BitcoinBlockchainOperationsDisabled,
                BtcOperationsDisabled = _globalSettings.BtcOperationsDisabled,
                IcoSettings = new IcoSettingsModel
                {
                    LKK2YAssetId = _icoSettings.LKK2YAssetId,
                    RestrictedCountriesIso3 = _icoSettings.RestrictedCountriesIso3
                },
                FeeSettings = new FeeSettingsModel
                {
                    FeeEnabled = _baseSettings.EnableFees,
                    TargetClientId = _feeSettings.TargetClientId.WalletApi
                }
            };
        }
    }
}
