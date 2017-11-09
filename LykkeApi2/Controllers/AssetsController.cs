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
        #region settings consts

        private const string BaseAssetSetting = "BaseAsset";

        #endregion

        private readonly IAssetsService _assetsService;
        private readonly IClientAccountSettingsClient _clientSettingsService;
        private readonly IRequestContext _requestContext;
        private readonly ILog _log;

        public AssetsController(IAssetsService assetsService, IClientAccountSettingsClient clientSettingsService, IRequestContext requestContext, ILog log)
        {
            _assetsService = assetsService;
            _clientSettingsService = clientSettingsService;
            _requestContext = requestContext;
            _log = log;
        }

        /// <summary>
        ///     Get assets.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ApiExplorerSettings(GroupName = "Exchange")]
        [ProducesResponseType(typeof(GetBaseAssetsRespModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Get()
        {
            var assets = (await _assetsService.AssetGetAllAsync()).Where(x => !x.IsDisabled);
            return Ok(GetBaseAssetsRespModel.Create(assets.Select(itm => itm.ConvertToApiModel()).ToArray()));
        }

        /// <summary>
        ///     Get asset by id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        [ProducesResponseType(typeof(GetClientBaseAssetRespModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get(string id)
        {
            var asset = await _assetsService.AssetGetAsync(id);
            if (asset == null)
            {
                ModelState.AddModelError("id", $"Asset {id} does not exist");
                return NotFound(ModelState);
            }
            return Ok(GetClientBaseAssetRespModel.Create(asset.ConvertToApiModel()));
        }

        /// <summary>
        ///     Get asset attributes.
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [HttpGet("{assetId}/attributes")]
        [ApiExplorerSettings(GroupName = "Exchange")]
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
        [ApiExplorerSettings(GroupName = "Exchange")]
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
        [ApiExplorerSettings(GroupName = "Exchange")]
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
        [ApiExplorerSettings(GroupName = "Exchange")]
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
        [ApiExplorerSettings(GroupName = "Exchange")]
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
        [HttpGet("categories{id}")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        [ProducesResponseType(typeof(GetAssetCategoriesResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetAssetCategory(string id)
        {
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
        [ApiExplorerSettings(GroupName = "Exchange")]
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
        [ApiExplorerSettings(GroupName = "Exchange")]
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
        [ApiExplorerSettings(GroupName = "Settings")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetBaseAsset()
        {
            BaseAssetClientModel response;
            try
            {
                response = await _clientSettingsService.GetBaseAssetAsync(_requestContext.ClientId);
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
        [ApiExplorerSettings(GroupName = "Settings")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> SetBaseAsset([FromBody] BaseAssetUpdateModel model)
        {
            try
            {
                await _clientSettingsService.SetBaseAssetAsync(_requestContext.ClientId, model.BaseAsssetId);
            }
            catch (Exception e)
            {
                await _log.WriteFatalErrorAsync(nameof(AssetsController), nameof(SetBaseAsset), e);
                return BadRequest(new { message = e.Message });
            }

            return Ok();
        }
    }
}
