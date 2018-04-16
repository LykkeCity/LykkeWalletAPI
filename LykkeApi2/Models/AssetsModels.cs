using Lykke.Service.Assets.Client.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LykkeApi2.Models
{
    public class GetAssetCategoriesResponseModel
    {
        public AssetCategoryModel[] AssetCategories { get; set; }

        public static GetAssetCategoriesResponseModel Create(AssetCategoryModel[] assetCategories)
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
        public IEnumerable<AssetExtendedModel> Assets { get; set; }

        public static AssetExtendedResponseModel Create(IEnumerable<AssetExtendedModel> assets)
        {
            return new AssetExtendedResponseModel
            {
                Assets = assets
            };
        }
    }

    public class AssetExtendedModel
    {
        public ApiAssetModel Asset { get; set; }
        public AssetDescriptionModel Description { get; set; }
        public AssetCategoryModel Category { get; set; }
        public IEnumerable<IAssetAttributesKeyValue> Attributes { get; set; }

        public static AssetExtendedModel Create(ApiAssetModel asset, AssetDescriptionModel description, AssetCategoryModel category, List<IAssetAttributesKeyValue> attributes)
        {
            return new AssetExtendedModel
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

    public class AssetDescriptionsModel
    {
        public IEnumerable<AssetDescriptionModel> Descriptions { get; set; }
        
        public static AssetDescriptionsModel Create(IEnumerable<AssetDescriptionModel> descriptions)
        {
            return new AssetDescriptionsModel
            {
                Descriptions = descriptions
            };
        }
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

    public static class AssetExtensions
    {
        public static int GetDisplayAccuracy(this Asset asset)
        {
            return asset.DisplayAccuracy ?? asset.Accuracy;
        }
    }

    public class AssetIdsResponse
    {
        public IEnumerable<string> AssetIds { get; set; }

        public static AssetIdsResponse Create(IEnumerable<string> assetIds)
        {
            return new AssetIdsResponse()
            {
                AssetIds = assetIds
            };
        }
    }
}
