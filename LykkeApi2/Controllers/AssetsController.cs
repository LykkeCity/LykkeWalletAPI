using Lykke.Service.Assets.Client.Custom;
using LykkeApi2.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

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

        [HttpPost("description")]
        public async Task<ResponseModel<AssetDescriptionsResponseModel>> GetAssetDescriptions([FromBody]GetAssetDescriptionsRequestModel request)
        {
            var res = await _assetsService.GetAssetDescriptionsAsync(new Lykke.Service.Assets.Client.Models.GetAssetDescriptionsRequestModel { Ids = request.Ids });

            return
                ResponseModel<AssetDescriptionsResponseModel>.CreateOk(res.ConvertToApiModel());
        }

        [HttpGet("{assetId}/description")]
        public async Task<ResponseModel<AssetDescriptionsResponseModel>> GetAssetDescription(string assetId)
        {
            var res = await _assetsService.GetAssetDescriptionsAsync(new Lykke.Service.Assets.Client.Models.GetAssetDescriptionsRequestModel { Ids = new List<string> { assetId } });

            return ResponseModel<AssetDescriptionsResponseModel>.CreateOk(res.ConvertToApiModel());
        }

        [HttpGet("categories")]
        public async Task<ResponseModel<GetAssetCategoriesResponseModel>> GetAssetCategories()
        {
            var res = await _assetsService.GetAssetCategoriesAsync();

            return ResponseModel<GetAssetCategoriesResponseModel>.CreateOk(
               GetAssetCategoriesResponseModel.Create(res.Select(itm => itm.ConvertToApiModel()).ToArray()));
        }

        [HttpGet("{assetId}/categories")]
        public async Task<ResponseModel<GetAssetCategoriesResponseModel>> GetAssetCategory(string assetId)
        {
            var res = await _assetsService.TryGetAssetCategoryAsync(assetId);

            if (res.errorResponse != null)
            {
                return ResponseModel<GetAssetCategoriesResponseModel>.CreateNotFound(ResponseModel.ErrorCodeType.AssetAttributeNotFound, ResponseModel.ErrorCodeType.AssetAttributeNotFound.ToString());
            }

            return ResponseModel<GetAssetCategoriesResponseModel>.CreateOk(
               GetAssetCategoriesResponseModel.Create(new ApiAssetCategoryModel[] { res.ConvertToApiModel() }));
        }

    }
}
