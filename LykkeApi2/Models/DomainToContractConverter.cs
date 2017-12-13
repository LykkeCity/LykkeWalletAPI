using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.OperationsRepository.Contract;
using Lykke.Service.OperationsRepository.Contract.Abstractions;
using LykkeApi2.Models.AssetPairRates;
using LykkeApi2.Models.AssetPairsModels;
using LykkeApi2.Models.Operations;

namespace LykkeApi2.Models
{
    public static class DomainToContractConverter
    {
        private static string GetIcon(this Asset asset)
        {
            return asset.IdIssuer;
        }

        public static ApiAssetModel ConvertToApiModel(this Asset src)
        {
            return new ApiAssetModel
            {
                Id = src.Id,
                Name = src.Name,
                DisplayId = src.DisplayId,
                Accuracy = src.Accuracy,
                Symbol = src.Symbol,
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

        public static AssetExtended ConvertTpApiModel(this AssetExtendedInfo src)
        {
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

        public static ApiBalanceChange ConvertToApiModel(this ICashInOutOperation cashInOutOperation, Asset asset)
        {
            bool isSettled = !string.IsNullOrEmpty(cashInOutOperation.BlockChainHash);

            return new ApiBalanceChange
            {
                DateTime = cashInOutOperation.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Id = cashInOutOperation.Id,
                Amount = cashInOutOperation.Amount.TruncateDecimalPlaces(asset.GetDisplayAccuracy()),
                Asset = cashInOutOperation.AssetId,
                IconId = asset.GetIcon(),
                BlockChainHash = cashInOutOperation.BlockChainHash ?? string.Empty,
                IsRefund = cashInOutOperation.IsRefund,
                AddressFrom = cashInOutOperation.AddressFrom,
                AddressTo = cashInOutOperation.AddressTo,
                IsSettled = isSettled,
                Type = cashInOutOperation.Type.ToString(),
                State = cashInOutOperation.State
            };
        }

        public static ApiTransfer ConvertToApiModel(this ITransferEvent evnt, Asset asset)
        {
            var isSettled = evnt.IsSettled ?? !string.IsNullOrEmpty(evnt.BlockChainHash);

            return new ApiTransfer
            {
                DateTime = evnt.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Id = evnt.Id,
                Volume = evnt.Amount.TruncateDecimalPlaces(asset.GetDisplayAccuracy()),
                Asset = evnt.AssetId,
                IconId = asset.GetIcon(),
                BlockChainHash = evnt.BlockChainHash ?? string.Empty,
                AddressFrom = evnt.AddressFrom,
                AddressTo = evnt.AddressTo,
                IsSettled = isSettled,
                State = evnt.State
            };
        }

        public static ApiCashOutAttempt ConvertToApiModel(this ICashOutRequest request, Asset asset)
        {
            return new ApiCashOutAttempt
            {
                Id = request.Id,
                DateTime = request.DateTime.ToIsoDateTime(),
                Volume = request.Amount.TruncateDecimalPlaces(asset.GetDisplayAccuracy()),
                Asset = request.AssetId,
                IconId = asset.GetIcon()
            };
        }

        public static ApiLimitTradeEvent ConvertToApiModel(this ILimitTradeEvent limitTradeEvent, AssetPair assetPair,
            int accuracy)
        {
            var isBuy = limitTradeEvent.OrderType == OrderType.Buy;

            var rate = limitTradeEvent.Price.TruncateDecimalPlaces(assetPair.Accuracy, isBuy);

            var converted = (rate * limitTradeEvent.Volume).TruncateDecimalPlaces(accuracy, isBuy);

            return new ApiLimitTradeEvent
            {
                DateTime = limitTradeEvent.CreatedDt,
                Id = limitTradeEvent.Id,
                Volume = Math.Abs(limitTradeEvent.Volume),
                Price = limitTradeEvent.Price,
                OrderId = limitTradeEvent.OrderId,
                Asset = limitTradeEvent.AssetId,
                AssetPair = limitTradeEvent.AssetPair,
                Status = limitTradeEvent.Status.ToString(),
                Type = limitTradeEvent.OrderType.ToString(),
                TotalCost = Math.Abs(converted)
            };
        }

        public static ApiTrade ConvertToApiModel(this IClientTrade clientTrade, Asset asset, AssetPair assetPair)
        {
            var isSettled = !string.IsNullOrEmpty(clientTrade.BlockChainHash);

            return new ApiTrade
            {
                DateTime = clientTrade.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Id = clientTrade.Id,
                Asset = clientTrade.AssetId,
                Volume = clientTrade.Amount.TruncateDecimalPlaces(asset.Accuracy),
                IconId = asset.GetIcon(),
                BlockChainHash = clientTrade.BlockChainHash ?? string.Empty,
                AddressFrom = clientTrade.AddressFrom,
                AddressTo = clientTrade.AddressTo,
                IsSettled = isSettled,
                State = clientTrade.State
            };
        }
    }
}
