using Common;
using Common.Log;
using Lykke.Service.Assets.Client.Custom;
using LykkeApi2.Models;
using LykkeApi2.Models.AssetPairsModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeApi2.Controllers
{
    [Route("api/[controller]")]
    public class AssetPairsController : Controller
    {
        private readonly CachedDataDictionary<string, IAssetPair> _assetPairs;
        private readonly ILog _log;

        public AssetPairsController(CachedDataDictionary<string, IAssetPair> assetPairs, ILog log)
        {
            _assetPairs = assetPairs;
            _log = log;
        }

        [HttpGet]
        public async Task<ResponseModel<AssetPairResponseModesl>> Get()
        {
            var assetPairs = (await _assetPairs.Values()).Where(s => !s.IsDisabled);
            return ResponseModel<AssetPairResponseModesl>.CreateOk(
                AssetPairResponseModesl.Create(assetPairs.Select(itm => itm.ConvertToApiModel()).ToArray()));
        }

        [HttpGet("{assetId}")]
        public async Task<ResponseModel<AssetPairResponseModesl>> GetAssetPairById(string assetId)
        {
            var assetPair = (await _assetPairs.Values()).First(x => x.Id == assetId);
            return ResponseModel<AssetPairResponseModesl>.CreateOk(
                AssetPairResponseModesl.Create(new List<AssetPairModel> { assetPair.ConvertToApiModel() }));
        }       
    }
}
