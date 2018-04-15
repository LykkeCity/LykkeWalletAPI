using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.MarketProfileService.Client;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using LykkeApi2.Infrastructure;
using LykkeApi2.Models;
using LykkeApi2.Models.AssetPairRates;
using LykkeApi2.Models.AssetPairsModels;
using LykkeApi2.Models.ValidationModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Controllers
{
    [Route("api/[controller]")]
    [ValidateModel]
    public class AssetPairsController : Controller
    {
        private readonly CachedDataDictionary<string, AssetPair> _assetPairs;
        private readonly CachedDataDictionary<string, Asset> _assetsCache;
        private readonly IAssetsService _assetsService;
        private readonly ILykkeMarketProfileServiceAPI _marketProfileService;
        private readonly IRequestContext _requestContext;

        public AssetPairsController(
            CachedDataDictionary<string, AssetPair> assetPairs,
            CachedDataDictionary<string, Asset> assetsCache,
            IAssetsService assetsService,
            ILykkeMarketProfileServiceAPI marketProfile,
            IRequestContext requestContext)
        {
            _assetPairs = assetPairs;
            _assetsCache = assetsCache;
            _assetsService = assetsService;
            _marketProfileService = marketProfile;
            _requestContext = requestContext;
        }

        /// <summary>
        /// Get asset pairs.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(Models.AssetPairsModels.AssetPairResponseModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            var allAsetPairs = await _assetPairs.Values();
            var nonDisabledAassetPairs = allAsetPairs.Where(s => !s.IsDisabled);
            
            return Ok(Models.AssetPairsModels.AssetPairResponseModel.Create(nonDisabledAassetPairs.Select(itm => itm.ConvertToApiModel()).ToArray()));
        }

        /// <summary>
        /// Get available asset pairs.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("available")]
        [ProducesResponseType(typeof(Models.AssetPairsModels.AssetPairResponseModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAvailable()
        {
            var allNondisabledAssetPairs = (await _assetPairs.Values()).Where(s => !s.IsDisabled);

            var allTradableNondisabledAssets = (await _assetsCache.Values()).Where(x => !x.IsDisabled && x.IsTradable);

            var currentPartnersTradableNondisabledAssets = new HashSet<string>(allTradableNondisabledAssets.Where(x =>
            {
                if (x.NotLykkeAsset)
                {
                    return _requestContext.PartnerId != null && x.PartnerIds.Contains(_requestContext.PartnerId);
                }
                return _requestContext.PartnerId == null || x.PartnerIds.Contains(_requestContext.PartnerId);
            }).Select(x => x.Id));

            var assetsAvailableToUser = new HashSet<string>(await _assetsService.ClientGetAssetIdsAsync(_requestContext.ClientId, true));

            var availableAssetPairs =
                allNondisabledAssetPairs.Where(x =>
                    assetsAvailableToUser.Contains(x.BaseAssetId) &&
                    assetsAvailableToUser.Contains(x.QuotingAssetId) &&
                    currentPartnersTradableNondisabledAssets.Contains(x.BaseAssetId) &&
                    currentPartnersTradableNondisabledAssets.Contains(x.QuotingAssetId));

            return Ok(Models.AssetPairsModels.AssetPairResponseModel.Create(availableAssetPairs.Select(itm => itm.ConvertToApiModel()).ToArray()));
        }

        /// <summary>
        /// Get asset pair by id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Models.AssetPairsModels.AssetPairResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetAssetPairById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();
            
            var allAssetPairs = await _assetPairs.Values();
            var assetPair = allAssetPairs.Where(x => !x.IsDisabled).FirstOrDefault(x => x.Id == id);
            
            if (assetPair == null)
                return NotFound();
            
            return Ok(Models.AssetPairsModels.AssetPairResponseModel.Create(new List<AssetPairModel> { assetPair.ConvertToApiModel() }));
        }

        /// <summary>
        /// Get asset pair rates.
        /// </summary>
        /// <returns></returns>
        [HttpGet("rates")]
        [ProducesResponseType(typeof(AssetPairRatesResponseModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAssetPairRates()
        {
            var allAssetPairs = await _assetPairs.Values();
            var allNondisabledAssetPairs = allAssetPairs.Where(x => !x.IsDisabled);

            var allAssets = await _assetsCache.Values();
            var allTradableNondisabledAssets =
                new HashSet<string>(
                    allAssets
                    .Where(x => !x.IsDisabled && x.IsTradable)
                    .Select(x => x.Id));

            var assetPairsDict = allNondisabledAssetPairs.Where(x =>
                allTradableNondisabledAssets.Contains(x.BaseAssetId) &&
                allTradableNondisabledAssets.Contains(x.QuotingAssetId))
                .ToDictionary(x => x.Id);
            
            var marketProfile = await _marketProfileService.ApiMarketProfileGetAsync();

            marketProfile = marketProfile.Where(itm => assetPairsDict.ContainsKey(itm.AssetPair)).ToList();
            
            return Ok(AssetPairRatesResponseModel.Create(marketProfile.Select(m => m.ConvertToApiModel()).ToArray()));
        }

        /// <summary>
        /// Get asset pair rates by id.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("rates/{assetPairId}")]
        [ProducesResponseType(typeof(AssetPairRatesResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetAssetPairRatesById([FromRoute] AssetPairRequestModel request)
        {
            var asset = (await _assetPairs.Values()).FirstOrDefault(x => x.Id == request.AssetPairId);

            if (asset == null)
                return NotFound();

            var marketProfile = await _marketProfileService.ApiMarketProfileGetAsync();
            var feedData = marketProfile.FirstOrDefault(itm => itm.AssetPair == request.AssetPairId);

            if (feedData == null)
                return NotFound();

            return Ok(AssetPairRatesResponseModel.Create(new List<AssetPairRateModel> { feedData.ConvertToApiModel() }));
        }
    }
}