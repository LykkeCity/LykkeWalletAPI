using System;
using System.Linq;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.Kyc.Abstractions.Domain.Verification;
using LykkeApi2.Models.AssetPairRates;
using LykkeApi2.Models.AssetPairsModels;
using LykkeApi2.Models.ClientAccountModels;

namespace LykkeApi2.Models
{
    public static class ConversionExtensions
    {
        private static string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

        public static AssetModel ToApiModel(this Asset src)
        {
            return new AssetModel
            {
                Id = src.Id,
                Name = src.Name,
                DisplayId = src.DisplayId,
                Accuracy = src.Accuracy,
                KycNeeded = src.KycNeeded,
                BankCardsDepositEnabled = src.BankCardsDepositEnabled,
                SwiftDepositEnabled = src.SwiftDepositEnabled,
                BlockchainDepositEnabled = src.BlockchainDepositEnabled,
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

        public static ApiKycStatus ToApiModel(this KycStatus kycStatus)
        {
            switch (kycStatus)
            {
                case KycStatus.NeedToFillData:
                    return ApiKycStatus.NeedToFillData;
                case KycStatus.Pending:
                case KycStatus.Complicated:
                case KycStatus.ReviewDone:
                case KycStatus.JumioOk:
                case KycStatus.JumioInProgress:
                case KycStatus.JumioFailed:
                    return ApiKycStatus.Pending;
                case KycStatus.Ok:
                    return ApiKycStatus.Ok;
                case KycStatus.Rejected:
                    return ApiKycStatus.Rejected;
                case KycStatus.RestrictedArea:
                    return ApiKycStatus.RestrictedArea;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kycStatus), kycStatus, null);
            }
        }

    }
}