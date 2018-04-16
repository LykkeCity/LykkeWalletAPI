using System.Collections.Generic;
using System.Linq;
using Lykke.Service.Assets.Client.Models;
using LykkeApi2.Models.AssetPairRates;
using LykkeApi2.Models.AssetPairsModels;

namespace LykkeApi2.Models
{
    public static class ConversionExtensions
    {
        private static string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

        public static ApiAssetModel ConvertToApiModel(this Asset src)
        {
            return new ApiAssetModel
            {
                Id = src.Id,
                Name = src.Name,
                DisplayId = src.DisplayId,
                Accuracy = src.Accuracy,
                HideWithdraw = src.HideWithdraw,
                HideDeposit = src.HideDeposit,
                KycNeeded = src.KycNeeded,
                BankCardsDepositEnabled = src.BankCardsDepositEnabled,
                SwiftDepositEnabled = src.SwiftDepositEnabled,
                BlockchainDepositEnabled = src.BlockchainDepositEnabled,
                CategoryId = src.CategoryId,
                IsBase = src.IsBase,
                IconUrl = src.IconUrl
            };
        }

        public static AssetCategoryModel ConvertToApiModel(this AssetCategory src)
        {
            return new AssetCategoryModel
            {
                Id = src.Id,
                Name = src.Name,
                IosIconUrl = src.IosIconUrl,
                AndroidIconUrl = src.AndroidIconUrl,
                SortOrder = src.SortOrder
            };
        }

        public static AssetAttributesModel ConvertToApiModel(this AssetAttributes src)
        {
            return new AssetAttributesModel
            {
                Attrbuttes =
                    src != null
                        ? (src.Attributes?.Select(ConvertToApiModel).ToArray() ?? new KeyValue[0])
                        : new KeyValue[0]
            };

        }

        public static IAssetAttributesKeyValue ConvertToApiModel(this AssetAttribute src)
        {
            return new KeyValue {Key = src.Key, Value = src.Value};

        }

        public static AssetExtendedModel CreateAssetExtended(
            Asset asset,
            AssetExtendedInfo assetExtendedInfo,
            AssetCategory assetCategory,
            AssetAttributes attributesKeyValues)
        {
            return new AssetExtendedModel
            {
                Asset = asset?.ConvertToApiModel(),
                Description = assetExtendedInfo?.ConvertToApiModel(),
                Category = assetCategory?.ConvertToApiModel(),
                Attributes =
                    attributesKeyValues?.Attributes.Select(x => new KeyValue { Key = x.Key, Value = x.Value}) ?? new List<KeyValue>()
            };
        }
        
        public static AssetDescriptionModel ConvertToApiModel(this AssetExtendedInfo extendedInfo)
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

        public static AssetPairModel ConvertToApiModel(this AssetPair src)
        {
            return new AssetPairModel
            {
                Id = src.Id,
                Accuracy = src.Accuracy,
                BaseAssetId = src.BaseAssetId,
                InvertedAccuracy = src.InvertedAccuracy,
                Name = src.Name,
                QuotingAssetId = src.QuotingAssetId
            };
        }

        public static AssetPairRateModel ConvertToApiModel(
            this Lykke.MarketProfileService.Client.Models.AssetPairModel src)
        {
            return new AssetPairRateModel
            {
                AskPrice = src.AskPrice,
                AskPriceTimestamp = src.AskPriceTimestamp,
                AssetPair = src.AssetPair,
                BidPrice = src.BidPrice,
                BidPriceTimestamp = src.BidPriceTimestamp
            };
        }

    }
}