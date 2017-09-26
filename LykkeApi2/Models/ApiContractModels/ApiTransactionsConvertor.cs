using Common;
using Core.CashOperations;
using Core.Enumerators;
using Core.Exchange;
using Lykke.Service.Assets.Client.Custom;
using System;
using System.Collections.Generic;

namespace LykkeApi2.Models.ApiContractModels
{
    public static class ApiTransactionsConvertor
    {
        public static ApiBalanceChangeModel ConvertToApiModel(this ICashInOutOperation cashInOutOperation, IAsset asset)
        {
            bool isSettled = !string.IsNullOrEmpty(cashInOutOperation.BlockChainHash); //ToDo: cashInOutOperation.IsSettled
            return new ApiBalanceChangeModel
            {
                DateTime = cashInOutOperation.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Id = cashInOutOperation.Id,
                Amount = cashInOutOperation.Amount.TruncateDecimalPlaces(asset.Accuracy),
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

        private static string GetIcon(this IAsset asset)
        {
            return asset.IdIssuer;
        }

        public static ApiTransfer ConvertToApiModel(this ITransferEvent evnt, IAsset asset)
        {
            var isSettled = evnt.IsSettled ?? !string.IsNullOrEmpty(evnt.BlockChainHash);
            return new ApiTransfer
            {
                DateTime = evnt.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Id = evnt.Id,
                Volume = evnt.Amount.TruncateDecimalPlaces(asset.Accuracy),
                Asset = evnt.AssetId,
                IconId = asset.GetIcon(),
                BlockChainHash = evnt.BlockChainHash ?? string.Empty,
                AddressFrom = evnt.AddressFrom,
                AddressTo = evnt.AddressTo,
                IsSettled = isSettled,
                State = evnt.State
            };
        }

        public static ApiCashOutAttempt ConvertToApiModel(this ICashOutRequest request, IAsset asset)
        {
            return new ApiCashOutAttempt
            {
                DateTime = request.DateTime.ToIsoDateTime(),
                Id = request.Id,
                Volume = request.Amount.TruncateDecimalPlaces(asset.Accuracy),
                Asset = request.AssetId,
                IconId = asset.GetIcon()
            };
        }

        public static ApiLimitTradeEvent ConvertToApiModel(this ILimitTradeEvent limitTradeEvent, IAssetPair assetPair, int accuracy)
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

        public static ApiTradeOperation ConvertToApiModel(this IClientTrade clientTrade, IAsset asset,
           IMarketOrder marketOrder, IAssetPair assetPair)
        {
            var isSettled = !string.IsNullOrEmpty(clientTrade.BlockChainHash); //ToDo: clientTrade.IsSettled
            return new ApiTradeOperation
            {
                DateTime = clientTrade.DateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Id = clientTrade.Id,
                Asset = clientTrade.AssetId,
                Volume = clientTrade.Amount.TruncateDecimalPlaces(asset.Accuracy),
                IconId = asset.GetIcon(),
                BlockChainHash = clientTrade.BlockChainHash ?? string.Empty,
                AddressFrom = clientTrade.AddressFrom,
                AddressTo = clientTrade.AddressTo,
                MarketOrder = marketOrder?.ConvertToApiModel(assetPair, asset.Accuracy),
                IsSettled = isSettled,
                State = clientTrade.State,
                IsLimitTrade = clientTrade.IsLimitOrderResult
            };
        }

        public static ApiMarketOrder ConvertToApiModel(this IMarketOrder marketOrder, IAssetPair assetPair, int accuracy)
        {
            var rate =
                (!marketOrder.Straight ? 1 / marketOrder.Price : marketOrder.Price).TruncateDecimalPlaces(
                    marketOrder.Straight ? assetPair.Accuracy : assetPair.InvertedAccuracy, marketOrder.OrderAction() == OrderAction.Buy);

            double converted = (rate * marketOrder.Volume).TruncateDecimalPlaces(accuracy, marketOrder.OrderAction() == OrderAction.Buy);

            var totalCost = marketOrder.OrderAction() == OrderAction.Sell ? marketOrder.Volume : converted;
            var volume = marketOrder.OrderAction() == OrderAction.Sell ? converted : marketOrder.Volume;

            return new ApiMarketOrder
            {
                Id = marketOrder.Id,
                OrderType = marketOrder.OrderAction().ToString(),
                AssetPair = marketOrder.AssetPairId,
                Volume = Math.Abs(volume),
                Comission = 0,
                Position = marketOrder.Volume, //???
                TotalCost = Math.Abs(totalCost),
                DateTime = marketOrder.CreatedAt.ToIsoDateTime(),
                Accuracy = assetPair.Accuracy,
                Price = rate,
                BaseAsset = marketOrder.Straight ? assetPair.BaseAssetId : assetPair.QuotingAssetId
            };
        }

        public static ApiTradeOperation[] GetClientTrades(IClientTrade[] clientTrades, IDictionary<string, IAsset> assetsDict, IDictionary<string, IAssetPair> assetPairsDict,
            IDictionary<string, IMarketOrder> marketOrdersDict)
        {
            var apiClientTrades = new List<ApiTradeOperation>();

            foreach (var itm in clientTrades)
            {
                if (string.IsNullOrWhiteSpace(itm.MarketOrderId))
                    continue;

                var asset = assetsDict[itm.AssetId];

                if (asset == null)
                    continue;

                var marketOrder = marketOrdersDict.ContainsKey(itm.MarketOrderId) ? marketOrdersDict[itm.MarketOrderId] : null;
                if (marketOrder != null)
                {
                    var assetPair = assetPairsDict.ContainsKey(marketOrder.AssetPairId)
                        ? assetPairsDict[marketOrder.AssetPairId]
                        : null;
                    if (assetPair != null)
                    {
                        apiClientTrades.Add(itm.ConvertToApiModel(asset, marketOrder, assetPair));
                    }
                }
            }

            return apiClientTrades.ToArray();
        }
    }
}
