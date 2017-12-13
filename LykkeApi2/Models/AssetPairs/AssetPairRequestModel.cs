using Microsoft.AspNetCore.Mvc;

namespace LykkeApi2.Models.AssetPairsModels
{
    public class AssetPairRequestModel
    {
        [FromRoute(Name = "assetPairId")]
        public string AssetPairId { get; set; }
    }
}
