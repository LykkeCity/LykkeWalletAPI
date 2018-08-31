using System.Linq;
using LykkeApi2.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
using Core.Services;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using LykkeApi2.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace LykkeApi2.Controllers
{
    [Route("api/assets")]
    public class AssetsController : Controller
    {
        private readonly IClientAccountSettingsClient _clientAccountSettingsClient;
        private readonly IAssetsHelper _assetsHelper;
        private readonly IRequestContext _requestContext;

        public AssetsController(
            IClientAccountSettingsClient clientAccountSettingsClient,
            IAssetsHelper assetsHelper,
            IRequestContext requestContext)
        {
            _clientAccountSettingsClient = clientAccountSettingsClient;
            _assetsHelper = assetsHelper;
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
            var allAssets = await _assetsHelper.GetAllAssetsAsync();
            
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

            var asset = await _assetsHelper.GetAssetAsync(id);
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
            
            var asset = await _assetsHelper.GetAssetAsync(assetId);
            if (asset == null || asset.IsDisabled)
            {
                return NotFound();
            }
            
            var keyValues = await _assetsHelper.GetAssetsAttributesAsync(assetId);
            
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
            
            var asset = await _assetsHelper.GetAssetAsync(assetId);
            if (asset == null || asset.IsDisabled)
            {
                return NotFound();
            }
            
            var keyValues = await _assetsHelper.GetAssetAttributesAsync(assetId, key);
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
            var res = await _assetsHelper.GetAssetsExtendedInfosAsync();

            var allAssets = await _assetsHelper.GetAllAssetsAsync();
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
            
            var asset = await _assetsHelper.GetAssetAsync(assetId);
            if (asset == null || asset.IsDisabled)
            {
                return NotFound();
            }
            
            var extendedInfo = await _assetsHelper.GetAssetExtendedInfoAsync(assetId) ??
                               await _assetsHelper.GetDefaultAssetExtendedInfoAsync();
            
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
            var categories = await _assetsHelper.GetAssetCategoriesAsync();
            
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

            var res = await _assetsHelper.GetAssetCategoryAsync(id);
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
            var baseAsset = model.BaseAssetId ?? model.BaseAsssetId;
            
            if (string.IsNullOrWhiteSpace(baseAsset))
                return BadRequest();

            var asset = await _assetsHelper.GetAssetAsync(baseAsset);
            if (asset == null || asset.IsDisabled)
            {
                return NotFound();
            }

            var assetsAvailableToUser =
                await _assetsHelper.GetAssetsAvailableToClientAsync(_requestContext.ClientId, _requestContext.PartnerId,
                    true);

            if (!asset.IsBase || !assetsAvailableToUser.Contains(baseAsset))
                return BadRequest();

            await _clientAccountSettingsClient.SetBaseAssetAsync(_requestContext.ClientId, baseAsset);

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
            var assetsAvailableToUser =
                await _assetsHelper.GetAssetsAvailableToClientAsync(_requestContext.ClientId, _requestContext.PartnerId, true);
            
            return Ok(
                AssetIdsModel.Create(
                    assetsAvailableToUser
                        .OrderBy(x => x)
                        .ToArray()));
        }
    }
}