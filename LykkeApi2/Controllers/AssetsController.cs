using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Lykke.Service.Assets.Client.Custom;
using LykkeApi2.Models;
//using Lykke.Service.Assets.Client.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LykkeApi2.Controllers
{
    [Route("api/[controller]")]
    public class AssetsController : Controller
    {
        private readonly ICachedAssetsService _assetsService;

        public AssetsController(ICachedAssetsService assetsService)
        {
            _assetsService = assetsService;
        }

        [HttpGet]
        public async Task<ResponseModel<GetBaseAssetsRespModel>> Get()
        {
            var assets = (await _assetsService.GetAllAssetsAsync()).Where(x => !x.IsDisabled);
            return ResponseModel<GetBaseAssetsRespModel>.CreateOk(
                GetBaseAssetsRespModel.Create(assets.Select(itm => itm.ConvertToApiModel()).ToArray()));
        }

        [HttpGet("{id}")]
        public async Task<ResponseModel<GetClientBaseAssetRespModel>> Get(string id)
        {
            var asset = await _assetsService.TryGetAssetAsync(id);
            return ResponseModel<GetClientBaseAssetRespModel>.CreateOk(
                GetClientBaseAssetRespModel.Create(asset.ConvertToApiModel()));

        }

        [HttpGet("{assetId}/attributes")]
        public async Task<ResponseModel<AssetAttributesModel>> GetAssetAttributes(string assetId)
        {
            var keyValues = await _assetsService.GetAssetAttributesAsync(assetId);

            return ResponseModel<AssetAttributesModel>.CreateOk(keyValues.ConvertToApiModel());
        }

        [HttpGet("{assetId}/attributes/{key}")]
        public async Task<ResponseModel<IAssetAttributesKeyValue>> GetAssetAttributeByKey(string assetId, string key)
        {
            var keyValues = await _assetsService.GetAssetAttributeByKeyAsync(assetId, key);

            return ResponseModel<IAssetAttributesKeyValue>.CreateOk(keyValues.ConvertToApiModel().Pairs.FirstOrDefault() ?? new KeyValue());
        }

        [HttpPost("description/list")]
        public async Task<ResponseModel<AssetDescriptionsResponseModel>> GetAssetDescriptionsList([FromBody]GetAssetDescriptionsRequestModel request)
        {
            var res = await _assetsService.GetAssetDescriptionsAsync(new Lykke.Service.Assets.Client.Models.GetAssetDescriptionsRequestModel { Ids = request.Ids });

            return
                ResponseModel<AssetDescriptionsResponseModel>.CreateOk(res.ConvertToApiModel());
        }
    }
}
