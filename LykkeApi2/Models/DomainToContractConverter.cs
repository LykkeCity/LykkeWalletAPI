using Lykke.Service.Assets.Client.Custom;
using LykkeApi2.Models.AssetPairRates;
using LykkeApi2.Models.AssetPairsModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeApi2.Models
{
    public static class DomainToContractConverter
    {
        public static ApiAssetModel ConvertToApiModel(this Lykke.Service.Assets.Client.Custom.IAsset src)
        {
            return new ApiAssetModel
            {
                Id = src.Id,
                Name = src.Name,
                Accuracy = src.Accuracy,
                Symbol = src.Symbol,
                HideWithdraw = src.HideWithdraw,
                HideDeposit = src.HideDeposit,
                KycNeeded = src.KycNeeded,
                BankCardsDepositEnabled = src.BankCardsDepositEnabled,
                SwiftDepositEnabled = src.SwiftDepositEnabled,
                BlockchainDepositEnabled = src.BlockchainDepositEnabled,
                CategoryId = src.CategoryId
            };
        }

        public static ApiAssetCategoryModel ConvertToApiModel(this Lykke.Service.Assets.Client.Custom.IAssetCategory src)
        {
            return new ApiAssetCategoryModel
            {
                Id = src.Id,
                Name = src.Name,
                IosIconUrl = src.IosIconUrl,
                AndroidIconUrl = src.AndroidIconUrl,
                SortOrder = src.SortOrder
            };
        }

        public static AssetAttributesModel ConvertToApiModel(this IAssetAttributes src)
        {
            return new AssetAttributesModel
            {
                Attrbuttes = src.Attributes.Select(aa => new KeyValue { Key = aa.Key, Value = aa.Value }).ToArray()
            };

        }

        public static AssetDescriptionModel ConvertToApiModel(this Lykke.Service.Assets.Client.Custom.IAssetDescription src)
        {
            return new AssetDescriptionModel
            {
                Id = src.Id,
                AssetClass = src.AssetClass,
                Description = src.Description,
                IssuerName = src.IssuerName,
                MarketCapitalization = src.MarketCapitalization,
                NumberOfCoins = src.NumberOfCoins,
                PopIndex = src.PopIndex,
                AssetDescriptionUrl = src.AssetDescriptionUrl,
                FullName = src.FullName
            };

        }

        public static AssetExtended ConvertTpApiModel(this Lykke.Service.Assets.Client.Models.AssetExtended src)
        {
            var asset = src.Asset.ConvertToApiModel();
            var description = src.Description.ConvertToApiModel();
            var attributes = src.Attributes.ConvertToApiModel();
            var category = src.Category.ConvertToApiModel();


            return new AssetExtended
            {
                Asset = asset,
                Description = description,
                Category = category,
                Attributes = attributes.Attrbuttes
            };
        }

        public static AssetPairModel ConvertToApiModel(this Lykke.Service.Assets.Client.Custom.IAssetPair src)
        {
            return new AssetPairModel
            {
                Id = src.Id,
                Accuracy = src.Accuracy,
                BaseAssetId = src.BaseAssetId,
                InvertedAccuracy = src.InvertedAccuracy,
                IsDisabled = src.IsDisabled,
                Name = src.Name,
                QuotingAssetId = src.QuotingAssetId,
                Source = src.Source,
                Source2 = src.Source2,                
            };
        }

        public static AssetPairRateModel ConvertToApiModel(this Lykke.MarketProfileService.Client.Models.AssetPairModel src)
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
