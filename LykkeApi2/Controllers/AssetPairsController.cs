using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core.Services;
using LkeServices;
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
    [ApiController]
    public class AssetPairsController : Controller
    {
        private readonly ILykkeMarketProfileServiceAPI _marketProfileService;
        private readonly IAssetsHelper _assetsHelper;
        private readonly IRequestContext _requestContext;

        public AssetPairsController(
            ILykkeMarketProfileServiceAPI marketProfile,
            IAssetsHelper assetsHelper,
            IRequestContext requestContext)
        {
            _marketProfileService = marketProfile;
            _assetsHelper = assetsHelper;
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
            var allAsetPairs = await _assetsHelper.GetAllAssetPairsAsync();
            var nonDisabledAssetPairs = allAsetPairs.Where(s => !s.IsDisabled);
            
            var allAssets = await _assetsHelper.GetAllAssetsAsync();
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
            var availableAssetPairs =
                await _assetsHelper.GetAssetPairsAvailableToClientAsync(_requestContext.ClientId,
                    _requestContext.PartnerId, true);
            
            return Ok(Models.AssetPairsModels.AssetPairResponseModel.Create(
                    availableAssetPairs
                        .Select(x => x.ToApiModel())
                        .OrderBy(x => x.Id)
                        .ToArray()
                ));
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

            var assetPair = await _assetsHelper.GetAssetPairAsync(id);
            if (assetPair == null || assetPair.IsDisabled)
                return NotFound();

            var allAssets = await _assetsHelper.GetAllAssetsAsync();
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
            var allAssetPairs = await _assetsHelper.GetAllAssetPairsAsync();
            var allNondisabledAssetPairs = allAssetPairs.Where(x => !x.IsDisabled);

            var allAssets = await _assetsHelper.GetAllAssetsAsync();
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
            
            var assetPair = await _assetsHelper.GetAssetPairAsync(request.AssetPairId);

            if (assetPair == null || assetPair.IsDisabled)
                return NotFound();
            
            var allAssets = await _assetsHelper.GetAllAssetsAsync();
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
