using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Antares.Service.Assets.Client;
using Common;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.MatchingEngine.Connector.Models.Api;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Assets.Core.Domain;
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
using Swashbuckle.AspNetCore.Annotations;
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
        private readonly IClientSessionsClient _clientSessionsClient;
        private readonly IPersonalDataService _personalDataService;
        private readonly IKycStatusService _kycStatusService;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IAssetsServiceClient _assetsServiceClient;
        private readonly IMatchingEngineClient _matchingEngineClient;
        private readonly FeeSettings _feeSettings;
        private readonly IOperationsClient _operationsClient;
        private readonly BaseSettings _baseSettings;
        private readonly IcoSettings _icoSettings;
        private readonly GlobalSettings _globalSettings;
        private readonly IHistoryClient _historyClient;

        public OrdersController(
            IRequestContext requestContext,
            IClientSessionsClient clientSessionsClient,
            IPersonalDataService personalDataService,
            IKycStatusService kycStatusService,
            IClientAccountClient clientAccountClient,
            IAssetsServiceClient assetsServiceClient,
            IMatchingEngineClient matchingEngineClient,
            FeeSettings feeSettings,
            IOperationsClient operationsClient,
            BaseSettings baseSettings,
            IcoSettings icoSettings,
            GlobalSettings globalSettings,
            IHistoryClient historyClient)
        {
            _requestContext = requestContext;
            _clientSessionsClient = clientSessionsClient;
            _personalDataService = personalDataService;
            _kycStatusService = kycStatusService;
            _clientAccountClient = clientAccountClient;
            _assetsServiceClient = assetsServiceClient;
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
        public async Task<IActionResult> GetLimitOrders(int offset = 0, int limit = 1000, string assetPair = null)
        {
            var clientId = _requestContext.ClientId;

            var orders = await _historyClient.OrdersApi.GetActiveOrdersByWalletAsync(Guid.Parse(clientId), offset, limit, assetPair);

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
            var tradingSession = await _clientSessionsClient.GetTradingSession(_requestContext.SessionId);

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

            var asset = _assetsServiceClient.Assets.Get(request.AssetId);
            var pair = _assetsServiceClient.AssetPairs.Get(request.AssetPairId);

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

            var baseAsset = _assetsServiceClient.Assets.Get(pair.BaseAssetId);
            var quotingAsset = _assetsServiceClient.Assets.Get(pair.QuotingAssetId);

            var tradingSession = await _clientSessionsClient.GetTradingSession(_requestContext.SessionId);

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

            var pair = _assetsServiceClient.AssetPairs.Get(request.AssetPairId);

            if (pair == null)
            {
                return NotFound($"Asset pair '{request.AssetPairId}' not found.");
            }

            if (pair.IsDisabled)
            {
                return BadRequest($"Asset pair '{request.AssetPairId}' disabled.");
            }

            var baseAsset = _assetsServiceClient.Assets.Get(pair.BaseAssetId);
            var quotingAsset = _assetsServiceClient.Assets.Get(pair.QuotingAssetId);

            var tradingSession = await _clientSessionsClient.GetTradingSession(_requestContext.SessionId);
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
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> PlaceStopLimitOrder([FromBody] StopLimitOrderRequest request)
        {
            var id = Guid.NewGuid();

            var pair = _assetsServiceClient.AssetPairs.Get(request.AssetPairId);

            if (pair == null)
                return NotFound($"Asset pair '{request.AssetPairId}' not found.");

            if (pair.IsDisabled)
                return BadRequest($"Asset pair '{request.AssetPairId}' disabled.");

            var baseAsset = _assetsServiceClient.Assets.Get(pair.BaseAssetId);
            var quotingAsset = _assetsServiceClient.Assets.Get(pair.QuotingAssetId);

            var tradingSession = await _clientSessionsClient.GetTradingSession(_requestContext.SessionId);
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
            var tradingSession = await _clientSessionsClient.GetTradingSession(_requestContext.SessionId);

            var confirmationRequired = _baseSettings.EnableSessionValidation && !(tradingSession?.Confirmed ?? false);
            if (confirmationRequired)
            {
                return BadRequest("Session confirmation is required");
            }

            if (!string.IsNullOrEmpty(model.AssetPairId))
            {
                var pair = _assetsServiceClient.AssetPairs.Get(model.AssetPairId);

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

        private AssetModel ConvertAssetToAssetModel(IAsset asset)
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
            var personalDataTask = _personalDataService.GetAsync(_requestContext.ClientId);
            var cashoutBlockTask = _clientAccountClient.ClientSettings.GetCashOutBlockSettingsAsync(_requestContext.ClientId);
            var backupDoneTask = _clientAccountClient.ClientSettings.GetBackupSettingsAsync(_requestContext.ClientId);
            var kycStatusTask = _kycStatusService.GetKycStatusAsync(_requestContext.ClientId);

            await Task.WhenAll(personalDataTask, cashoutBlockTask, backupDoneTask, kycStatusTask);

            var personalData = personalDataTask.Result;

            return new ClientModel
            {
                Id = new Guid(_requestContext.ClientId),
                TradesBlocked = cashoutBlockTask.Result.TradesBlocked,
                BackupDone = backupDoneTask.Result.BackupDone,
                KycStatus = kycStatusTask.Result.ToString(),
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
