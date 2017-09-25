using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeApi2.Models.AssetPairsModels
{
    public class AssetPairResponseModel
    {
        public IEnumerable<AssetPairModel> AssetPairs { get; set; }

        public static AssetPairResponseModel Create(IEnumerable<AssetPairModel> assetsPairs)
        {
            return new AssetPairResponseModel
            {
                AssetPairs = assetsPairs
            };
        }
    }
}
