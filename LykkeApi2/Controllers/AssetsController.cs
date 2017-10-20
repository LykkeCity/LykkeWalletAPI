using Lykke.Service.Assets.Client.Custom;
using LykkeApi2.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeApi2.Controllers
{
    [Route("api/assets")]
    public class AssetsController : Controller
    {
        private readonly ICachedAssetsService _assetsService;

        public AssetsController(ICachedAssetsService assetsService)
        {
            _assetsService = assetsService;
        }

        /// <summary>
        /// Get assets.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> Get()
        {
            var assets = (await _assetsService.GetAllAssetsAsync()).Where(x => !x.IsDisabled);
            return Ok(GetBaseAssetsRespModel.Create(assets.Select(itm => itm.ConvertToApiModel()).ToArray()));
        }

        /// <summary>
        /// Get asset by id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> Get(string id)
        {
            var asset = await _assetsService.TryGetAssetAsync(id);
            if (asset == null)
            {
                ModelState.AddModelError("id", $"Asset {id} does not exist");
                return NotFound(ModelState);
            }
            return Ok(GetClientBaseAssetRespModel.Create(asset.ConvertToApiModel()));
        }

        /// <summary>
        /// Get asset attributes.
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [HttpGet("{assetId}/attributes")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> GetAssetAttributes(string assetId)
        {
            var keyValues = await _assetsService.GetAssetAttributesAsync(assetId);
            return Ok(keyValues.ConvertToApiModel());
        }

        /// <summary>
        /// Get asset attributes by key.
        /// </summary>
        /// <param name="assetId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        [HttpGet("{assetId}/attributes/{key}")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> GetAssetAttributeByKey(string assetId, string key)
        {
            var keyValues = await _assetsService.GetAssetAttributeByKeyAsync(assetId, key);
            return Ok(keyValues.ConvertToApiModel().Attrbuttes.FirstOrDefault() ?? new KeyValue());
        }

        /// <summary>
        /// Get asset descriptions.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("description")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> GetAssetDescriptions([FromBody]GetAssetDescriptionsRequestModel request)
        {
            var res = await _assetsService.GetAssetDescriptionsAsync(new Lykke.Service.Assets.Client.Models.GetAssetDescriptionsRequestModel { Ids = request.Ids });
            return Ok(AssetDescriptionsResponseModel.Create(res.Select(s => s.ConvertToApiModel()).ToList()));
        }

        /// <summary>
        /// Get asset description.
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [HttpGet("{assetId}/description")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> GetAssetDescription(string assetId)
        {
            var res = await _assetsService.GetAssetDescriptionsAsync(new Lykke.Service.Assets.Client.Models.GetAssetDescriptionsRequestModel { Ids = new List<string> { assetId } });
            return Ok(AssetDescriptionsResponseModel.Create(res.Select(s => s.ConvertToApiModel()).ToList()));
        }

        /// <summary>
        /// Get asset categories.
        /// </summary>
        /// <returns></returns>
        [HttpGet("categories")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> GetAssetCategories()
        {
            var res = await _assetsService.GetAssetCategoriesAsync();
            return Ok(GetAssetCategoriesResponseModel.Create(res.Select(itm => itm.ConvertToApiModel()).ToArray()));
        }

        /// <summary>
        /// Get asset category.
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [HttpGet("{assetId}/categories")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> GetAssetCategory(string assetId)
        {
            var res = await _assetsService.TryGetAssetCategoryAsync(assetId);

            if (res.errorResponse != null)
            {
                return NotFound($"Error while retrieving asset category for asset {assetId}. {res.errorResponse.ErrorMessages}");
            }

            return Ok(GetAssetCategoriesResponseModel.Create(new[] { res.ConvertToApiModel() }));
        }

        /// <summary>
        /// Get extended assets.
        /// </summary>
        /// <returns></returns>
        [HttpGet("extended")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> GetAssetsExtended()
        {
            var res = await _assetsService.GetAssetsExtendedAsync();
            var assetsExtended = res.Assets.Select(s => s.ConvertTpApiModel()).ToList();
            return Ok(AssetExtendedResponseModel.Create(assetsExtended));
        }

        /// <summary>
        /// Get extended asset.
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [HttpGet("{assetId}/extended")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> GetAssetsExtended(string assetId)
        {
            var res = await _assetsService.GetAssetExtendedByIdAsync(assetId);
            var assetsExtended = res.Assets.Select(s => s.ConvertTpApiModel()).ToList();
            return Ok(AssetExtendedResponseModel.Create(assetsExtended));
        }

    }
}
