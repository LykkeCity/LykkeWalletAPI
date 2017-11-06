using System;
using Lykke.Service.Assets.Client.Custom;
using LykkeApi2.Models;
using LykkeApi2.Models.ResponceModels;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;

namespace LykkeApi2.Controllers
{
    [Route("api/[controller]")]
    public class AssetsController : Controller
    {
        #region settings consts

        private const string BaseAssetSetting = "BaseAsset";

        #endregion

        private readonly ICachedAssetsService _assetsService;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly ILog _log;

        public AssetsController(ICachedAssetsService assetsService, IClientAccountClient clientAccountClient,
            ILog log)
        {
            _assetsService = assetsService;
            _clientAccountClient = clientAccountClient;
            _log = log;
        }

        [HttpGet]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> Get()
        {
            var assets = (await _assetsService.GetAllAssetsAsync()).Where(x => !x.IsDisabled);
            return Ok(GetBaseAssetsRespModel.Create(assets.Select(itm => itm.ConvertToApiModel()).ToArray()));
        }

        [HttpGet("{id}")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> Get(string id)
        {
            var asset = await _assetsService.TryGetAssetAsync(id);
            if (asset == null)
            {
                ModelState.AddModelError("id", $"Asset {id} does not exist");
                return NotFound(new ApiBadRequestResponse(ModelState));
            }
            return Ok(GetClientBaseAssetRespModel.Create(asset.ConvertToApiModel()));
        }

        [HttpGet("{assetId}/attributes")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> GetAssetAttributes(string assetId)
        {
            var keyValues = await _assetsService.GetAssetAttributesAsync(assetId);
            return Ok(keyValues.ConvertToApiModel());
        }

        [HttpGet("{assetId}/attributes/{key}")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> GetAssetAttributeByKey(string assetId, string key)
        {
            var keyValues = await _assetsService.GetAssetAttributeByKeyAsync(assetId, key);
            return Ok(keyValues.ConvertToApiModel().Attrbuttes.FirstOrDefault() ?? new KeyValue());
        }

        [HttpPost("description")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> GetAssetDescriptions([FromBody] GetAssetDescriptionsRequestModel request)
        {
            var res = await _assetsService.GetAssetDescriptionsAsync(
                new Lykke.Service.Assets.Client.Models.GetAssetDescriptionsRequestModel {Ids = request.Ids});
            return Ok(AssetDescriptionsResponseModel.Create(res.Select(s => s.ConvertToApiModel()).ToList()));
        }

        [HttpGet("{assetId}/description")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> GetAssetDescription(string assetId)
        {
            var res = await _assetsService.GetAssetDescriptionsAsync(
                new Lykke.Service.Assets.Client.Models.GetAssetDescriptionsRequestModel
                {
                    Ids = new List<string> {assetId}
                });
            return Ok(AssetDescriptionsResponseModel.Create(res.Select(s => s.ConvertToApiModel()).ToList()));
        }

        [HttpGet("categories")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> GetAssetCategories()
        {
            var res = await _assetsService.GetAssetCategoriesAsync();
            return Ok(GetAssetCategoriesResponseModel.Create(res.Select(itm => itm.ConvertToApiModel()).ToArray()));
        }

        [HttpGet("{assetId}/categories")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> GetAssetCategory(string assetId)
        {
            var res = await _assetsService.TryGetAssetCategoryAsync(assetId);

            if (res.errorResponse != null)
            {
                return NotFound(new ApiResponse(HttpStatusCode.NotFound,
                    $"Error while retrieving asset category for asset {assetId}. {res.errorResponse.ErrorMessages}"));
            }

            return Ok(GetAssetCategoriesResponseModel.Create(new ApiAssetCategoryModel[] {res.ConvertToApiModel()}));
        }

        [HttpGet("extended")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> GetAssetsExtended()
        {
            var res = await _assetsService.GetAssetsExtendedAsync();
            var assetsExtended = res.Assets.Select(s => s.ConvertTpApiModel()).ToList();
            return Ok(AssetExtendedResponseModel.Create(assetsExtended));
        }

        [HttpGet("{assetId}/extended")]
        [ApiExplorerSettings(GroupName = "Exchange")]
        public async Task<IActionResult> GetAssetsExtended(string assetId)
        {
            var res = await _assetsService.GetAssetExtendedByIdAsync(assetId);
            var assetsExtended = res.Assets.Select(s => s.ConvertTpApiModel()).ToList();
            return Ok(AssetExtendedResponseModel.Create(assetsExtended));
        }

        [HttpGet("baseAsset")]
        [ApiExplorerSettings(GroupName = "Settings")]
        public async Task<IActionResult> GetBaseAsset([FromQuery] string clientId)
        {
            BaseAssetClientModel response;
            try
            {
                response = await _clientAccountClient.GetBaseAssetAsync(clientId);
            }
            catch (Exception e)
            {
                await _log.WriteFatalErrorAsync(nameof(AssetsController), nameof(GetBaseAsset), e);
                return BadRequest(new {message = e.Message});
            }

            return Ok(response);
        }

        [HttpPost("baseAsset")]
        [ApiExplorerSettings(GroupName = "Settings")]
        public async Task<IActionResult> SetBaseAsset([FromBody] BaseAssetUpdateModel model)
        {
            try
            {
                await _clientAccountClient.SetBaseAssetAsync(model.ClientId, model.BaseAsssetId);
            }
            catch (Exception e)
            {
                await _log.WriteFatalErrorAsync(nameof(AssetsController), nameof(SetBaseAsset), e);
                return BadRequest(new {message = e.Message});
            }

            return Ok();
        }
    }
}
