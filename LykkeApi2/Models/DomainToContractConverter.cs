using System.Collections.Generic;
using System.Linq;
using Lykke.Service.Assets.Client.Models;
using LykkeApi2.Models.AssetPairRates;
using LykkeApi2.Models.AssetPairsModels;

namespace LykkeApi2.Models
{
    public static class DomainToContractConverter
    {
        public static ApiAssetModel ConvertToApiModel(this Asset src)
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

        public static ApiAssetCategoryModel ConvertToApiModel(this AssetCategory src)
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

        public static AssetAttributesModel ConvertToApiModel(this AssetAttributes src)
        {
            return new AssetAttributesModel
            {
                Attrbuttes = src.Attributes.Select(ConvertToApiModel).ToArray()
            };

        }

        public static IAssetAttributesKeyValue ConvertToApiModel(this AssetAttribute src)
        {
            return new KeyValue { Key = src.Key, Value = src.Value };

        }

        //public static AssetDescriptionModel ConvertToApiModel(this AssetDescription src)
        //{
        //    return new AssetDescriptionModel
        //    {
        //        Id = src.Id,
        //        AssetClass = src.AssetClass,
        //        Description = src.Description,
        //        IssuerName = src.IssuerName,
        //        MarketCapitalization = src.MarketCapitalization,
        //        NumberOfCoins = src.NumberOfCoins,
        //        PopIndex = src.PopIndex,
        //        AssetDescriptionUrl = src.AssetDescriptionUrl,
        //        FullName = src.FullName
        //    };

        //}

        public static AssetExtended ConvertTpApiModel(this AssetExtendedInfo src)
        {
            //var asset = src.Asset.ConvertToApiModel();
            //var description = src.Description.ConvertToApiModel();
            //var attributes = src.Attributes.ConvertToApiModel();
            //var category = src.Category.ConvertToApiModel();
            var asset = new ApiAssetModel { Id = src.Id, Name = src.FullName };
            var description = new AssetDescriptionModel { Id = src.Id, Description = src.Description, AssetClass = src.AssetClass, FullName = src.FullName };
            var category = new ApiAssetCategoryModel();
            var attributes = new List<IAssetAttributesKeyValue>();

            return new AssetExtended
            {
                Asset = asset,
                Description = description,
                Category = category,
                Attributes = attributes
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
