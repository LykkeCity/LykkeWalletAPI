using Core.Enums;
using Microsoft.AspNetCore.Mvc;
using System;
using Core.Enumerators;

namespace LykkeApi2.Models
{
    public class CandleSticksRequestModel
    {
        [FromRoute]
        public MarketType Type { get; set; }
        [FromRoute]
        public string AssetPairId { get; set; }
        [FromRoute]
        public EnPriceType PriceType { get; set; }
        [FromRoute]
        public EnTimeInterval TimeInterval { get; set; }
        [FromRoute]
        public DateTime FromMoment { get; set; }
        [FromRoute]
        public DateTime ToMoment { get; set; }
    }
}
