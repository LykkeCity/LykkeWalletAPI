using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Services;
using Lykke.Service.History.Contracts.History;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LykkeApi2.Models.History
{
    public enum Direction
    {
        Buy,
        Sell
    }

    public class TradeResponseModel
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        public string AssetPairId { get; set; }

        public decimal Price { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Direction Direction { get; set; }

        public string BaseAssetName { get; set; }

        public decimal BaseVolume { get; set; }

        public string QuoteAssetName { get; set; }

        public decimal QuoteVolume { get; set; }

        public DateTime Timestamp { get; set; }
    }

    public static class TradeModelExtensions
    {
        /// <summary>
        /// Converts history trade models to API response
        /// </summary>
        /// <param name="historyModel">Should be only of type TradeModel</param>
        /// <param name="assetsHelper"></param>
        /// <returns></returns>
        public static async Task<TradeResponseModel> ToTradeResponseModel(this BaseHistoryModel historyModel, IAssetsHelper assetsHelper)
        {
            if (!(historyModel is TradeModel tradeModel))
                return null;

            var assetPair = await assetsHelper.GetAssetPairAsync(tradeModel.AssetPairId);
            var baseAsset = await assetsHelper.GetAssetAsync(assetPair.BaseAssetId);
            var quoteAsset = await assetsHelper.GetAssetAsync(assetPair.QuotingAssetId);

            return new TradeResponseModel
            {
                Id = tradeModel.Id,
                OrderId = tradeModel.OrderId,
                AssetPairId = assetPair.Id,
                BaseAssetName = baseAsset?.DisplayId ?? assetPair.BaseAssetId,
                QuoteAssetName = quoteAsset?.DisplayId ?? assetPair.QuotingAssetId,
                BaseVolume = Math.Abs(tradeModel.BaseVolume),
                QuoteVolume = Math.Abs(tradeModel.QuotingVolume),
                Direction = tradeModel.BaseVolume > 0 ? Direction.Buy : Direction.Sell,
                Price = tradeModel.Price,
                Timestamp = tradeModel.Timestamp
            };
        }
    }
}
