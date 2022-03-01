using System.Linq;
using Lykke.Job.MarketProfile.Contract;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using LykkeApi2.Models.AssetPairRates;
using LykkeApi2.Models.AssetPairsModels;

namespace LykkeApi2.Models
{
    public static class ConversionExtensions
    {
        public static AssetModel ToApiModel(this Asset src)
        {
            return new AssetModel
            {
                Id = src.Id,
                Name = src.Name,
                DisplayId = src.DisplayId,
                Accuracy = src.DisplayAccuracy ?? src.Accuracy,
                KycNeeded = src.KycNeeded,
                BankCardsDepositEnabled = src.BankCardsDepositEnabled,
                SwiftDepositEnabled = src.SwiftDepositEnabled,
                BlockchainDepositEnabled = src.BlockchainDepositEnabled,
                SiriusBlockchainId = src.SiriusBlockchainId,
                BlockchainNetworkName = src.BlockchainNetworkName,
                CategoryId = src.CategoryId,
                CanBeBase = src.IsBase,
                IsBase = src.IsBase,
                IconUrl = src.IconUrl
            };
        }

        public static AssetCategoryModel ToApiModel(this AssetCategory src)
        {
            return new AssetCategoryModel
            {
                Id = src.Id,
                Name = src.Name,
                SortOrder = src.SortOrder
            };
        }

        public static AssetAttributesModel ToApiModel(this AssetAttributes src)
        {
            return new AssetAttributesModel
            {
                Attrbuttes = src?.Attributes?.Select(ToApiModel).OrderBy(x => x.Key).ToArray() ?? new KeyValue[0]
            };
        }

        public static IAssetAttributesKeyValue ToApiModel(this AssetAttribute src)
        {
            return new KeyValue {Key = src.Key, Value = src.Value};
        }

        public static AssetDescriptionModel ToApiModel(this AssetExtendedInfo extendedInfo)
        {
            return new AssetDescriptionModel
            {
                Id = extendedInfo.Id,
                AssetClass = extendedInfo.AssetClass,
                Description = extendedInfo.Description,
                IssuerName = null,
                NumberOfCoins = extendedInfo.NumberOfCoins,
                AssetDescriptionUrl = extendedInfo.AssetDescriptionUrl,
                FullName = extendedInfo.FullName
            };
        }

        public static AssetPairModel ToApiModel(this AssetPair src)
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

        public static AssetPairRateModel ToApiModel(
            this IAssetPair src)
        {
            return new AssetPairRateModel
            {
                AskPrice = src.AskPrice,
                AskPriceTimestamp = src.AskPriceTimestamp,
                AssetPair = src.AssetPair,
                BidPrice = src.BidPrice,
                BidPriceTimestamp = src.BidPriceTimestamp,
            };
        }

        public static KycStatus ToApiModel(this KycStatus status)
        {
            switch (status)
            {
                case KycStatus.NeedToFillData:
                    return KycStatus.NeedToFillData;
                case KycStatus.Ok:
                case KycStatus.ReviewDone:
                    return KycStatus.Ok;
                case KycStatus.RestrictedArea:
                    return KycStatus.RestrictedArea;
                default:
                    return KycStatus.Pending;
            }
        }

        public static AssetWhitelistModel ToModel(this Asset src)
        {
            return new AssetWhitelistModel
            {
                Id = src.Id,
                Name = src.Name,
                DisplayId = src.DisplayId,
                Accuracy = src.DisplayAccuracy ?? src.Accuracy,
                KycNeeded = src.KycNeeded,
                BankCardsDepositEnabled = src.BankCardsDepositEnabled,
                SwiftDepositEnabled = src.SwiftDepositEnabled,
                BlockchainDepositEnabled = src.BlockchainDepositEnabled,
                SiriusBlockchainId = src.SiriusBlockchainId,
                CategoryId = src.CategoryId,
                CanBeBase = src.IsBase,
                IsBase = src.IsBase,
                IconUrl = src.IconUrl
            };
        }

    }
}
