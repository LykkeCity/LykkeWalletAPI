using System.Collections.Generic;

namespace LykkeApi2.Models.AssetPairRates
{
    public class AssetPairRatesResponseModel
    {
        public IEnumerable<AssetPairRateModel> AssetPairRates { get; set; }

        public static AssetPairRatesResponseModel Create(IEnumerable<AssetPairRateModel> assetsPairRates)
        {
            return new AssetPairRatesResponseModel
            {
                AssetPairRates = assetsPairRates
            };
        }
    }
}
