using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeApi2.Models.AssetPairsModels
{
    public class AssetPairRequestModel
    {
        [FromRoute(Name = "assetPairId")]
        public string AssetPairId { get; set; }
    }
}
