using Core.Enums;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeApi2.Models
{
    public class CandleSticksRequestModel
    {
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
