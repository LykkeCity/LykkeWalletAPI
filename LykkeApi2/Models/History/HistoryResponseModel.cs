using System;
using System.Collections.Generic;
using Lykke.Service.History.Contracts.Enums;
using Lykke.Service.History.Contracts.History;
using Lykke.Service.OperationsHistory.AutorestClient.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LykkeApi2.Models.History
{
    public class HistoryResponseModel
    {
        public string Id { get; set; }

        public DateTime DateTime { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public HistoryType Type { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public HistoryState State { get; set; }

        public decimal Amount { get; set; }

        public string Asset { get; set; }

        public string AssetPair { get; set; }

        public decimal? Price { get; set; }

        public decimal FeeSize { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public FeeType FeeType { get; set; }
    }

    public static class HistoryOperationToResponseConverter
    {
        public static IEnumerable<HistoryResponseModel> ToResponseModel(this BaseHistoryModel baseModel)
        {
            switch (baseModel)
            {
                case CashinModel cashin:
                    yield return cashin.ToResponseModel();
                    break;
                case CashoutModel cashout:
                    yield return cashout.ToResponseModel();
                    break;
                case TransferModel transfer:
                    yield return transfer.ToResponseModel();
                    break;
                case TradeModel trade:
                    foreach (var item in trade.ToResponseModel())
                        yield return item;
                    break;
                case OrderEventModel orderEvent:
                    yield return orderEvent.ToResponseModel();
                    break;
            }
        }

        public static IEnumerable<HistoryResponseModel> ToResponseModel(this TradeModel trade)
        {
            yield return new HistoryResponseModel
            {
                Id = trade.Id.ToString(),
                DateTime = trade.Timestamp,
                Type = HistoryType.Trade,
                Asset = trade.BaseAssetId,
                Amount = trade.BaseVolume,
                AssetPair = trade.AssetPairId,
                Price = trade.Price,
                State = HistoryState.Finished,
                FeeSize = trade.FeeSize > 0 && trade.FeeAssetId == trade.BaseAssetId ? trade.FeeSize.GetValueOrDefault(0) : 0,
                FeeType = FeeType.Absolute
            };

            yield return new HistoryResponseModel
            {
                Id = trade.Id.ToString(),
                DateTime = trade.Timestamp,
                Type = HistoryType.Trade,
                Asset = trade.QuotingAssetId,
                Amount = trade.QuotingVolume,
                AssetPair = trade.AssetPairId,
                Price = trade.Price,
                State = HistoryState.Finished,
                FeeSize = trade.FeeSize > 0 && trade.FeeAssetId == trade.QuotingAssetId ? trade.FeeSize.GetValueOrDefault(0) : 0,
                FeeType = FeeType.Absolute
            };
        }

        public static HistoryResponseModel ToResponseModel(this CashinModel cashin)
        {
            return new HistoryResponseModel
            {
                Id = cashin.Id.ToString(),
                DateTime = cashin.Timestamp,
                Type = HistoryType.CashIn,
                Asset = cashin.AssetId,
                Amount = cashin.Volume,
                State = cashin.State,
                FeeSize = cashin.FeeSize.GetValueOrDefault(0),
                FeeType = FeeType.Absolute
            };
        }

        public static HistoryResponseModel ToResponseModel(this CashoutModel cashout)
        {
            return new HistoryResponseModel
            {
                Id = cashout.Id.ToString(),
                DateTime = cashout.Timestamp,
                Type = HistoryType.CashOut,
                Asset = cashout.AssetId,
                Amount = cashout.Volume,
                State = cashout.State,
                FeeSize = cashout.FeeSize.GetValueOrDefault(0),
                FeeType = FeeType.Absolute
            };
        }

        public static HistoryResponseModel ToResponseModel(this TransferModel transfer)
        {
            return new HistoryResponseModel
            {
                Id = transfer.Id.ToString(),
                DateTime = transfer.Timestamp,
                Type = HistoryType.Transfer,
                Asset = transfer.AssetId,
                Amount = transfer.Volume,
                State = HistoryState.Finished,
                FeeSize = transfer.FeeSize.GetValueOrDefault(0),
                FeeType = FeeType.Absolute
            };
        }

        public static HistoryResponseModel ToResponseModel(this OrderEventModel orderEvent)
        {
            var status = HistoryState.InProgress;
            if (orderEvent.Status == OrderStatus.Cancelled)
                status = HistoryState.Canceled;

            return new HistoryResponseModel
            {
                Id = orderEvent.Id.ToString(),
                DateTime = orderEvent.Timestamp,
                Type = HistoryType.OrderEvent,
                Amount = orderEvent.Volume,
                AssetPair = orderEvent.AssetPairId,
                State = status
            };
        }
    }
}
