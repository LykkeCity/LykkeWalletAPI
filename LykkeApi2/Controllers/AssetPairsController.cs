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
        private readonly CachedDataDictionary<string, AssetPair> _assetPairsCache;
        private readonly CachedDataDictionary<string, Asset> _assetsCache;
        private readonly IAssetsService _assetsService;
        private readonly ILykkeMarketProfileServiceAPI _marketProfileService;
        private readonly IRequestContext _requestContext;

        public AssetPairsController(
            CachedDataDictionary<string, AssetPair> assetPairsCache,
            CachedDataDictionary<string, Asset> assetsCache,
            IAssetsService assetsService,
            ILykkeMarketProfileServiceAPI marketProfile,
            IRequestContext requestContext)
        {
            _assetPairsCache = assetPairsCache;
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
            var allAsetPairs = await _assetPairsCache.Values();
            var nonDisabledAssetPairs = allAsetPairs.Where(s => !s.IsDisabled);
            
            var allAssets = await _assetsCache.Values();
            var nondisabledAssets = new HashSet<string>(allAssets.Where(x => !x.IsDisabled).Select(x => x.Id));

            var validAssetPairs = nonDisabledAssetPairs.Where(
                x => nondisabledAssets.Contains(x.BaseAssetId) &&
                     nondisabledAssets.Contains(x.QuotingAssetId));
            
            return Ok(Models.AssetPairsModels.AssetPairResponseModel.Create(
                validAssetPairs.Select(itm => itm.ToApiModel()).OrderBy(x => x.Id).ToArray()));
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
            var allNondisabledAssetPairs = (await _assetPairsCache.Values()).Where(s => !s.IsDisabled);

            var allTradableNondisabledAssets = (await _assetsCache.Values()).Where(x => !x.IsDisabled && x.IsTradable);

            var currentPartnersTradableNondisabledAssets = new HashSet<string>(allTradableNondisabledAssets.Where(x => x.NotLykkeAsset
                ? _requestContext.PartnerId != null && x.PartnerIds.Contains(_requestContext.PartnerId)
                : _requestContext.PartnerId == null || x.PartnerIds.Contains(_requestContext.PartnerId)).Select(x => x.Id));

            var assetsAvailableToUser = new HashSet<string>(await _assetsService.ClientGetAssetIdsAsync(_requestContext.ClientId, true));

            var availableAssetPairs =
                allNondisabledAssetPairs.Where(x =>
                    assetsAvailableToUser.Contains(x.BaseAssetId) &&
                    assetsAvailableToUser.Contains(x.QuotingAssetId) &&
                    currentPartnersTradableNondisabledAssets.Contains(x.BaseAssetId) &&
                    currentPartnersTradableNondisabledAssets.Contains(x.QuotingAssetId));

            return Ok(Models.AssetPairsModels.AssetPairResponseModel.Create(
                availableAssetPairs.Select(x => x.ToApiModel()).OrderBy(x => x.Id).ToArray()));
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
            
            var allAssetPairs = await _assetPairsCache.Values();
            var assetPair = allAssetPairs.Where(x => !x.IsDisabled).FirstOrDefault(x => x.Id == id);
            
            if (assetPair == null)
                return NotFound();

            var allAssets = await _assetsCache.Values();
            var nondisabledAssets = new HashSet<string>(allAssets.Where(x => !x.IsDisabled).Select(x => x.Id));

            if (!nondisabledAssets.Contains(assetPair.BaseAssetId) ||
                !nondisabledAssets.Contains(assetPair.QuotingAssetId))
                return NotFound();
            
            return Ok(Models.AssetPairsModels.AssetPairResponseModel.Create(new List<AssetPairModel> { assetPair.ToApiModel() }));
        }

        /// <summary>
        /// Get asset pair rates.
        /// </summary>
        /// <returns></returns>
        [HttpGet("rates")]
        [ProducesResponseType(typeof(AssetPairRatesResponseModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAssetPairRates()
        {
            var allAssetPairs = await _assetPairsCache.Values();
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
            var relevantMarketProfile = marketProfile.Where(itm => assetPairsDict.ContainsKey(itm.AssetPair));
            
            return Ok(AssetPairRatesResponseModel.Create(
                relevantMarketProfile.Select(x => x.ToApiModel()).OrderBy(x => x.AssetPair).ToArray()));
        }

        /// <summary>
        /// Get asset pair rates by id.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("rates/{assetPairId}")]
        [ProducesResponseType(typeof(AssetPairRatesResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetAssetPairRatesById([FromRoute] AssetPairRequestModel request)
        {
            if (string.IsNullOrWhiteSpace(request.AssetPairId))
                return BadRequest();
            
            var allAssetPairs = await _assetPairsCache.Values();
            var assetPair = allAssetPairs.FirstOrDefault(x => x.Id == request.AssetPairId);

            if (assetPair == null || assetPair.IsDisabled)
                return NotFound();
            
            var allAssets = await _assetsCache.Values();
            var allTradableNondisabledAssets =
                new HashSet<string>(
                    allAssets
                        .Where(x => !x.IsDisabled && x.IsTradable)
                        .Select(x => x.Id));
            
            if(!allTradableNondisabledAssets.Contains(assetPair.BaseAssetId) ||
               !allTradableNondisabledAssets.Contains(assetPair.QuotingAssetId))
                return NotFound();

            var marketProfile = await _marketProfileService.ApiMarketProfileGetAsync();
            var feedData = marketProfile.FirstOrDefault(itm => itm.AssetPair == request.AssetPairId);

            if (feedData == null)
                return NotFound();

            return Ok(AssetPairRatesResponseModel.Create(new List<AssetPairRateModel> { feedData.ToApiModel() }));
        }
    }
}