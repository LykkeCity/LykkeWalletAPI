using System;
using System.Collections.Generic;
using System.Linq;
using Lykke.Service.CandlesHistory.Client.Models;

namespace LykkeApi2.Models
{
    public class CandleSticksResponseModel
    {
        public List<CandleStickHistoryItem> History { set; get; }
    }

    public class CandleStickHistoryItem
    {
        public DateTime DateTime { set; get; }
        public double Open { get; set; }
        public double Close { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Volume { get; set; }
        public double OppositeVolume { get; set; }
    }

    public static class CandleSticksToResponseConverter
    {
        public static CandleSticksResponseModel ToResponseModel(this CandlesHistoryResponseModel model, int baseAssetAccuracy, int quotingAssetAccuracy)
        {
            var resp = new CandleSticksResponseModel();

            resp.History = model.History.Select(x => new CandleStickHistoryItem
            {
                DateTime = x.DateTime,
                Open = x.Open,
                Close = x.Close,
                High = x.High,
                Low = x.Low,
                Volume = Math.Round(x.TradingVolume, baseAssetAccuracy),
                OppositeVolume =  Math.Round(x.TradingOppositeVolume, quotingAssetAccuracy)
            }).ToList();

            return resp;
        }
    }
}