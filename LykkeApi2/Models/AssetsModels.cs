using System;
using Lykke.Service.Assets.Client.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LykkeApi2.Models
{
    public class AssetCategoriesModel
    {
        public AssetCategoryModel[] AssetCategories { get; set; }

        public static AssetCategoriesModel Create(AssetCategoryModel[] assetCategories)
        {
            return new AssetCategoriesModel
            {
                AssetCategories = assetCategories
            };
        }
    }

    public class AssetsModel
    {
        public AssetModel[] Assets { get; set; }

        public static AssetsModel Create(AssetModel[] assets)
        {
            return new AssetsModel
            {
                Assets = assets
            };
        }
    }

    public class AssetRespModel
    {
        public AssetModel Asset { get; set; }

        public static AssetRespModel Create(AssetModel asset)
        {
            return new AssetRespModel
            {
                Asset = asset
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
        [Obsolete]
        public string BaseAsssetId { get; set; }
        public string BaseAssetId { get; set; }
    }

    public class AssetIdsModel
    {
        public IEnumerable<string> AssetIds { get; set; }

        public static AssetIdsModel Create(IEnumerable<string> assetIds)
        {
            return new AssetIdsModel()
            {
                AssetIds = assetIds
            };
        }
    }

    public class AssetMinOrderAmountModel
    {
        public string AssetId { get; set; }
        public string AssetDisplayId { get; set; }
        public double MinOrderAmount { get; set; }
    }
}
