using System;
using System.Linq;
using LykkeApi2.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
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
        private readonly IClientAccountSettingsClient _clientAccountSettingsClient;
        private readonly IRequestContext _requestContext;
        private readonly ILog _log;

        public AssetsController(IAssetsService assetsService, IClientAccountSettingsClient clientAccountSettingsClient, IRequestContext requestContext,
            ILog log)
        {
            _assetsService = assetsService;
            _clientAccountSettingsClient = clientAccountSettingsClient;
            _requestContext = requestContext;
            _log = log;
        }

        /// <summary>
        ///     Get assets.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(GetBaseAssetsRespModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            var allAssets = await _assetsService.AssetGetAllAsync();
            
            return Ok(
                GetBaseAssetsRespModel.Create(
                    allAssets
                        .Where(x => !x.IsDisabled)
                        .Select(x => x.ConvertToApiModel())
                        .OrderBy(x => x.DisplayId == null)
                        .ThenBy(x => x.DisplayId)
                        .ToArray()));
        }

        /// <summary>
        ///     Get asset by id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(GetClientBaseAssetRespModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest();

            var asset = await _assetsService.AssetGetAsync(id);
            if (asset == null)
            {
                return NotFound();
            }
            return Ok(GetClientBaseAssetRespModel.Create(asset.ConvertToApiModel()));
        }

        /// <summary>
        ///     Get asset attributes.
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [HttpGet("{assetId}/attributes")]
        [ProducesResponseType(typeof(AssetAttributesModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetAssetAttributes(string assetId)
        {
            var keyValues = await _assetsService.AssetAttributeGetAllForAssetAsync(assetId);
            if (keyValues == null)
                return NotFound();
            return Ok(keyValues.ConvertToApiModel());
        }

        /// <summary>
        ///     Get asset attributes by key.
        /// </summary>
        /// <param name="assetId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        [HttpGet("{assetId}/attributes/{key}")]
        [ProducesResponseType(typeof(KeyValue), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetAssetAttributeByKey(string assetId, string key)
        {
            var keyValues = await _assetsService.AssetAttributeGetAsync(assetId, key);
            if (keyValues == null)
                return NotFound();

            return Ok(keyValues.ConvertToApiModel());
        }

        /// <summary>
        ///     Get asset descriptions.
        /// </summary>
        /// <returns></returns>
        [HttpGet("description")]
        [ProducesResponseType(typeof(AssetDescriptionsResponseModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAssetDescriptions()
        {
            var res = await _assetsService.AssetExtendedInfoGetAllAsync();
            return Ok(AssetDescriptionsResponseModel.Create(res.Select(ConvertToAssetDescription).ToList()));
        }

        /// <summary>
        ///     Get asset description.
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [HttpGet("{assetId}/description")]
        [ProducesResponseType(typeof(AssetDescriptionModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAssetDescription(string assetId)
        {
            var extendedInfo = await _assetsService.AssetExtendedInfoGetAsync(assetId) ??
                               await _assetsService.AssetExtendedInfoGetDefaultAsync();
            if (string.IsNullOrEmpty(extendedInfo.Id))
                extendedInfo.Id = assetId;

            return Ok(ConvertToAssetDescription(extendedInfo));
        }

        private static AssetDescriptionModel ConvertToAssetDescription(AssetExtendedInfo extendedInfo)
        {
            return new AssetDescriptionModel
            {
                Id = extendedInfo.Id,
                AssetClass = extendedInfo.AssetClass,
                Description = extendedInfo.Description,
                IssuerName = null,
                MarketCapitalization = extendedInfo.MarketCapitalization,
                NumberOfCoins = extendedInfo.NumberOfCoins,
                PopIndex = extendedInfo.PopIndex,
                AssetDescriptionUrl = extendedInfo.AssetDescriptionUrl,
                FullName = extendedInfo.FullName
            };
        }

        /// <summary>
        ///     Get asset categories.
        /// </summary>
        /// <returns></returns>
        [HttpGet("categories")]
        [ProducesResponseType(typeof(GetAssetCategoriesResponseModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAssetCategories()
        {
            var res = await _assetsService.AssetCategoryGetAllAsync();
            return Ok(GetAssetCategoriesResponseModel.Create(res.Select(itm => itm.ConvertToApiModel()).ToArray()));
        }

        /// <summary>
        ///     Get asset category.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("categories/{id}")]
        [ProducesResponseType(typeof(GetAssetCategoriesResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetAssetCategory(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest();

            var res = await _assetsService.AssetCategoryGetAsync(id);
            if (res == null)
                return NotFound();

            return Ok(GetAssetCategoriesResponseModel.Create(new[] { res.ConvertToApiModel() }));
        }

        /// <summary>
        ///     Get extended assets.
        /// </summary>
        /// <returns></returns>
        [HttpGet("extended")]
        [ProducesResponseType(typeof(AssetExtendedResponseModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAssetsExtended()
        {
            var res = await _assetsService.AssetExtendedInfoGetAllAsync();
            var assetsExtended = res.Select(s => s.ConvertTpApiModel()).ToList();
            return Ok(AssetExtendedResponseModel.Create(assetsExtended));
        }

        /// <summary>
        ///     Get extended asset.
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [HttpGet("{assetId}/extended")]
        [ProducesResponseType(typeof(AssetExtendedResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetAssetsExtended(string assetId)
        {
            var res = await _assetsService.AssetExtendedInfoGetAsync(assetId);
            if (res == null)
                return NotFound();
            return Ok(AssetExtendedResponseModel.Create(new[] { res.ConvertTpApiModel() }));
        }

        [Authorize]
        [HttpGet("baseAsset")]
        [ProducesResponseType(typeof(BaseAssetClientModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetBaseAsset()
        {
            BaseAssetClientModel response;
            try
            {
                response = await _clientAccountSettingsClient.GetBaseAssetAsync(_requestContext.ClientId);
            }
            catch (Exception e)
            {
                await _log.WriteFatalErrorAsync(nameof(AssetsController), nameof(GetBaseAsset), e);
                return BadRequest(new { message = e.Message });
            }

            return Ok(response);
        }

        [Authorize]
        [HttpPost("baseAsset")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        [ProducesResponseType((int) HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> SetBaseAsset([FromBody] BaseAssetUpdateModel model)
        {
            try
            {
                var assetResponse = await _assetsService.AssetGetWithHttpMessagesAsync(model.BaseAsssetId);
                var asset = assetResponse?.Body as Asset;

                if (asset == null)
                    return NotFound();

                if (!asset.IsBase)
                    return BadRequest(new {message = "Asset can't be set as base"});

                await _clientAccountSettingsClient.SetBaseAssetAsync(_requestContext.ClientId, model.BaseAsssetId);
            }
            catch (Exception e)
            {
                await _log.WriteFatalErrorAsync(nameof(AssetsController), nameof(SetBaseAsset), e);
                return StatusCode((int) HttpStatusCode.InternalServerError);
            }

            return Ok();
        }
        
        /// <summary>
        ///     Get assets available for the user based on regulations.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("available")]
        [ProducesResponseType(typeof(AssetIdsResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetAvailableAssets()
        {
            var assetsIds = await _assetsService.ClientGetAssetIdsAsync(_requestContext.ClientId, true);
            if (assetsIds == null)
                return NotFound();
            
            return Ok(AssetIdsResponse.Create(assetsIds));
        }
    }
}
