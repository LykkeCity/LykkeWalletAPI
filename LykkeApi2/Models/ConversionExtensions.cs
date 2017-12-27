using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Lykke.Service.Assets.Client.Models;
using Lykke.Service.OperationsRepository.Contract;
using Lykke.Service.OperationsRepository.Contract.Abstractions;
using LykkeApi2.Models.AssetPairRates;
using LykkeApi2.Models.AssetPairsModels;
using LykkeApi2.Models.History;

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

        public static ApiCashInHistoryOperation ConvertToCashInApiModel(this ICashInOutOperation operation,
            Asset asset)
        {
            if (operation.Amount < 0) return null;

            var isSettled = operation.IsSettled ?? !string.IsNullOrEmpty(operation.BlockChainHash);

            var amount = operation.Amount.TruncateDecimalPlaces(asset.GetDisplayAccuracy());

            return new ApiCashInHistoryOperation
            {
                DateTime = operation.DateTime.ToString(DateTimeFormat),
                Id = operation.Id,
                Amount = Math.Abs(amount),
                Asset = operation.AssetId,
                BlockChainHash = operation.BlockChainHash ?? string.Empty,
                IsRefund = operation.IsRefund,
                AddressFrom = operation.AddressFrom,
                AddressTo = operation.AddressTo,
                IsSettled = isSettled,
                Type = operation.Type.ToString(),
                State = operation.State,
                ContextOperationType = nameof(OperationType.CashInOut)
            };
        }

        public static ApiCashOutHistoryOperation ConvertToCashOutApiModel(this ICashInOutOperation operation,
            Asset asset)
        {
            if (operation.Amount >= 0) return null;

            var isSettled = operation.IsSettled ?? !string.IsNullOrEmpty(operation.BlockChainHash);

            var amount = operation.Amount.TruncateDecimalPlaces(asset.GetDisplayAccuracy());

            return new ApiCashOutHistoryOperation
            {
                DateTime = operation.DateTime.ToString(DateTimeFormat),
                Id = operation.Id,
                Amount = -Math.Abs(amount),
                Asset = operation.AssetId,
                BlockChainHash = operation.BlockChainHash ?? string.Empty,
                IsRefund = operation.IsRefund,
                AddressFrom = operation.AddressFrom,
                AddressTo = operation.AddressTo,
                IsSettled = isSettled,
                Type = operation.Type.ToString(),
                State = operation.State,
                ContextOperationType = nameof(OperationType.CashInOut),
                CashOutState = CashOutState.Regular
            };
        }

        public static ApiCashOutHistoryOperation ConvertToCashOutApiModel(this ITransferEvent operation, Asset asset)
        {
            var isSettled = operation.IsSettled ?? !string.IsNullOrEmpty(operation.BlockChainHash);

            var amount = operation.Amount.TruncateDecimalPlaces(asset.GetDisplayAccuracy());

            return new ApiCashOutHistoryOperation
            {
                Asset = operation.AssetId,
                AddressFrom = operation.AddressFrom,
                AddressTo = operation.AddressTo,
                Id = operation.Id,
                DateTime = operation.DateTime.ToString(DateTimeFormat),
                Type = string.Empty,
                BlockChainHash = operation.BlockChainHash ?? string.Empty,
                State = operation.State,
                IsSettled = isSettled,
                Amount = -Math.Abs(amount),
                ContextOperationType = nameof(OperationType.TransferEvent),
                IsRefund = false,
                CashOutState = CashOutState.Regular
            };
        }

        public static ApiCashInHistoryOperation ConvertToCashInApiModel(this ITransferEvent operation, Asset asset)
        {
            var isSettled = operation.IsSettled ?? !string.IsNullOrEmpty(operation.BlockChainHash);

            var amount = operation.Amount.TruncateDecimalPlaces(asset.GetDisplayAccuracy());

            return new ApiCashInHistoryOperation
            {
                Asset = operation.AssetId,
                AddressFrom = operation.AddressFrom,
                AddressTo = operation.AddressTo,
                Type = string.Empty,
                Id = operation.Id,
                DateTime = operation.DateTime.ToString(DateTimeFormat),
                BlockChainHash = operation.BlockChainHash ?? string.Empty,
                State = operation.State,
                IsSettled = isSettled,
                Amount = Math.Abs(amount),
                ContextOperationType = nameof(OperationType.TransferEvent),
                IsRefund = false
            };
        }

        public static ApiCashOutHistoryOperation ConvertToApiModel(this ICashOutRequest operation, Asset asset)
        {
            var isSettled = !string.IsNullOrEmpty(operation.BlockchainHash);

            return new ApiCashOutHistoryOperation
            {
                Asset = operation.AssetId,
                AddressFrom = null,
                Type = nameof(CashOperationType.None),
                CashOutState = CashOutState.Request,
                Id = operation.Id,
                DateTime = operation.DateTime.ToString(DateTimeFormat),
                ContextOperationType = nameof(OperationType.CashOutAttempt),
                BlockChainHash = operation.BlockchainHash ?? string.Empty,
                State = operation.State,
                IsSettled = isSettled,
                Amount = operation.Amount.TruncateDecimalPlaces(asset.GetDisplayAccuracy()),
                IsRefund = false,
                AddressTo = null
            };
        }

        public static ApiTradeHistoryOperation ConvertToApiModel(this ILimitTradeEvent operation, AssetPair assetPair,
            int accuracy)
        {
            var isBuy = operation.OrderType == OrderType.Buy;

            return new ApiTradeHistoryOperation
            {
                DateTime = operation.CreatedDt.ToString(DateTimeFormat),
                Id = operation.Id,
                Asset = operation.AssetId,
                MarketOrderId = null,
                LimitOrderId = operation.OrderId,
                Volume = Math.Abs(operation.Volume).TruncateDecimalPlaces(accuracy, isBuy),
                ContextOperationType = nameof(OperationType.LimitTradeEvent),
                State = string.Empty,
                IsSettled = false
            };
            
        }

        public static ApiTradeHistoryOperation ConvertToApiModel(this IClientTrade operation, Asset asset)
        {
            var isSettled = !string.IsNullOrEmpty(operation.BlockChainHash);

            return new ApiTradeHistoryOperation
            {
                DateTime = operation.DateTime.ToString(DateTimeFormat),
                Id = operation.Id,
                Asset = operation.AssetId,
                Volume = operation.Amount.TruncateDecimalPlaces(asset.Accuracy),
                IsSettled = isSettled,
                State = operation.State.ToString(),
                MarketOrderId = operation.MarketOrderId,
                LimitOrderId = operation.LimitOrderId,
                ContextOperationType = nameof(OperationType.ClientTrade)
            };
        }
    }
}
