using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeApi2.Models
{
    public class GetAssetCategoriesResponseModel
    {
        public ApiAssetCategoryModel[] AssetCategories { get; set; }

        public static GetAssetCategoriesResponseModel Create(ApiAssetCategoryModel[] assetCategories)
        {
            return new GetAssetCategoriesResponseModel
            {
                AssetCategories = assetCategories
            };
        }
    }


    public class GetBaseAssetsRespModel
    {
        public ApiAssetModel[] Assets { get; set; }

        public static GetBaseAssetsRespModel Create(ApiAssetModel[] assets)
        {
            return new GetBaseAssetsRespModel
            {
                Assets = assets
            };
        }
    }

    public class GetClientBaseAssetRespModel
    {
        public ApiAssetModel Asset { get; set; }

        public static GetClientBaseAssetRespModel Create(ApiAssetModel asset)
        {
            return new GetClientBaseAssetRespModel
            {
                Asset = asset
            };
        }
    }

    public class AssetExtendedResponseModel
    {
        public IEnumerable<AssetExtended> Assets { get; set; }

        public static AssetExtendedResponseModel Create(IEnumerable<AssetExtended> assets)
        {
            return new AssetExtendedResponseModel
            {
                Assets = assets
            };
        }
    }

    public class AssetExtended
    {
        public ApiAssetModel Asset { get; set; }
        public AssetDescriptionModel Description { get; set; }
        public ApiAssetCategoryModel Category { get; set; }
        public IEnumerable<IAssetAttributesKeyValue> Attributes { get; set; }

        public static AssetExtended Create(ApiAssetModel asset, AssetDescriptionModel description, ApiAssetCategoryModel category, IEnumerable<IAssetAttributesKeyValue> attributes)
        {
            return new AssetExtended
            {
                Asset = asset,
                Description = description,
                Category = category,
                Attributes = attributes
            };
        }
    }


    public class AssetAttributesModel
    {
        public IAssetAttributesKeyValue[] Attrbuttes { get; set; }
    }

    public interface IAssetAttributesKeyValue
    {
        string Key { get; set; }
        string Value { get; set; }
    }

    public class KeyValue : IAssetAttributesKeyValue
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class AssetDescriptionsResponseModel
    {
        public IEnumerable<AssetDescriptionModel> Descriptions { get; set; }
        public static AssetDescriptionsResponseModel Create(IEnumerable<AssetDescriptionModel> descriptions)
        {
            return new AssetDescriptionsResponseModel
            {
                Descriptions = descriptions
            };
        }
    }

    public class GetAssetDescriptionsRequestModel
    {
        public string[] Ids { get; set; }
    }

    public class BaseAssetModel
    {
        public string BaseAssetId { get; set; }
    }

    public class BaseAssetUpdateModel
    {
        [Required]
        public string BaseAsssetId { get; set; }
    }
}
