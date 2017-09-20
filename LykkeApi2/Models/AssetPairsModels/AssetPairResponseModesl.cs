using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeApi2.Models.AssetPairsModels
{
    public class AssetPairResponseModesl
    {
        public IEnumerable<AssetPairModel> AssetPairs { get; set; }

        public static AssetPairResponseModesl Create(IEnumerable<AssetPairModel> assetsPairs)
        {
            return new AssetPairResponseModesl
            {
                AssetPairs = assetsPairs
            };
        }
    }
}
