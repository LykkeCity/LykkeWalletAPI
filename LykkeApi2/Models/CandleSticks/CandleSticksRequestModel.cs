using System;
using Core.Enumerators;
using Lykke.Service.CandlesHistory.Client.Models;

namespace LykkeApi2.Models.CandleSticks
{
    public class CandleSticksRequestModel
    {
        public MarketType Type { get; set; }
        
        public string AssetPairId { get; set; }
        
        public CandlePriceType PriceType { get; set; }
        
        public CandleTimeInterval TimeInterval { get; set; }
        
        public DateTime FromMoment { get; set; }
        
        public DateTime ToMoment { get; set; }
    }
}
