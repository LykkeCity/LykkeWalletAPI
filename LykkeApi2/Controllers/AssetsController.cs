using System;
using System.Collections.Generic;
using System.Linq;
using LykkeApi2.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.Assets.Client;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.ClientAccount.Client.Models;
using LykkeApi2.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace LykkeApi2.Controllers
{
    [Route("api/assets")]
    public class AssetsController : Controller
    {
        private readonly IAssetsService _assetsService;
        private readonly CachedDataDictionary<string, Asset> _assetsCache;
        private readonly IClientAccountSettingsClient _clientAccountSettingsClient;
        private readonly IRequestContext _requestContext;

        public AssetsController(
            IAssetsService assetsService,
            CachedDataDictionary<string, Asset> assetsCache,
            IClientAccountSettingsClient clientAccountSettingsClient,
            IRequestContext requestContext)
        {
            _assetsService = assetsService;
            _assetsCache = assetsCache;
            _clientAccountSettingsClient = clientAccountSettingsClient;
            _requestContext = requestContext;
        }

        /// <summary>
        /// Get assets.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(AssetsModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            var allAssets = await _assetsService.AssetGetAllAsync();
            
            return Ok(
                AssetsModel.Create(
                    allAssets
                        .Where(x => !x.IsDisabled)
                        .Select(x => x.ToApiModel())
                        .OrderBy(x => x.DisplayId == null)
                        .ThenBy(x => x.DisplayId)
                        .ToArray()));
        }

        /// <summary>
        /// Get asset by id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AssetRespModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();

            var assets = await _assetsCache.Values();
            var asset = assets.FirstOrDefault(x => x.Id == id);
            if (asset == null || asset.IsDisabled)
            {
                return NotFound();
            }
            
            return Ok(AssetRespModel.Create(asset.ToApiModel()));
        }

        /// <summary>
        /// Get asset attributes.
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [HttpGet("{assetId}/attributes")]
        [ProducesResponseType(typeof(AssetAttributesModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetAssetAttributes(string assetId)
        {
            if (string.IsNullOrWhiteSpace(assetId))
                return BadRequest();
            
            var assets = await _assetsCache.Values();
            var asset = assets.FirstOrDefault(x => x.Id == assetId);
            if (asset == null || asset.IsDisabled)
            {
                return NotFound();
            }
            
            var keyValues = await _assetsService.AssetAttributeGetAllForAssetAsync(assetId);
            
            return Ok(keyValues.ToApiModel());
        }

        /// <summary>
        /// Get asset attributes by key.
        /// </summary>
        /// <param name="assetId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        [HttpGet("{assetId}/attributes/{key}")]
        [ProducesResponseType(typeof(KeyValue), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetAssetAttributeByKey(string assetId, string key)
        {
            if (string.IsNullOrWhiteSpace(assetId) || string.IsNullOrWhiteSpace(key))
                return BadRequest();
            
            var assets = await _assetsCache.Values();
            var asset = assets.FirstOrDefault(x => x.Id == assetId);
            if (asset == null || asset.IsDisabled)
            {
                return NotFound();
            }
            
            var keyValues = await _assetsService.AssetAttributeGetAsync(assetId, key);
            if (keyValues == null)
                return NotFound();

            return Ok(keyValues.ToApiModel());
        }

        /// <summary>
        /// Get asset descriptions.
        /// </summary>
        /// <returns></returns>
        [HttpGet("description")]
        [ProducesResponseType(typeof(AssetDescriptionsModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAssetDescriptions()
        {
            var res = await _assetsService.AssetExtendedInfoGetAllAsync();

            var allAssets = await _assetsCache.Values();
            var assetsIsDisabledDict = allAssets.ToDictionary(x => x.Id, x => x.IsDisabled);

            var nondisabledAssets = res.Where(x => assetsIsDisabledDict.ContainsKey(x.Id) && !assetsIsDisabledDict[x.Id]);
            
            return Ok(AssetDescriptionsModel.Create(
                nondisabledAssets.Select(x => x.ToApiModel()).OrderBy(x => x.Id).ToArray()));
        }

        /// <summary>
        /// Get asset description.
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [HttpGet("{assetId}/description")]
        [ProducesResponseType(typeof(AssetDescriptionModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetAssetDescription(string assetId)
        {
            if (string.IsNullOrWhiteSpace(assetId))
                return BadRequest();
            
            var assets = await _assetsCache.Values();
            var asset = assets.FirstOrDefault(x => x.Id == assetId);
            if (asset == null || asset.IsDisabled)
            {
                return NotFound();
            }
            
            var extendedInfo = await _assetsService.AssetExtendedInfoGetAsync(assetId) ??
                               await _assetsService.AssetExtendedInfoGetDefaultAsync();
            
            if (string.IsNullOrEmpty(extendedInfo.Id))
                extendedInfo.Id = assetId;

            return Ok(extendedInfo.ToApiModel());
        }

        /// <summary>
        /// Get asset categories.
        /// </summary>
        /// <returns></returns>
        [HttpGet("categories")]
        [ProducesResponseType(typeof(AssetCategoriesModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAssetCategories()
        {
            var categories = await _assetsService.AssetCategoryGetAllAsync();
            
            return Ok(AssetCategoriesModel.Create(categories.Select(x => x.ToApiModel()).OrderBy(x => x.Id).ToArray()));
        }

        /// <summary>
        /// Get asset category.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("categories/{id}")]
        [ProducesResponseType(typeof(AssetCategoriesModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetAssetCategory(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();

            var res = await _assetsService.AssetCategoryGetAsync(id);
            if (res == null)
                return NotFound();

            return Ok(AssetCategoriesModel.Create(new[] { res.ToApiModel() }));
        }

        [Authorize]
        [HttpGet("baseAsset")]
        [ProducesResponseType(typeof(BaseAssetClientModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetBaseAsset()
        {
            var baResp = await _clientAccountSettingsClient.GetBaseAssetAsync(_requestContext.ClientId);

            var baseAssetModel = new BaseAssetModel
            {
                BaseAssetId = baResp.BaseAssetId
            };
            
            return Ok(baseAssetModel);
        }

        [Authorize]
        [HttpPost("baseAsset")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> SetBaseAsset([FromBody] BaseAssetUpdateModel model)
        {
            if (string.IsNullOrWhiteSpace(model.BaseAsssetId))
                return BadRequest();

            var assets = await _assetsCache.Values();
            var asset = assets.FirstOrDefault(x => x.Id == model.BaseAsssetId);
            
            if (asset == null || asset.IsDisabled)
            {
                return NotFound();
            }
            
            var partnerEligible = asset.NotLykkeAsset
                ? _requestContext.PartnerId != null && asset.PartnerIds.Contains(_requestContext.PartnerId)
                : _requestContext.PartnerId == null || asset.PartnerIds.Contains(_requestContext.PartnerId);

            if (!asset.IsBase || !partnerEligible || !asset.IsTradable)
                return BadRequest();
            
            var assetsAvailableToUser = await _assetsService.ClientGetAssetIdsAsync(_requestContext.ClientId, true);
            
            if (assetsAvailableToUser.All(x => x != asset.Id))
                return BadRequest();

            await _clientAccountSettingsClient.SetBaseAssetAsync(_requestContext.ClientId, model.BaseAsssetId);

            return Ok();
        }
        
        /// <summary>
        /// Get assets available for the user based on regulations.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("available")]
        [ProducesResponseType(typeof(AssetIdsModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAvailableAssets()
        {
            var allTradableNondisabledAssets = (await _assetsCache.Values()).Where(x => !x.IsDisabled && x.IsTradable);
            
            var currentPartnersTradableNondisabledAssets = new HashSet<string>(allTradableNondisabledAssets.Where(x => x.NotLykkeAsset
                ? _requestContext.PartnerId != null && x.PartnerIds.Contains(_requestContext.PartnerId)
                : _requestContext.PartnerId == null || x.PartnerIds.Contains(_requestContext.PartnerId)).Select(x => x.Id));
            
            var assetsAvailableToUser = await _assetsService.ClientGetAssetIdsAsync(_requestContext.ClientId, true);
            
            return Ok(
                AssetIdsModel.Create(
                    assetsAvailableToUser
                        .Where(x => currentPartnersTradableNondisabledAssets.Contains(x))
                        .OrderBy(x => x)
                        .ToArray()));
        }
    }
}