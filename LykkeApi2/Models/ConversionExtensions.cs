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

        //public static ApiBalanceChange ConvertToApiModel(this ICashInOutOperation cashInOutOperation, Asset asset)
        //{
        //    bool isSettled = !string.IsNullOrEmpty(cashInOutOperation.BlockChainHash);

        //    return new ApiBalanceChange
        //    {
        //        DateTime = cashInOutOperation.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
        //        Id = cashInOutOperation.Id,
        //        Amount = cashInOutOperation.Amount.TruncateDecimalPlaces(asset.GetDisplayAccuracy()),
        //        Asset = cashInOutOperation.AssetId,
        //        IconId = asset.GetIcon(),
        //        BlockChainHash = cashInOutOperation.BlockChainHash ?? string.Empty,
        //        IsRefund = cashInOutOperation.IsRefund,
        //        AddressFrom = cashInOutOperation.AddressFrom,
        //        AddressTo = cashInOutOperation.AddressTo,
        //        IsSettled = isSettled,
        //        Type = cashInOutOperation.Type.ToString(),
        //        State = cashInOutOperation.State
        //    };
        //}

        public static ApiCashInHistoryOperation ConvertToCashInApiModel(this ICashInOutOperation operation,
            Asset asset)
        {
            if (operation.Amount < 0) return null;

            var isSettled = operation.IsSettled ?? !string.IsNullOrEmpty(operation.BlockChainHash);

            var amount = operation.Amount.TruncateDecimalPlaces(asset.GetDisplayAccuracy());

            return new ApiCashInHistoryOperation
            {
                DateTime = operation.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
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
                //Todo: remove string
                ContextOperationType = "CashIn"
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
                DateTime = operation.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
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
                //Todo: remove string
                ContextOperationType = "CashOut",
                CashOutState = CashOutState.Regular
            };
        }

        //public static ApiTransfer ConvertToApiModel(this ITransferEvent evnt, Asset asset)
        //{
        //    var isSettled = evnt.IsSettled ?? !string.IsNullOrEmpty(evnt.BlockChainHash);

        //    return new ApiTransfer
        //    {
        //        DateTime = evnt.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
        //        Id = evnt.Id,
        //        Volume = evnt.Amount.TruncateDecimalPlaces(asset.GetDisplayAccuracy()),
        //        Asset = evnt.AssetId,
        //        IconId = asset.GetIcon(),
        //        BlockChainHash = evnt.BlockChainHash ?? string.Empty,
        //        AddressFrom = evnt.AddressFrom,
        //        AddressTo = evnt.AddressTo,
        //        IsSettled = isSettled,
        //        State = evnt.State
        //    };
        //}

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
                // Todo: use constant
                DateTime = operation.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                // Todo: remove string
                Type = "Transfer",
                BlockChainHash = operation.BlockChainHash,
                State = operation.State,
                IsSettled = isSettled,
                Amount = -Math.Abs(amount),
                // Todo: remove string
                ContextOperationType = "Transfer",
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
                // Todo: remove string
                Type = "Transfer",
                Id = operation.Id,
                DateTime = operation.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                BlockChainHash = operation.BlockChainHash,
                State = operation.State,
                IsSettled = isSettled,
                Amount = Math.Abs(amount),
                // Todo: remove string
                ContextOperationType = "Transfer",
                IsRefund = false
            };
        }

        public static ApiBaseCashOperation[] ConvertToApiModel(this ITransferEvent operation, Asset asset)
        {
            return new ApiBaseCashOperation[]
                {operation.ConvertToCashInApiModel(asset), operation.ConvertToCashOutApiModel(asset)};
        }

        //public static ApiCashOutAttempt ConvertToApiModel(this ICashOutRequest request, Asset asset)
        //{
        //    return new ApiCashOutAttempt
        //    {
        //        Id = request.Id,
        //        DateTime = request.DateTime.ToIsoDateTime(),
        //        Volume = request.Amount.TruncateDecimalPlaces(asset.GetDisplayAccuracy()),
        //        Asset = request.AssetId,
        //        IconId = asset.GetIcon()
        //    };
        //}

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
                DateTime = operation.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                //todo: remove string
                ContextOperationType = "CashOutRequest",
                BlockChainHash = operation.BlockchainHash,
                State = operation.State,
                IsSettled = isSettled,
                Amount = operation.Amount.TruncateDecimalPlaces(asset.GetDisplayAccuracy()),
                IsRefund = false,
                AddressTo = null
            };
        }

        //public static ApiLimitTradeEvent ConvertToApiModel(this ILimitTradeEvent limitTradeEvent, AssetPair assetPair,
        //    int accuracy)
        //{
        //    var isBuy = limitTradeEvent.OrderType == OrderType.Buy;

        //    var rate = limitTradeEvent.Price.TruncateDecimalPlaces(assetPair.Accuracy, isBuy);

        //    var converted = (rate * limitTradeEvent.Volume).TruncateDecimalPlaces(accuracy, isBuy);

        //    return new ApiLimitTradeEvent
        //    {
        //        DateTime = limitTradeEvent.CreatedDt,
        //        Id = limitTradeEvent.Id,
        //        Volume = Math.Abs(limitTradeEvent.Volume),
        //        Price = limitTradeEvent.Price,
        //        OrderId = limitTradeEvent.OrderId,
        //        Asset = limitTradeEvent.AssetId,
        //        AssetPair = limitTradeEvent.AssetPair,
        //        Status = limitTradeEvent.Status.ToString(),
        //        Type = limitTradeEvent.OrderType.ToString(),
        //        TotalCost = Math.Abs(converted)
        //    };
        //}

        public static ApiTradeHistoryOperation ConvertToApiModel(this ILimitTradeEvent operation, AssetPair assetPair,
            int accuracy)
        {
            var isBuy = operation.OrderType == OrderType.Buy;

            var rate = operation.Price.TruncateDecimalPlaces(assetPair.Accuracy, isBuy);

            var converted = (rate * operation.Volume).TruncateDecimalPlaces(accuracy, isBuy);

            return new ApiTradeHistoryOperation
            {
                DateTime = operation.CreatedDt.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Id = operation.Id,
                Asset = operation.AssetId,
                MarketOrderId = null,
                LimitOrderId = operation.OrderId,
                Volume = Math.Abs(operation.Volume).TruncateDecimalPlaces(accuracy, isBuy)
            };
            
        }

        //public static ApiTrade ConvertToApiModel(this IClientTrade clientTrade, Asset asset)
        //{
        //    var isSettled = !string.IsNullOrEmpty(clientTrade.BlockChainHash);

        //    return new ApiTrade
        //    {
        //        DateTime = clientTrade.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
        //        Id = clientTrade.Id,
        //        Asset = clientTrade.AssetId,
        //        Volume = clientTrade.Amount.TruncateDecimalPlaces(asset.Accuracy),
        //        IconId = asset.GetIcon(),
        //        BlockChainHash = clientTrade.BlockChainHash ?? string.Empty,
        //        AddressFrom = clientTrade.AddressFrom,
        //        AddressTo = clientTrade.AddressTo,
        //        IsSettled = isSettled,
        //        State = clientTrade.State
        //    };
        //}

        public static ApiTradeHistoryOperation ConvertToApiModel(this IClientTrade operation, Asset asset)
        {
            var isSettled = !string.IsNullOrEmpty(operation.BlockChainHash);

            return new ApiTradeHistoryOperation
            {
                DateTime = operation.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Id = operation.Id,
                Asset = operation.AssetId,
                Volume = operation.Amount.TruncateDecimalPlaces(asset.Accuracy),
                BlockChainHash = operation.BlockChainHash ?? string.Empty,
                AddressFrom = operation.AddressFrom,
                AddressTo = operation.AddressTo,
                IsSettled = isSettled,
                State = operation.State,
                MarketOrderId = operation.MarketOrderId,
                LimitOrderId = operation.LimitOrderId
            };
        }
    }
}
