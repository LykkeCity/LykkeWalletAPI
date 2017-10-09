using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeApi2.Models.AssetPairRates
{
    public class AssetPairRateModel
    {
        public string AssetPair { get; set; }
        public double BidPrice { get; set; }
        public double AskPrice { get; set; }

        public DateTime BidPriceTimestamp { get; set; }
        public DateTime AskPriceTimestamp { get; set; } 
    }
}
