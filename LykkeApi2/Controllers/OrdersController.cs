using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.MatchingEngine.Connector.Abstractions.Services;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.Kyc.Abstractions.Services;
using Lykke.Service.Operations.Client;
using Lykke.Service.Operations.Contracts;
using Lykke.Service.OperationsRepository.Client.Abstractions.CashOperations;
using Lykke.Service.PersonalData.Contract;
using Lykke.Service.PersonalData.Contract.Models;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models.Orders;
using LykkeApi2.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Rest;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;
using OrderAction = Lykke.MatchingEngine.Connector.Abstractions.Models.OrderAction;

namespace LykkeApi2.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    [Route("api/orders")]
    public class OrdersController : Controller
    {
        private readonly IRequestContext _requestContext;
        private readonly IPersonalDataService _personalDataService;
        private readonly IKycStatusService _kycStatusService;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IAssetsServiceWithCache _assetsServiceWithCache;
        private readonly IMatchingEngineClient _matchingEngineClient;
        private readonly ILimitOrdersRepositoryClient _limitOrdersRepository;        
        private readonly FeeSettings _feeSettings;
        private readonly IOperationsClient _operationsClient;
        private readonly BaseSettings _baseSettings;
        private readonly IcoSettings _icoSettings;
        private readonly GlobalSettings _globalSettings;

        public OrdersController(IRequestContext requestContext,
            IPersonalDataService personalDataService,
            IKycStatusService kycStatusService,
            IClientAccountClient clientAccountClient,
            IAssetsServiceWithCache assetsServiceWithCache,
            IMatchingEngineClient matchingEngineClient,
            ILimitOrdersRepositoryClient limitOrdersRepository,            
            FeeSettings feeSettings,
            IOperationsClient operationsClient,
            BaseSettings baseSettings,
            IcoSettings icoSettings,
            GlobalSettings globalSettings)
        {
            _requestContext = requestContext;
            _personalDataService = personalDataService;
            _kycStatusService = kycStatusService;
            _clientAccountClient = clientAccountClient;
            _assetsServiceWithCache = assetsServiceWithCache;
            _matchingEngineClient = matchingEngineClient;
            _limitOrdersRepository = limitOrdersRepository;            
            _feeSettings = feeSettings;
            _operationsClient = operationsClient;
            _baseSettings = baseSettings;
            _icoSettings = icoSettings;
            _globalSettings = globalSettings;
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
                Volume = Math.Abs((decimal)x.Volume),
                RemainingVolume = Math.Abs((decimal)(x.RemainingVolume ?? 0)),
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
            var personalData = await _personalDataService.GetAsync(_requestContext.ClientId);
            
            var command = await CreateOrderCommand<CreateOrderCommand>(request.AssetId, request.AssetPairId, request.Volume, asset, baseAsset, quotingAsset, pair, personalData);

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
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> PlaceLimitOrder([FromBody] LimitOrderRequest request)
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
            var personalData = await _personalDataService.GetAsync(_requestContext.ClientId);

            var command = await CreateOrderCommand<CreateLimitOrderCommand>(request.AssetId, request.AssetPairId, request.Volume, asset, baseAsset, quotingAsset, pair, personalData);
            command.Price = request.Price;

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

        private async Task<T> CreateOrderCommand<T>(
            string assetId, 
            string assetPairId, 
            double volume, 
            Asset asset, 
            Asset baseAsset, 
            Asset quotingAsset, 
            AssetPair pair,             
            IPersonalData personalData)
            where T : CreateOrderCommand, new()
        {
            return new T
            {
                AssetId = assetId,
                AssetPairId = assetPairId,
                Volume = volume,
                Asset = new AssetShortModel
                {
                    Id = asset.Id,
                    Blockchain = asset.Blockchain.ToString(),
                    IsTradable = asset.IsTradable,
                    IsTrusted = asset.IsTrusted
                },
                AssetPair = new AssetPairModel
                {
                    Id = assetPairId,
                    BaseAsset = new AssetModel
                    {
                        Id = baseAsset.Id,
                        IsTradable = baseAsset.IsTradable,
                        IsTrusted = baseAsset.IsTrusted,
                        Accuracy = baseAsset.Accuracy,
                        Blockchain = baseAsset.Blockchain.ToString(),
                        KycNeeded = baseAsset.KycNeeded,
                        LykkeEntityId = baseAsset.LykkeEntityId
                    },
                    QuotingAsset = new AssetModel
                    {
                        Id = quotingAsset.Id,
                        IsTradable = quotingAsset.IsTradable,
                        IsTrusted = quotingAsset.IsTrusted,
                        Accuracy = quotingAsset.Accuracy,
                        Blockchain = quotingAsset.Blockchain.ToString(),
                        KycNeeded = quotingAsset.KycNeeded,
                        LykkeEntityId = quotingAsset.LykkeEntityId
                    },
                    MinVolume = pair.MinVolume,
                    MinInvertedVolume = pair.MinInvertedVolume
                },
                Client = new ClientModel
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
                },
                GlobalSettings = new GlobalSettingsModel
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
                }
            };
        }
    }
}
