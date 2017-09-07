using Lykke.Service.Assets.Client.Custom;
using Lykke.Service.Assets.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeApi2.Models
{
    public class ApiAssetModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Accuracy { get; set; }
        public string Symbol { get; set; }
        public bool HideWithdraw { get; set; }
        public bool HideDeposit { get; set; }
        public bool KycNeeded { get; set; }
        public bool BankCardsDepositEnabled { get; set; }
        public bool SwiftDepositEnabled { get; set; }
        public bool BlockchainDepositEnabled { get; set; }
        public string CategoryId { get; set; }
    }

    public static class DomainToContractConverter
    {
        public static ApiAssetModel ConvertToApiModel(this IAsset src)
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

        public static AssetAttributesModel ConvertToApiModel(this IAssetAttributes src)
        {
            return new AssetAttributesModel
            {
                Pairs = src.Attributes.Select(aa => new KeyValue { Key = aa.Key, Value = aa.Value }).ToArray()
            };

        }

        public static AssetDescriptionsResponseModel ConvertToApiModel(this IAssetExtendedInfo src)
        {
            return new AssetDescriptionsResponseModel
            {
                Descriptions = src.Descriptions.Select(d=> AssetDescriptionModel.Create(d)).ToList()
            };

        }
    }

    public class AssetDescriptionModel
    {
        public string Id { get; set; }
        public string AssetClass { get; set; }
        public int PopIndex { get; set; }
        public string Description { get; set; }
        public string IssuerName { get; set; }
        public string NumberOfCoins { get; set; }
        public string MarketCapitalization { get; set; }
        public string AssetDescriptionUrl { get; set; }
        public string FullName { get; set; }


        public static AssetDescriptionModel Create(AssetExtendedInfo src)
        {
            return new AssetDescriptionModel
            {
                Id = src.Id,
                AssetClass = src.AssetClass,
                Description = src.Description,
                IssuerName = src.IssuerName,
                MarketCapitalization = src.MarketCapitalization,
                NumberOfCoins = src.NumberOfCoins,
                PopIndex = src.PopIndex ?? 0,
                AssetDescriptionUrl = src.AssetDescriptionUrl,
                FullName = src.FullName
            };
        }

    }

}
